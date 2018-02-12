using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using NSubstitute.Elevated.Weaver;
using Shouldly;
using Unity.Core;

namespace NSubstitute.Elevated.Tests.Utilities
{
    public class TestAssembly : IDisposable
    {
        string m_TestAssemblyPath;
        AssemblyDefinition m_TestAssembly;

        public TestAssembly(string assemblyName, string testSourceCodeFile)
        {
            var outputFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            outputFolder.ShouldNotBeNull();

            m_TestAssemblyPath = Path.Combine(outputFolder, assemblyName + ".dll");

            var compiler = new Microsoft.CSharp.CSharpCodeProvider();
            var compilerArgs = new CompilerParameters
            {
                OutputAssembly = m_TestAssemblyPath,
                IncludeDebugInformation = true,
                CompilerOptions = "/o- /debug+ /warn:0"
            };
            compilerArgs.ReferencedAssemblies.Add(typeof(Enumerable).Assembly.Location);

            var compilerResult = compiler.CompileAssemblyFromSource(compilerArgs, testSourceCodeFile);
            if (compilerResult.Errors.Count > 0)
            {
                var errorText = compilerResult.Errors
                    .OfType<CompilerError>()
                    .Select(e => $"({e.Line},{e.Column}): error {e.ErrorNumber}: {e.ErrorText}")
                    .Prepend("Compiler errors:")
                    .StringJoin("\n");
                throw new Exception(errorText);
            }

            m_TestAssemblyPath = compilerResult.PathToAssembly;

            //PeVerify.Verify(m_TestAssemblyPath); // pre-check..sometimes we can compile code that doesn't verify
            Verify(m_TestAssemblyPath);

            var results = ElevatedWeaver.PatchAllDependentAssemblies(m_TestAssemblyPath, PatchTestAssembly.Yes, new [] { new FileInfo(m_TestAssemblyPath).Name.Replace(".dll", string.Empty) });
            results.Count.ShouldBe(2);
            results.ShouldContain(new PatchResult("mscorlib", null, PatchState.IgnoredOutsideAllowedPaths));
            results.ShouldContain(new PatchResult(m_TestAssemblyPath, ElevatedWeaver.GetPatchBackupPathFor(m_TestAssemblyPath), PatchState.Patched));

            m_TestAssembly = AssemblyDefinition.ReadAssembly(m_TestAssemblyPath);
            MockInjector.IsPatched(m_TestAssembly).ShouldBeTrue();

            //PeVerify.Verify(m_TestAssemblyPath);
            Verify(m_TestAssemblyPath);
        }

        // TODO: Fix
        const string peVerifyLocation = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\x64\PEVerify.exe";
        static void Verify(string assemblyName)
        {
            var p = new Process
            {
                StartInfo =
                {
                    Arguments = $"/nologo \"{assemblyName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = peVerifyLocation,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            var error = "";
            var output = "";

            p.OutputDataReceived += (_, e) => output += $"{e.Data}\n";
            p.ErrorDataReceived += (_, e) => error += $"{e.Data}\n";

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            Console.WriteLine(assemblyName);
            p.ExitCode.ShouldBe(0, () => $"{error}\n{output}");
        }

        public void Dispose()
        {
            m_TestAssembly.Dispose();

            var dir = new DirectoryInfo(Path.GetDirectoryName(m_TestAssemblyPath));
            foreach (var file in dir.EnumerateFiles(Path.GetFileNameWithoutExtension(m_TestAssemblyPath) + ".*"))
                File.Delete(file.FullName);
        }

        public TypeDefinition GetType(string typeName) => m_TestAssembly.MainModule.GetType(typeName);
        public IEnumerable<TypeDefinition> SelectTypes(IncludeNested includeNested) => m_TestAssembly.SelectTypes(includeNested);
    }
}
