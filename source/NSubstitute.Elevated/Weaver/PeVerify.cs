using System;
using System.Collections.Generic;
using Unity.Core;

namespace NSubstitute.Elevated.Weaver
{
    public class PeVerifyException : Exception
    {
        public PeVerifyException(string assemblyName, int exitCode, IEnumerable<string> log)
            : base(
                $"Failed to PEVerify assembly '{assemblyName}' (exit={exitCode})\n\n"
                + log.StringJoin('\n')) { }
    }

    public static class PeVerify
    {
        // TODO: detect, make compat with dotnet core for xplat..
        // others have solved it like this:
        //   https://github.com/castleproject-deprecated/Castle.Core-READONLY/blob/master/src/Castle.Core.Tests/BasePEVerifyTestCase.cs
        //   https://github.com/rsdn/nemerle/blob/6904f590bf8b25a97838c6733dd2e53bd68467fd/snippets/codegentests/Tests/Verification/PeVerify.cs#L55
        //   https://github.com/bamboo/boo/blob/1f2f60a08ca69e95db34b30154985170f4723cad/src/Boo.Lang.Compiler/Steps/PEVerify.cs#L47
        public static string ExePath => @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\x64\PEVerify.exe";

        public static void Verify(string assemblyName)
        {
            var log = new List<string>();
            var rc = ProcessUtility.ExecuteCommandLine(ExePath, new[] { "/nologo", assemblyName }, null, log, log);

            // TODO: not great to just throw like this vs. returning an error structure
            // TODO: will it return 0 even if there are warnings?
            if (rc != 0)
                throw new PeVerifyException(assemblyName, rc, log);
        }
    }
}
