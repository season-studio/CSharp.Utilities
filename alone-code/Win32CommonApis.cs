using System;
using System.Runtime.InteropServices;

namespace GeneralKit
{
    public class Win32CommonApis
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public UInt32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        public enum SECURITY_IMPERSONATION_LEVEL : int
        {
            SecurityAnonymous = 0,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

    }
}
