using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace GeneralKit
{
    /// <summary>
    /// 远程访问API
    /// </summary>
    public class DesktopSessionInterop
    {
        #region Native API 申明
        /// <summary>
        /// 当前服务句柄
        /// </summary>
        private static IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

        /// <summary>
        /// MessageBox的风格常数定义类
        /// </summary>
        public abstract class MsgBoxStyle
        {
            /// <summary>
            /// 停止、重试、忽略
            /// </summary>
            public const int MB_ABORTRETRYIGNORE = 0x00000002;
            /// <summary>
            /// 取消、重试、忽略
            /// </summary>
            public const int MB_CANCELTRYCONTINUE = 0x00000006;
            /// <summary>
            /// 帮助
            /// </summary>
            public const int MB_HELP = 0x00004000;
            /// <summary>
            /// 确定
            /// </summary>
            public const int MB_OK = 0x00000000;
            /// <summary>
            /// 确定和取消
            /// </summary>
            public const int MB_OKCANCEL = 0x00000001;
            /// <summary>
            /// 重试和取消
            /// </summary>
            public const int MB_RETRYCANCEL = 0x00000005;
            /// <summary>
            /// 是否
            /// </summary>
            public const int MB_YESNO = 0x00000004;
            /// <summary>
            /// 是否和取消
            /// </summary>
            public const int MB_YESNOCANCEL = 0x00000003;
            /// <summary>
            /// 感叹号图标
            /// </summary>
            public const int MB_ICONEXCLAMATION = 0x00000030;
            /// <summary>
            /// 警告图标
            /// </summary>
            public const int MB_ICONWARNING = 0x00000030;
            /// <summary>
            /// 信息图标
            /// </summary>
            public const int MB_ICONINFORMATION = 0x00000040;
            /// <summary>
            /// 星号图标
            /// </summary>
            public const int MB_ICONASTERISK = 0x00000040;
            /// <summary>
            /// 询问图标
            /// </summary>
            public const int MB_ICONQUESTION = 0x00000020;
            /// <summary>
            /// 停止图标
            /// </summary>
            public const int MB_ICONSTOP = 0x00000010;
            /// <summary>
            /// 错误图标
            /// </summary>
            public const int MB_ICONERROR = 0x00000010;
            /// <summary>
            /// 手状图标
            /// </summary>
            public const int MB_ICONHAND = 0x00000010;
            /// <summary>
            /// 默认按键1
            /// </summary>
            public const int MB_DEFBUTTON1 = 0x00000000;
            /// <summary>
            /// 默认按键2
            /// </summary>
            public const int MB_DEFBUTTON2 = 0x00000100;
            /// <summary>
            /// 默认按键3
            /// </summary>
            public const int MB_DEFBUTTON3 = 0x00000200;
            /// <summary>
            /// 默认按键4
            /// </summary>
            public const int MB_DEFBUTTON4 = 0x00000300;
            /// <summary>
            /// APP模式
            /// </summary>
            public const int MB_APPLMODAL = 0x00000000;
            /// <summary>
            /// 系统模式
            /// </summary>
            public const int MB_SYSTEMMODAL = 0x00001000;
            /// <summary>
            /// 任务模式
            /// </summary>
            public const int MB_TASKMODAL = 0x00002000;
            /// <summary>
            /// 默认桌面
            /// </summary>
            public const int MB_DEFAULT_DESKTOP_ONLY = 0x00020000;
            /// <summary>
            /// 靠右模式
            /// </summary>
            public const int MB_RIGHT = 0x00080000;
            /// <summary>
            /// 从右向左读模式
            /// </summary>
            public const int MB_RTLREADING = 0x00100000;
            /// <summary>
            /// 设置前端焦点
            /// </summary>
            public const int MB_SETFOREGROUND = 0x00010000;
            /// <summary>
            /// 总在前端
            /// </summary>
            public const int MB_TOPMOST = 0x00040000;
            /// <summary>
            /// 服务通知
            /// </summary>
            public const int MB_SERVICE_NOTIFICATION = 0x00200000;
        }

        /// <summary>
        /// MessageBox的操作结果码
        /// </summary>
        public enum MsgBoxResult : int
        {
            /// <summary>
            /// 终止
            /// </summary>
            IDABORT = 3,
            /// <summary>
            /// 取消
            /// </summary>
            IDCANCEL = 2,
            /// <summary>
            /// 继续
            /// </summary>
            IDCONTINUE = 11,
            /// <summary>
            /// 忽略
            /// </summary>
            IDIGNORE = 5,
            /// <summary>
            /// 否
            /// </summary>
            IDNO = 7,
            /// <summary>
            /// 确定
            /// </summary>
            IDOK = 1,
            /// <summary>
            /// 重试
            /// </summary>
            IDRETRY = 4,
            /// <summary>
            /// 重试
            /// </summary>
            IDTRYAGAIN = 10,
            /// <summary>
            /// 是
            /// </summary>
            IDYES = 6
        }

        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSSendMessage(IntPtr hServer, int SessionId, string pTitle, int TitleLength, string pMessage, int MessageLength,
                                                 int Style, int Timeout, out int pResponse, bool bWait);

        private const int GENERIC_ALL_ACCESS = 0x10000000;

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(
            uint sessionId,
            out IntPtr Token);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(
            out IntPtr lpEnvironment,
            IntPtr hToken,
            bool bInherit);
        #endregion

        /// <summary>
        /// 在当前的会话中显示消息框
        /// </summary>
        /// <param name="_msg">消息内容</param>
        /// <param name="_title">消息标题</param>
        /// <param name="_style">消息框风格，取值可以是<see cref="MsgBoxStyle"/>中值的按位组合</param>
        /// <param name="_timeout">消息框超时时间</param>
        /// <returns>用户在对话框上操作的结果码，取值参考<see cref="MsgBoxResult"/></returns>
        public static int MessageBox(string _msg, string _title, int _style = 0, int _timeout = 0)
        {
            int _resp = 0;
            WTSSendMessage(WTS_CURRENT_SERVER_HANDLE, WTSGetActiveConsoleSessionId(),
                           _title, _title.Length, _msg, _msg.Length, _style, _timeout, out _resp, true);
            return _resp;
        }

        /// <summary>
        /// 在当前会话中创建进程
        /// </summary>
        /// <param name="_sessionID">运行程序的目标会话ID</param>
        /// <param name="_app">程序名</param>
        /// <param name="_args">程序命令行参数</param>
        /// <param name="_runPath">运行路径</param>
        /// <param name="_asSystem">true以系统权限运行，false以普通权限运行</param>
        /// <param name="_hProc">得到的进程句柄</param>
        /// <param name="_hThread">得到的进程主线程句柄</param>
        /// <returns>0表示成功，非0表示失败</returns>
        public static int CreateProcess(uint _sessionID, string _app, string _args, string _runPath, bool _asSystem, out IntPtr _hProc, out IntPtr _hThread)
        {
            int _ret = 0;
            IntPtr lpEnvironment = IntPtr.Zero;
            IntPtr _hToken = IntPtr.Zero;
            IntPtr _hDupedToken = IntPtr.Zero;
            Win32CommonApis.SECURITY_ATTRIBUTES _sa = new Win32CommonApis.SECURITY_ATTRIBUTES();
            _sa.Length = (uint)Marshal.SizeOf(_sa);

            _hProc = IntPtr.Zero;
            _hThread = IntPtr.Zero;

            // 获取目标用户令牌
            if (!_asSystem)
            {
                if (!WTSQueryUserToken(_sessionID, out _hToken))
                {
                    _ret = -1;
                }
            }
            else
            {
                _hToken = WindowsIdentity.GetCurrent().Token;
            }

            // 复制令牌
            if (0 == _ret)
            {
                if (!Win32ProcApis.DuplicateTokenEx(_hToken, GENERIC_ALL_ACCESS, ref _sa,
                                      Win32CommonApis.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                                      Win32ProcApis.TOKEN_TYPE.TokenPrimary,
                                      out _hDupedToken))
                {
                    _ret = -2;
                }
            }

            // 以系统模式运行时设置系统账户对当前会话的访问授权
            if ((0 == _ret) && _asSystem && (Process.GetCurrentProcess().SessionId != (int)_sessionID))
            {
                if (!Win32ProcApis.SetTokenInformation(_hDupedToken, Win32ProcApis.TOKEN_INFORMATION_CLASS.TokenSessionId, ref _sessionID, sizeof(uint)))
                {
                    _ret = -3;
                }
            }

            // 创建环境
            if (0 == _ret)
            {
                if (!CreateEnvironmentBlock(out lpEnvironment, _hDupedToken, false))
                {
                    _ret = -4;
                }
            }

            // 创建进程
            if (0 == _ret)
            {
                Win32ProcApis.PROCESS_INFORMATION _pi = new Win32ProcApis.PROCESS_INFORMATION();
                Win32ProcApis.STARTUPINFO _si = new Win32ProcApis.STARTUPINFO();
                _si.cb = Marshal.SizeOf(_si);
                string _cmdLine;
                if (string.IsNullOrWhiteSpace(_args))
                    _cmdLine = _app;
                else
                    _cmdLine = string.Format("{0} {1}", _app, _args);
                if (!Win32ProcApis.CreateProcessAsUser(_hDupedToken, null, _cmdLine, ref _sa, ref _sa, false, 0x430, lpEnvironment, _runPath, ref _si, ref _pi))
                {
                    _ret = -5;
                }
                else
                {
                    _hProc = _pi.hProcess;
                    _hThread = _pi.hThread;
                }
            }

            if (_hDupedToken != IntPtr.Zero)
                Win32CommonApis.CloseHandle(_hDupedToken);

            return _ret;
        }

        /// <summary>
        /// 在当前会话中创建进程
        /// </summary>
        /// <param name="_app">程序名</param>
        /// <param name="_args">程序命令行参数</param>
        /// <param name="_runPath">运行路径</param>
        /// <param name="_asSystem">true以系统权限运行，false以普通权限运行</param>
        /// <param name="_hProc">得到的进程句柄</param>
        /// <param name="_hThread">得到的进程主线程句柄</param>
        /// <returns>0表示成功，非0表示失败</returns>
        public static int CreateProcess(string _app, string _args, string _runPath, bool _asSystem, out IntPtr _hProc, out IntPtr _hThread)
        {
            // 以当前活动会话作为目标会话ID
            uint dwSessionID = (uint)WTSGetActiveConsoleSessionId();
            return CreateProcess(dwSessionID, _app, _args, _runPath, _asSystem, out _hProc, out _hThread);
        }

        /// <summary>
        /// 执行程序
        /// </summary>
        /// <param name="_app">程序</param>
        /// <param name="_cmdline">命令行</param>
        /// <param name="_runPath">运行目录</param>
        /// <param name="_asSystem">是否用系统权限启动</param>
        /// <returns>0表示成功，非0表示失败</returns>
        public static int ExecuteProcess(string _app, string _cmdline, string _runPath, bool _asSystem)
        {
            int _ret;
            IntPtr _hProc, _hThread;

            _ret = CreateProcess(_app, _cmdline, _runPath, _asSystem, out _hProc, out _hThread);
            if (IntPtr.Zero != _hProc)
                Win32CommonApis.CloseHandle(_hProc);
            if (IntPtr.Zero != _hThread)
                Win32CommonApis.CloseHandle(_hThread);

            return _ret;
        }

        /// <summary>
        /// 执行程序
        /// </summary>
        /// <param name="_sessionID">运行程序的目标会话ID</param>
        /// <param name="_app">程序</param>
        /// <param name="_cmdline">命令行</param>
        /// <param name="_runPath">运行目录</param>
        /// <param name="_asSystem">是否用系统权限启动</param>
        /// <returns>0表示成功，非0表示失败</returns>
        public static int ExecuteProcess(uint _sessionID, string _app, string _cmdline, string _runPath, bool _asSystem)
        {
            int _ret;
            IntPtr _hProc, _hThread;

            _ret = CreateProcess(_sessionID, _app, _cmdline, _runPath, _asSystem, out _hProc, out _hThread);
            if (IntPtr.Zero != _hProc)
                Win32CommonApis.CloseHandle(_hProc);
            if (IntPtr.Zero != _hThread)
                Win32CommonApis.CloseHandle(_hThread);

            return _ret;
        }
    }
}
