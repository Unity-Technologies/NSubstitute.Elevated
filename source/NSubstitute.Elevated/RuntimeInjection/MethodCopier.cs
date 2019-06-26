using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Reflection;

static internal class MethodCopier
{
    struct ExceptionHandler
    {
        public Type CatchType;
        public ExceptionHandlingClauseOptions Flag;
        public int TryStart;
        public int TryEnd;
        public int HandlerStart;
        public int HandlerEnd;
    }
    
    public static DynamicMethod CopyMethod(MethodInfo methodInfo, string newName)
    {
        var parameterInfos = methodInfo.GetParameters();
        var dynamicMethod = new DynamicMethod(
            newName,
            methodInfo.Attributes,
            methodInfo.CallingConvention,
            methodInfo.ReturnType,
            parameterInfos.Select(p => p.ParameterType).ToArray(),
            methodInfo.Module,
            true);

        foreach (var parameterInfo in parameterInfos)
        {
            dynamicMethod.DefineParameter(parameterInfo.Position, parameterInfo.Attributes, parameterInfo.Name);
        }

        MethodCopier.CopyMethodBody(methodInfo, dynamicMethod.GetILGenerator());
        return dynamicMethod;
    }
    
    static void CopyMethodBody(MethodInfo methodInfo, ILGenerator generator)
    {
        var body = methodInfo.GetMethodBody();

        // 1. Declare all the variables
        foreach (var localVariableInfo in body.LocalVariables)
        {
            generator.DeclareLocal(localVariableInfo.LocalType, localVariableInfo.IsPinned);
        }

        // 2. Decompile the original method
        var instructions = methodInfo.GetInstructions().ToArray();

        // 3. Labels
        // Define labels for all the jump targets
        var labels = new Dictionary<int, Label>();
        foreach (var instr in instructions)
        {
            if (instr.Operand is Instruction instruction)
            {
                if (instr.OpCode != OpCodes.Leave && instr.OpCode != OpCodes.Leave_S)
                {
                    var offset = instruction.Offset;
                    if (!labels.ContainsKey(offset))
                        labels.Add(offset, generator.DefineLabel());
                }
            }
        }

        var handlers = body.ExceptionHandlingClauses.Select(
            clause => new ExceptionHandler
            {
                Flag = clause.Flags,
                CatchType = clause.Flags == ExceptionHandlingClauseOptions.Clause ? clause.CatchType : null,
                TryStart = clause.TryOffset,
                TryEnd = clause.TryOffset + clause.TryLength,
                HandlerStart = clause.HandlerOffset,
                HandlerEnd = clause.HandlerOffset + clause.HandlerLength,
            }).ToArray();

        // 4. Copy the instructions
        foreach (var instr in instructions)
        {
            var offset = instr.Offset;

            if (labels.TryGetValue(offset, out var targetLabel))
                generator.MarkLabel(targetLabel);
            
            EmitILForExceptionHandlers(generator, handlers, instr.Offset);

            if (instr.Operand != null)
            {
                switch (instr.Operand) {
                    case Instruction instrOperand:
                    {
                        if (instr.OpCode != OpCodes.Leave && instr.OpCode != OpCodes.Leave_S)
                        {
                            var label = labels[instrOperand.Offset];
                            generator.Emit(instr.OpCode, label);
                        }

                        break;
                    }

                    case ParameterInfo parameterInfo:
                        generator.Emit(instr.OpCode, (byte)parameterInfo.Position);
                        break;

                    default:
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
                        break;
                    }
                }
            }
            else
            {
                generator.Emit(instr.OpCode);
            }
        }
    }
    
    static void EmitILForExceptionHandlers(ILGenerator generator, ExceptionHandler[] handlers, int offset)
    {
        var isFirstCatch = true;
        foreach (var handler in handlers.Where(h => h.TryStart == offset))
        {
            if (handler.Flag == ExceptionHandlingClauseOptions.Clause)
            {
                if (isFirstCatch)
                {
                    generator.BeginExceptionBlock();
                    isFirstCatch = false;
                }
            }
            else
            {
                generator.BeginExceptionBlock();
            }
        }

        foreach (var handler in handlers.Where(h => h.HandlerStart == offset))
        {
            switch (handler.Flag)
            {
                case ExceptionHandlingClauseOptions.Clause:
                    generator.BeginCatchBlock(handler.CatchType);
                    break;
                case ExceptionHandlingClauseOptions.Finally:
                    generator.BeginFinallyBlock();
                    break;
                default:
                    throw new NotImplementedException($"Support for {handler.Flag} Handlers is not implemented");
            }
        }

        foreach (var handler in handlers.Where(h => h.HandlerEnd == offset))
        {
            if (handler.Flag == ExceptionHandlingClauseOptions.Clause)
            {
                var _handlers = handlers.Where(h => h.TryEnd == handler.TryEnd && h.Flag == ExceptionHandlingClauseOptions.Clause);
                if (handler.HandlerEnd == _handlers.Select(h => h.HandlerEnd).Max())
                    generator.EndExceptionBlock();
            }
            else
            {
                generator.EndExceptionBlock();
            }
        }
    }
}