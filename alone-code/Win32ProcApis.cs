using System;
using System.Runtime.InteropServices;

namespace GeneralKit
{
    public abstract class Win32ProcApis
    {
        /// <summary>
        /// 令牌类别
        /// </summary>
        public enum TOKEN_INFORMATION_CLASS
        {
            /// <summary>
            /// 用户
            /// </summary>
            TokenUser = 1,
            /// <summary>
            /// 组
            /// </summary>
            TokenGroups,
            /// <summary> </summary>
            TokenPrivileges,
            /// <summary> </summary>
            TokenOwner,
            /// <summary> </summary>
            TokenPrimaryGroup,
            /// <summary> </summary>
            TokenDefaultDacl,
            /// <summary> </summary>
            TokenSource,
            /// <summary> </summary>
            TokenType,
            /// <summary> </summary>
            TokenImpersonationLevel,
            /// <summary> </summary>
            TokenStatistics,
            /// <summary> </summary>
            TokenRestrictedSids,
            /// <summary> </summary>
            TokenSessionId,
            /// <summary> </summary>
            TokenGroupsAndPrivileges,
            /// <summary> </summary>
            TokenSessionReference,
            /// <summary> </summary>
            TokenSandBoxInert,
            /// <summary> </summary>
            TokenAuditPolicy,
            /// <summary> </summary>
            TokenOrigin,
            /// <summary> </summary>
            TokenElevationType,
            /// <summary> </summary>
            TokenLinkedToken,
            /// <summary> </summary>
            TokenElevation,
            /// <summary> </summary>
            TokenHasRestrictions,
            /// <summary> </summary>
            TokenAccessInformation,
            /// <summary> </summary>
            TokenVirtualizationAllowed,
            /// <summary> </summary>
            TokenVirtualizationEnabled,
            /// <summary> </summary>
            TokenIntegrityLevel,
            /// <summary> </summary>
            TokenUIAccess,
            /// <summary> </summary>
            TokenMandatoryPolicy,
            /// <summary> </summary>
            TokenLogonSid,
            /// <summary> </summary>
            MaxTokenInfoClass
        };

        [DllImport("Advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool SetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ref uint TokenInformation, uint TokenInformationLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        public enum TOKEN_TYPE : int
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref Win32CommonApis.SECURITY_ATTRIBUTES lpProcessAttributes,
            ref Win32CommonApis.SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandle,
            Int32 dwCreationFlags,
            IntPtr lpEnvrionment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            UInt32 dwDesiredAccess,
            ref Win32CommonApis.SECURITY_ATTRIBUTES lpThreadAttributes,
            Win32CommonApis.SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            TOKEN_TYPE dwTokenType,
            out IntPtr phNewToken);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessWithTokenW", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public extern static bool CreateProcessWithTokenW(
            IntPtr hToken,
            uint dwLogonFlags,
            String lpApplicationName,
            String lpCommandLine,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            UInt32 DesiredAccess,
            out IntPtr TokenHandle);


        public const uint TOKEN_DUPLICATE = 0x0002;
        public const uint GENERIC_ALL_ACCESS = 0x10000000;

        public const uint LOGON_WITH_PROFILE = 00000001;
        public const uint NORMAL_PRIORITY_CLASS = 0x00000020;
        public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    }
}
