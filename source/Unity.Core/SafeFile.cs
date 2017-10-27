using System;
using System.IO;

namespace Unity.Core
{
    public static class SafeFile
    {
        // TODO: add tests (see https://stackoverflow.com/a/1528151/14582)
        public static void AtomicWrite(string path, Action<string> write)
        {
            // note that File.Delete doesn't throw if file doesn't exist

            // dotnet doesn't have an atomic move operation (have to pinvoke to something in the OS to get that,
            // and even then on windows it's not guaranteed). so the "atomic" part of this name is just to ensure
            // that partially written file never happens.

            var tmpPath = path + ".tmp";

            try
            {
                File.Delete(tmpPath);
                write(tmpPath);

                // temporarily keep the old file, until we're sure the new file is moved
                var bakPath = path + ".bak";
                File.Delete(bakPath);
                File.Move(path, bakPath);

                File.Move(tmpPath, path);

                // now the old one can go away
                // FUTURE: based on option to func, keep bak file
                File.Delete(bakPath);
            }
            finally
            {
                try
                {
                    File.Delete(tmpPath);
                }
                catch
                {
                    // failure to cleanup a tmp file isn't critical
                }
            }

            // FUTURE: options to throw on existing/auto-overwrite
        }
    }
}
