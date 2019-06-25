using System;

namespace NSubstitute.Elevated.RuntimeInjection
{
    static class MemoryUtilities
    {
        internal static void UnprotectMemoryPage(long memory)
        {
            /*
                if (IsWindows)
                {
                    var success = VirtualProtect(new IntPtr(memory), new UIntPtr(1), Protection.PAGE_EXECUTE_READWRITE, out _);
                    if (success == false)
                        throw new System.ComponentModel.Win32Exception();
                }
                */
        }

        internal static void ProtectMemoryPage(long memory) { }

        internal static void FlushInstructionCache(long memory)
        {
            //FlushInstructionCache(memory, new UIntPtr(1));
        }

        internal static unsafe long WriteByte(long memory, byte value)
        {
            var p = (byte*)memory;
            *p = value;
            return memory + sizeof(byte);
        }

        internal static unsafe int ReadInt(long memory)
        {
            var p = (int*)memory;
            return *p;
        }

        internal static unsafe long WriteLong(long memory, long value)
        {
            var p = (long*)memory;
            *p = value;
            return memory + sizeof(long);
        }

        internal static long WriteBytes(long memory, byte[] values)
        {
            foreach (var value in values)
                memory = WriteByte(memory, value);
            return memory;
        }

        internal static unsafe long WriteInt(long memory, int value)
        {
            var p = (int*)memory;
            *p = value;
            return memory + sizeof(int);
        }

        internal static unsafe bool CompareBytes(long memory, byte[] values)
        {
            var p = (byte*)memory;
            foreach (var value in values)
            {
                if (value != *p) return false;
                p++;
            }

            return true;
        }
    }
}