using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Reflection;
using NSubstitute.Core;
using Unity.Core;

namespace NSubstitute.Elevated.Tests {
    public class PatchingSupport
    {
        // returns true if a mock is in place and it is taking over functionality. instance may be null
        // if static. mockedReturnValue is ignored in a void return func.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue , Type[] methodGenericTypes, object[] args)
        {
            mockedReturnValue = 10;
            if (!(SubstitutionContext.Current is PatchingSubstituteContext elevated))
            {
                mockedReturnValue = mockedReturnType.GetDefaultValue();
                return false;
            }

            var method = (MethodInfo) new StackTrace(1).GetFrame(0).GetMethod();

            if (method.IsGenericMethodDefinition)
                method = method.MakeGenericMethod(methodGenericTypes);

            return elevated.patchingSubstituteManager.TryMock(actualType, instance, mockedReturnType, out mockedReturnValue, method, methodGenericTypes, args);
        }

        struct ExceptionHandler
        {
            public Type CatchType;
            public string Flag;
            public int TryStart;
            public int TryEnd;
            public int HandlerStart;
            public int HandlerEnd;
        }

        public static Delegate Rewrite(Action testAction)
        {
            // 1. Scan the methodbody, looking for `using (SubstituteStatic.For<StaticClass>())`
            //  - we store the beginning and end for the context
            // 2. as we scan, patch all the invocations to NSub API with invocations to trymock

            var methodInfo = testAction.GetMethodInfo();
            var dynamicMethod = new DynamicMethod(
                $"{methodInfo.Name}_Rewritten",
                methodInfo.Attributes,
                methodInfo.CallingConvention,
                methodInfo.ReturnType,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(),
                methodInfo.Module,
                true);

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                dynamicMethod.DefineParameter(parameterInfo.Position, parameterInfo.Attributes, parameterInfo.Name);
            }

            var generator = dynamicMethod.GetILGenerator();

            var stack = new Stack<Type>();
            var instructions = methodInfo.GetInstructions().ToArray();
            var originalMethodBody = methodInfo.GetMethodBody();
            var exceptionHandlingClauses = originalMethodBody.ExceptionHandlingClauses;

            foreach (var localVariableInfo in originalMethodBody.LocalVariables)
            {
                generator.DeclareLocal(localVariableInfo.LocalType, localVariableInfo.IsPinned);
            }
            
            var handlers = new List<ExceptionHandler>();

            foreach (var clause in exceptionHandlingClauses)
            {
                ExceptionHandler handler = new ExceptionHandler();
                handler.Flag = clause.Flags.ToString();
                handler.CatchType = handler.Flag == "Clause" ? clause.CatchType : null;
                handler.TryStart = clause.TryOffset;
                handler.TryEnd = (clause.TryOffset + clause.TryLength);
                handler.HandlerStart = clause.HandlerOffset;
                handler.HandlerEnd = (clause.HandlerOffset + clause.HandlerLength);
                handlers.Add(handler);
            }
            
            var labels = new Dictionary<int,Label>();
            
            for (var index = 0; index < instructions.Length; index++)
            {
                var instr = instructions[index];

                if (labels.TryGetValue(instr.Offset, out var targetLabel))
                {
                    generator.MarkLabel(targetLabel);
                    labels.Remove(instr.Offset);
                }

                EmitILForExceptionHandlers(generator, instr, handlers);
                
                if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt || instr.OpCode == OpCodes.Calli)
                {
                    var methodBeingCalled = (MethodInfo)instr.Operand;
                    if(methodBeingCalled.Name == "For")
                    {
                        var mockedType = methodBeingCalled.GetGenericArguments()[0];

                        stack.Push(mockedType);
                    }
                    else
                    {
                        var declaringType = methodBeingCalled.DeclaringType;
                        
                        if (IsTypeBeingMocked(stack, declaringType))
                        {
                            var proxy = GetOrCreateMockProxyFor(methodBeingCalled);
                            generator.Emit(OpCodes.Call, proxy);
                            continue;
                        }
                    }
                }

                if (instr.Operand == null)
                {
                    generator.Emit(instr.OpCode);
                }
                else
                {
                    if (instr.Operand.GetType().Name == "Instruction")
                    {
                        var targetInstruction = (Instruction)instr.Operand;
                        if(!labels.TryGetValue(targetInstruction.Offset, out var label))
                            labels.Add(targetInstruction.Offset, label = generator.DefineLabel());

                        generator.Emit(instr.OpCode, label);
                    }
                    else
                    {
                        var emitDelegate = generator.GetType().GetMethod("Emit", new[]
                        {
                            instr.OpCode.GetType(),
                            instr.Operand.GetType()
                        });
                
                        emitDelegate.Invoke(generator, new object[]
                        {
                            instr.OpCode,
                            instr.Operand 
                        });
                    }
                }
            }

