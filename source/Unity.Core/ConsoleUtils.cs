using System;
using System.Collections.Generic;

namespace Unity.Core
{
    public static class Stdin
    {
        public static IEnumerable<string> SelectLines()
        {
            for (;;)
            {
                var line = Console.ReadLine();
                if (line == null)
                    yield break;

                yield return line;
            }
        }
    }
}
