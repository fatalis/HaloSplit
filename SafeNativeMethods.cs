﻿using System;
using System.Runtime.InteropServices;

namespace LiveSplit.HaloSplit
{
    static class SafeNativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize, // should be IntPtr if we ever need to read a size bigger than 32 bit address space
            out int lpNumberOfBytesRead);
    }
}