            return dynamicMethod.CreateDelegate(
                typeof(Action));
        }

        static void EmitILForExceptionHandlers(ILGenerator ilGenerator, Instruction instruction, List<ExceptionHandler> handlers)
        {
            var catchBlockCount = 0;
            foreach (var handler in handlers.Where(h => h.TryStart == instruction.Offset))
            {
                if (handler.Flag == "Clause")
                {
                    if (catchBlockCount >= 1)
                        continue;

                    ilGenerator.BeginExceptionBlock();
                    catchBlockCount++;
                    continue;
                }

                ilGenerator.BeginExceptionBlock();
            }

            foreach (var handler in handlers.Where(h => h.HandlerStart == instruction.Offset))
            {
                if (handler.Flag == "Clause")
                    ilGenerator.BeginCatchBlock(handler.CatchType);
                else if (handler.Flag == "Finally")
                    ilGenerator.BeginFinallyBlock();
            }

            foreach (var handler in handlers.Where(h => h.HandlerEnd == instruction.Offset))
            {
                if (handler.Flag == "Clause")
                {
                    var _handlers = handlers.Where(h => h.TryEnd == handler.TryEnd && h.Flag == "Clause");
                    if (handler.HandlerEnd == _handlers.Select(h => h.HandlerEnd).Max())
                        ilGenerator.EndExceptionBlock();

                    continue;
                }

                ilGenerator.EndExceptionBlock();
            }
        }

        static DynamicMethod m_Cache;
        static DynamicMethod GetOrCreateMockProxyFor(MethodInfo methodInfo)
        {
            if (m_Cache != null)
                return m_Cache;
            m_Cache = new DynamicMethod(
                $"{methodInfo.Name}_Proxy_{methodInfo.GetParameters().Length}",
                methodInfo.Attributes,
                methodInfo.CallingConvention,
                methodInfo.ReturnType,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(),
                methodInfo.Module,
                true);
            
            
            var tryMockMethod = typeof(PatchingSupport).GetMethod("TryMock");
            var getTypeFromHandle = typeof(System.Type).GetMethod("GetTypeFromHandle");

            var il = m_Cache.GetILGenerator();
            
            // (Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, Type[] methodGenericTypes, object[] args)

            // object returnValue;
            il.DeclareLocal(typeof(object));
            
            // var a = typeof(StaticClass);
            il.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
            il.Emit(OpCodes.Call, getTypeFromHandle);
            // var b = null;
            il.Emit(OpCodes.Ldnull);
            // var c = typeof(int);
            il.Emit(OpCodes.Ldtoken, typeof(int));
            il.Emit(OpCodes.Call, getTypeFromHandle);
            // out returnValue
            il.Emit(OpCodes.Ldloca_S, 0);
            // var d = new Type[0];
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Newarr, typeof(Type));
            // var e = new[] { (object) i };
            il.Emit(OpCodes.Ldc_I4_1); // size
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_0); // index
            il.Emit(OpCodes.Ldarg_0); // arg-index
            il.Emit(OpCodes.Box, typeof(int)); // convert type to System.Object
            il.Emit(OpCodes.Stelem_Ref);
            
            // PatchingSupport.TryMock(a, b, c, out returnValue, d, e);
            il.Emit(OpCodes.Call, tryMockMethod);
            
            il.Emit(OpCodes.Pop);
            //return (int)returnValue;
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType); // TODO: convert
            il.Emit(OpCodes.Ret);
            
            // object returnValue;
            // PatchingSupport.TryMock(typeof(???), null, typeof(int), out returnValue, new Type[0], new[] { (object) i });
            //return (int)returnValue;

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                m_Cache.DefineParameter(parameterInfo.Position, parameterInfo.Attributes, parameterInfo.Name);
            }

//            var @delegate = m_Cache.CreateDelegate(
//                typeof(Func<,>).MakeGenericType(
//                    methodInfo.GetParameters()[0].ParameterType,
//                    methodInfo.ReturnType));
//            
//            return @delegate.GetMethodInfo();
            return m_Cache;
        }

        static bool IsTypeBeingMocked(Stack<Type> stack, Type declaringType)
        {
            return stack.Any(t => t == declaringType);
        }
    }
}