using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using NSubstitute.Elevated.Weaver;
using Shouldly;
using Unity.Core;

namespace NSubstitute.Elevated.Tests.Utilities
{
    public abstract class PatchingFixture : TestFileSystemFixture
    {
        public NPath Compile(string testAssemblyName, string sourceCode, params string[] dependentAssemblyNames)
        {
            var testAssemblyPath = BaseDir.Combine(testAssemblyName + ".dll");

            // set up to compile

            var compiler = new Microsoft.CSharp.CSharpCodeProvider();
            var compilerArgs = new CompilerParameters
            {
                OutputAssembly = testAssemblyPath,
                IncludeDebugInformation = true,
                CompilerOptions = "/o- /debug+ /warn:0"
            };
            compilerArgs.ReferencedAssemblies.Add(typeof(int).Assembly.Location); // mscorlib
            compilerArgs.ReferencedAssemblies.AddRange(
                dependentAssemblyNames.Select(n => BaseDir.Combine(n + ".dll").ToString()));

            // compile and handle errors

            var compilerResult = compiler.CompileAssemblyFromSource(compilerArgs, sourceCode);
            if (compilerResult.Errors.Count > 0)
            {
                var errorText = compilerResult.Errors
                    .OfType<CompilerError>()
                    .Select(e => $"({e.Line},{e.Column}): error {e.ErrorNumber}: {e.ErrorText}")
                    .Prepend("Compiler errors:")
                    .StringJoin("\n");
                throw new Exception(errorText);
            }

            testAssemblyPath.ShouldBe(new NPath(compilerResult.PathToAssembly));

            PeVerify.Verify(testAssemblyPath); // sanity check on what the compiler generated

            return testAssemblyPath;
        }

        public TypeDefinition GetType(AssemblyDefinition testAssembly, string typeName)
            => testAssembly.MainModule.GetType(typeName);
        public IEnumerable<TypeDefinition> SelectTypes(AssemblyDefinition testAssembly, IncludeNested includeNested)
            => testAssembly.SelectTypes(includeNested);
    }
}
