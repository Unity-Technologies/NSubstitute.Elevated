using System;
using System.Diagnostics;
using Microsoft.Build.Utilities;

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
        public static void Verify(string assemblyToTestPath)
        {
            var peVerifyPath = ToolLocationHelper.GetPathToDotNetFrameworkSdkFile("peverify.exe", TargetDotNetFrameworkVersion.Version45);

            var psi = new ProcessStartInfo(peVerifyPath, $"/nologo \"{assemblyToTestPath}\"")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(psi))
            {
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        var stdout = process.StandardOutput.ReadToEnd(); // peverify apparently doesn't write to stderr..
                        throw new PeVerifyException($"Failure during PEVerify of {assemblyToTestPath}", process.ExitCode, stdout);
                    }
                }
            }
        }
    }
}
