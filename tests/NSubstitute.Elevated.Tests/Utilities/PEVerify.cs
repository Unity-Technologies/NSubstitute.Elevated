using System;
using System.Diagnostics;
using Shouldly;

namespace NSubstitute.Elevated.Tests.Utilities
{
    public class PeVerifyException : Exception
    {
        public PeVerifyException(string message, int exitCode, string output)
            : base(message)
        {
            ExitCode = exitCode;
            Output = output;
        }

        public int ExitCode { get; }
        public string Output { get; }

        public override string ToString()
        {
            return $"{Message} (exit={ExitCode})\n{Output}";
        }
    }

    public static class PeVerify
    {
        // TODO: Fix
        const string k_PeVerifyLocation = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\x64\PEVerify.exe";
        public static void Verify(string assemblyName)
        {
            var p = new Process
            {
                StartInfo =
                {
                    Arguments = $"/nologo \"{assemblyName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = k_PeVerifyLocation,
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
    }
}
