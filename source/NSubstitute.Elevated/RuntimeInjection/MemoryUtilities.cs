using System;

namespace NSubstitute.Elevated.RuntimeInjection
{
    static class MemoryUtilities
    {
        static void UnprotectMemoryPage(long memory)
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

        static void ProtectMemoryPage(long memory) { }

        internal static void WriteJump(long memory, long destination)
        {
            UnprotectMemoryPage(memory);

            if (IntPtr.Size == sizeof(long))
            {
                if (CompareBytes(memory, new byte[] { 0xe9 }))
                {
                    var offset = ReadInt(memory + 1);
                    memory += 5 + offset;
                }

                memory = WriteBytes(memory, new byte[] { 0x48, 0xB8 });
                memory = WriteLong(memory, destination);
                _ = WriteBytes(memory, new byte[] { 0xFF, 0xE0 });
            }
            else
            {
                memory = WriteByte(memory, 0x68);
                memory = WriteInt(memory, (int)destination);
                _ = WriteByte(memory, 0xc3);
            }

            FlushInstructionCache(memory);

            ProtectMemoryPage(memory);
        }

        static void FlushInstructionCache(long memory)
        {
            //FlushInstructionCache(memory, new UIntPtr(1));
        }

        static unsafe long WriteByte(long memory, byte value)
        {
            var p = (byte*)memory;
            *p = value;
            return memory + sizeof(byte);
        }

        static unsafe int ReadInt(long memory)
        {
            var p = (int*)memory;
            return *p;
        }

        static unsafe long WriteLong(long memory, long value)
        {
            var p = (long*)memory;
            *p = value;
            return memory + sizeof(long);
        }

        static long WriteBytes(long memory, byte[] values)
        {
            foreach (var value in values)
                memory = WriteByte(memory, value);
            return memory;
        }

        static unsafe long WriteInt(long memory, int value)
        {
            var p = (int*)memory;
            *p = value;
            return memory + sizeof(int);
        }

        static unsafe bool CompareBytes(long memory, byte[] values)
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