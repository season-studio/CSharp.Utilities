using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GeneralKit
{
    /// <summary>
    /// 服务控制API
    /// </summary>
    public static class ServiceCtrlAPI
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern IntPtr OpenSCManager(string lpMachineName, string lpSCDB, int scParameter);
        [DllImport("Advapi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateService(IntPtr SC_HANDLE, string lpSvcName, string lpDisplayName,
                                                  int dwDesiredAccess, int dwServiceType, int dwStartType, int dwErrorControl, string lpPathName,
                                                  string lpLoadOrderGroup, int lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern void CloseServiceHandle(IntPtr SCHANDLE);
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool StartService(IntPtr SVHANDLE, int dwNumServiceArgs, string lpServiceArgVectors);
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern IntPtr OpenService(IntPtr SCHANDLE, string lpSvcName, int dwNumServiceArgs);
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool DeleteService(IntPtr SVHANDLE);
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool QueryServiceStatusEx(IntPtr hService, uint InfoLevel, ref SERVICE_STATUS_PROCESS lpBuf, int cbBufSize, out int pcbBytesNeeded);
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool ControlService(IntPtr hService, uint dwControl, out SERVICE_STATUS lpServiceStatus);
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern bool QueryServiceConfig(IntPtr hService, IntPtr QueryServiceConfig, int cbBufSize, out int pcbBytesNeeded);

        internal const int SC_MANAGER_CREATE_SERVICE = 0x0002;
        internal const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
        internal const int SERVICE_INTERACTIVE_PROCESS = 0x00000100;
        internal const int SERVICE_DEMAND_START = 0x00000003;
        internal const int SERVICE_ERROR_NORMAL = 0x00000001;
        internal const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        internal const int SERVICE_QUERY_CONFIG = 0x0001;
        internal const int SERVICE_CHANGE_CONFIG = 0x0002;
        internal const int SERVICE_QUERY_STATUS = 0x0004;
        internal const int SERVICE_ENUMERATE_DEPENDENTS = 0x0008;
        internal const int SERVICE_START = 0x0010;
        internal const int SERVICE_STOP = 0x0020;
        internal const int SERVICE_PAUSE_CONTINUE = 0x0040;
        internal const int SERVICE_INTERROGATE = 0x0080;
        internal const int SERVICE_USER_DEFINED_CONTROL = 0x0100;
        internal const int SERVICE_ALL_ACCESS = 0xF003F;
        internal const int SERVICE_AUTO_START = 0x00000002;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct SERVICE_STATUS_PROCESS
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
            public uint dwProcessId;
            public uint dwServiceFlags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct SERVICE_STATUS
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class QUERY_SERVICE_CONFIG
        {
            public int dwServiceType;
            public int dwStartType;
            public int dwErrorControl;
            public IntPtr lpBinaryPathName;
            public IntPtr lpLoadOrderGroup;
            public int dwTagId;
            public IntPtr lpDependencies;
            public IntPtr lpServiceStartName;
            public IntPtr lpDisplayName;
        }

        internal const uint SC_STATUS_PROCESS_INFO = 0;

        /// <summary>
        /// 服务已停止
        /// </summary>
        public const uint SERVICE_STOPPED = 0x00000001;
        /// <summary>
        /// 服务正在启动
        /// </summary>
        public const uint SERVICE_START_PENDING = 0x00000002;
        /// <summary>
        /// 服务正在停止
        /// </summary>
        public const uint SERVICE_STOP_PENDING = 0x00000003;
        /// <summary>
        /// 服务正在运行
        /// </summary>
        public const uint SERVICE_RUNNING = 0x00000004;
        /// <summary>
        /// 服务正在恢复
        /// </summary>
        public const uint SERVICE_CONTINUE_PENDING = 0x00000005;
        /// <summary>
        /// 服务正在暂停
        /// </summary>
        public const uint SERVICE_PAUSE_PENDING = 0x00000006;
        /// <summary>
        /// 服务暂停
        /// </summary>
        public const uint SERVICE_PAUSED = 0x00000007;
        /// <summary>
        /// 服务状态未知
        /// </summary>
        public const uint SERVICE_UNKNOWN_STATE = uint.MaxValue;

        internal const uint SERVICE_CONTROL_STOP = 0x00000001;
        internal const uint SERVICE_CONTROL_PAUSE = 0x00000002;
        internal const uint SERVICE_CONTROL_CONTINUE = 0x00000003;
        internal const uint SERVICE_CONTROL_INTERROGATE = 0x00000004;
        internal const uint SERVICE_CONTROL_SHUTDOWN = 0x00000005;
        internal const uint SERVICE_CONTROL_PARAMCHANGE = 0x00000006;
        internal const uint SERVICE_CONTROL_NETBINDADD = 0x00000007;
        internal const uint SERVICE_CONTROL_NETBINDREMOVE = 0x00000008;
        internal const uint SERVICE_CONTROL_NETBINDENABLE = 0x00000009;
        internal const uint SERVICE_CONTROL_NETBINDDISABLE = 0x0000000A;
        internal const uint SERVICE_CONTROL_DEVICEEVENT = 0x0000000B;
        internal const uint SERVICE_CONTROL_HARDWAREPROFILECHANGE = 0x0000000C;
        internal const uint SERVICE_CONTROL_POWEREVENT = 0x0000000D;
        internal const uint SERVICE_CONTROL_SESSIONCHANGE = 0x0000000E;
        internal const uint SERVICE_CONTROL_PRESHUTDOWN = 0x0000000F;
        internal const uint SERVICE_CONTROL_TIMECHANGE = 0x00000010;
        internal const uint SERVICE_CONTROL_TRIGGEREVENT = 0x00000020;
    }

    /// <summary>
    /// 服务管理类
    /// </summary>
    public class ServiceManager : IDisposable
    {
        /// <summary>
        /// 存储服务配置信息的结构体
        /// </summary>
        public struct Config
        {
            /// <summary>
            /// 服务类型
            /// </summary>
            public int ServiceType;
            /// <summary>
            /// 启动类型
            /// </summary>
            public int StartType;
            /// <summary>
            /// 错误控制
            /// </summary>
            public int ErrorControl;
            /// <summary>
            /// 二进制路径
            /// </summary>
            public string BinaryPathName;
            /// <summary>
            /// 加载组
            /// </summary>
            public string LoadOrderGroup;
            /// <summary>
            /// 标签ID
            /// </summary>
            public int TagId;
            /// <summary>
            /// 依赖
            /// </summary>
            public string Dependencies;
            /// <summary>
            /// 服务启动名
            /// </summary>
            public string ServiceStartName;
            /// <summary>
            /// 服务显示名
            /// </summary>
            public string DisplayName;
        }

        /// <summary>
        /// 管理器的句柄
        /// </summary>
        private IntPtr ManagerHandler;

        /// <summary>
        /// 服务的句柄
        /// </summary>
        private IntPtr ServiceHandler;

        /// <summary>
        /// 构造
        /// </summary>
        public ServiceManager(bool _onlyQuery = true)
        {
            ManagerHandler = ServiceCtrlAPI.OpenSCManager(null, null, (_onlyQuery ? ServiceCtrlAPI.SERVICE_QUERY_STATUS : ServiceCtrlAPI.SERVICE_ALL_ACCESS));
            ServiceHandler = IntPtr.Zero;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            CloseService();
            if (ManagerHandler != IntPtr.Zero)
                ServiceCtrlAPI.CloseServiceHandle(ManagerHandler);
        }

        /// <summary>
        /// 最后的错误消息
        /// </summary>
        public string LastErrorMessage
        {
            get
            {
                int ErrCode = Marshal.GetLastWin32Error();
                if (ErrCode == 0)
                    return string.Empty;
                else
                    return (new System.ComponentModel.Win32Exception(ErrCode)).Message;
            }
        }

        /// <summary>
        /// 检查管理器是否有效
        /// </summary>
        public bool IsManagerValid { get { return ManagerHandler != IntPtr.Zero; } }

        /// <summary>
        /// 检查服务是否有效
        /// </summary>
        public bool IsServiceOpened { get { return ServiceHandler != IntPtr.Zero; } }

        /// <summary>
        /// 清理已打开的服务
        /// </summary>
        public void CloseService()
        {
            if (ServiceHandler != IntPtr.Zero)
            {
                ServiceCtrlAPI.CloseServiceHandle(ServiceHandler);
                ServiceHandler = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 打开服务，之前已打开的服务会被关闭
        /// </summary>
        /// <param name="ServiceName">服务名称</param>
        /// <returns>True 服务已打开，False 服务无法打开</returns>
        public bool OpenService(string ServiceName)
        {
            CloseService();
            if (IsManagerValid)
            {
                ServiceHandler = ServiceCtrlAPI.OpenService(ManagerHandler, ServiceName, ServiceCtrlAPI.SERVICE_ALL_ACCESS);
            }

            return IsServiceOpened;
        }

        /// <summary>
        /// 打开服务，如果服务已被打开，则不会重复打开
        /// </summary>
        /// <param name="ServiceName">服务名称</param>
        /// <param name="_onlyQuery">是否只为查询状态而打开</param>
        /// <returns>True 服务已打开，False 服务无法打开</returns>
        public bool CheckAndOpenService(string ServiceName, bool _onlyQuery = true)
        {
            if (!IsServiceOpened)
            {
                if (IsManagerValid)
                {
                    ServiceHandler = ServiceCtrlAPI.OpenService(ManagerHandler, ServiceName, (_onlyQuery ? ServiceCtrlAPI.SERVICE_QUERY_STATUS : ServiceCtrlAPI.SERVICE_ALL_ACCESS));
                }
            }

            return IsServiceOpened;
        }

        /// <summary>
        /// 获取服务的当前状态
        /// </summary>
        public uint ServiceState
        {
            get
            {
                uint ret = ServiceCtrlAPI.SERVICE_UNKNOWN_STATE;

                if (IsServiceOpened)
                {
                    ServiceCtrlAPI.SERVICE_STATUS_PROCESS state = new ServiceCtrlAPI.SERVICE_STATUS_PROCESS();
                    int NeedBufSize;
                    if (ServiceCtrlAPI.QueryServiceStatusEx(ServiceHandler, ServiceCtrlAPI.SC_STATUS_PROCESS_INFO, ref state, Marshal.SizeOf(state), out NeedBufSize))
                    {
                        ret = state.dwCurrentState;
                    }
                }

                return ret;
            }
        }

        /// <summary>
        /// 等待服务到达某个状态
        /// </summary>
        /// <param name="State">要等待的状态</param>
        /// <param name="MaxWaitTimeMS">最大等待的毫秒数，如果为0，表示不设置超时</param>
        /// <returns>True 状态已达到，False 超时</returns>
        public Task<bool> WaitForServiceState(uint State, uint MaxWaitTimeMS = 0)
        {
            return Task.Run(() =>
            {
                bool ret = false;
                int StartTick = Environment.TickCount;

                if (IsServiceOpened)
                {
                    while (true)
                    {
                        if (ServiceState == State)
                        {
                            ret = true;
                            break;
                        }
                        else if (MaxWaitTimeMS > 0)
                        {
                            int CurTick = Environment.TickCount;
                            uint DeltaTick = (uint)(CurTick - StartTick);
                            if (DeltaTick >= MaxWaitTimeMS)
                                break;
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                }

                return ret;
            });
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="IsWait">True 表示等待启动完成，False表示不等待</param>
        /// <param name="MaxWaitTimeMS">最大等待的毫秒数，如果为0，表示不设置超时</param>
        /// <returns>True 操作成功，False 操作失败</returns>
        public Task<bool> StartService(bool IsWait = true, uint MaxWaitTimeMS = 0)
        {
            return Task.Run(async () =>
            {
                bool ret = false;

                if (IsServiceOpened)
                {
                    uint state = ServiceState;
                    if (state != ServiceCtrlAPI.SERVICE_RUNNING)
                    {
                        ret = ServiceCtrlAPI.StartService(ServiceHandler, 0, null);
                        if (ret && IsWait)
                        {
                            ret = await WaitForServiceState(ServiceCtrlAPI.SERVICE_RUNNING, MaxWaitTimeMS);
                        }
                    }
                }

                return ret;
            });
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <param name="IsWait">True 表示等待启动完成，False表示不等待</param>
        /// <param name="MaxWaitTimeMS">最大等待的毫秒数，如果为0，表示不设置超时</param>
        /// <returns>True 操作成功，False 操作失败</returns>
        public Task<bool> StopService(bool IsWait = true, uint MaxWaitTimeMS = 0)
        {
            return Task.Run(async () =>
            {
                bool ret = false;

                if (IsServiceOpened)
                {
                    uint state = ServiceState;
                    if ((state != ServiceCtrlAPI.SERVICE_STOPPED) && (state != ServiceCtrlAPI.SERVICE_STOP_PENDING))
                    {
                        ServiceCtrlAPI.SERVICE_STATUS lastState;
                        ret = ServiceCtrlAPI.ControlService(ServiceHandler, ServiceCtrlAPI.SERVICE_CONTROL_STOP, out lastState);
                        if (ret && IsWait)
                        {
                            ret = await WaitForServiceState(ServiceCtrlAPI.SERVICE_STOPPED, MaxWaitTimeMS);
                        }
                    }
                }

                return ret;
            });
        }

        /// <summary>
        /// 暂停服务
        /// </summary>
        /// <param name="IsWait">True 表示等待启动完成，False表示不等待</param>
        /// <param name="MaxWaitTimeMS">最大等待的毫秒数，如果为0，表示不设置超时</param>
        /// <returns>True 操作成功，False 操作失败</returns>
        public Task<bool> PauseService(bool IsWait = true, uint MaxWaitTimeMS = 0)
        {
            return Task.Run(async () =>
            {
                bool ret = false;

                if (IsServiceOpened)
                {
                    uint state = ServiceState;
                    if (state == ServiceCtrlAPI.SERVICE_RUNNING)
                    {
                        ServiceCtrlAPI.SERVICE_STATUS lastState;
                        ret = ServiceCtrlAPI.ControlService(ServiceHandler, ServiceCtrlAPI.SERVICE_CONTROL_PAUSE, out lastState);
                        if (ret && IsWait)
                        {
                            ret = await WaitForServiceState(ServiceCtrlAPI.SERVICE_PAUSED, MaxWaitTimeMS);
                        }
                    }
                }

                return ret;
            });
        }

        /// <summary>
        /// 继续被暂停的服务
        /// </summary>
        /// <param name="IsWait">True 表示等待启动完成，False表示不等待</param>
        /// <param name="MaxWaitTimeMS">最大等待的毫秒数，如果为0，表示不设置超时</param>
        /// <returns>True 操作成功，False 操作失败</returns>
        public Task<bool> ContinueService(bool IsWait = true, uint MaxWaitTimeMS = 0)
        {
            return Task.Run(async () =>
            {
                bool ret = false;

                if (IsServiceOpened)
                {
                    uint state = ServiceState;
                    if ((state == ServiceCtrlAPI.SERVICE_PAUSED) && (state != ServiceCtrlAPI.SERVICE_PAUSE_PENDING))
                    {
                        ServiceCtrlAPI.SERVICE_STATUS lastState;
                        ret = ServiceCtrlAPI.ControlService(ServiceHandler, ServiceCtrlAPI.SERVICE_CONTROL_CONTINUE, out lastState);
                        if (ret && IsWait)
                        {
                            ret = await WaitForServiceState(ServiceCtrlAPI.SERVICE_RUNNING, MaxWaitTimeMS);
                        }
                    }
                }

                return ret;
            });
        }

        /// <summary>
        /// 安装一个服务
        /// </summary>
        /// <param name="svcPath">服务的binPath参数</param>
        /// <param name="svcName">服务的名称</param>
        /// <param name="svcDispName">服务的显示名称</param>
        /// <param name="DependSvc">依赖的服务</param>
        /// <param name="_isAutoStart">是否自动启动</param>
        /// <param name="_isInteractive">是否与桌面交互</param>
        /// <returns>true安装成功，false安装失败</returns>
        public bool InstallService(string svcPath, string svcName, string svcDispName, string DependSvc, bool _isAutoStart = false, bool _isInteractive = false)
        {
            CloseService();
            if (IsManagerValid)
            {
                int _svcType = _isInteractive ? (ServiceCtrlAPI.SERVICE_WIN32_OWN_PROCESS | ServiceCtrlAPI.SERVICE_INTERACTIVE_PROCESS) : (ServiceCtrlAPI.SERVICE_WIN32_OWN_PROCESS);
                int _startType = _isAutoStart ? ServiceCtrlAPI.SERVICE_AUTO_START : ServiceCtrlAPI.SERVICE_DEMAND_START;
                ServiceHandler = ServiceCtrlAPI.CreateService(ManagerHandler, svcName, svcDispName,
                                                     ServiceCtrlAPI.SERVICE_ALL_ACCESS, _svcType, _startType, ServiceCtrlAPI.SERVICE_ERROR_NORMAL,
                                                     svcPath, null, 0, DependSvc, null, null);
            }

            return IsServiceOpened;
        }

        /// <summary>
        /// 卸载服务
        /// </summary>
        /// <param name="svcName">要卸载的服务名称</param>
        /// <returns>True 卸载成功，False 卸载失败</returns>
        public bool UninstallService(string svcName)
        {
            bool ret = false;

            if (IsManagerValid)
            {
                if (OpenService(svcName))
                {
                    StopService(true);
                    ret = ServiceCtrlAPI.DeleteService(ServiceHandler);
                    CloseService();
                }
            }

            return ret;
        }

        /// <summary>
        /// 获取服务的配置
        /// </summary>
        /// <param name="Config">存储配置信息的结构体</param>
        public void GetServiceConfig(out Config Config)
        {
            Config cfg = new Config();
            cfg.ServiceType = 0;
            cfg.StartType = 0;
            cfg.ErrorControl = 0;
            cfg.BinaryPathName = string.Empty;
            cfg.LoadOrderGroup = string.Empty;
            cfg.TagId = 0;
            cfg.Dependencies = string.Empty;
            cfg.ServiceStartName = string.Empty;
            cfg.DisplayName = string.Empty;
            if (IsServiceOpened)
            {
                IntPtr Buf;
                int NeedSize;
                ServiceCtrlAPI.QueryServiceConfig(ServiceHandler, IntPtr.Zero, 0, out NeedSize);
                Buf = Marshal.AllocHGlobal(NeedSize);
                if (Buf != IntPtr.Zero)
                {
                    if (ServiceCtrlAPI.QueryServiceConfig(ServiceHandler, Buf, NeedSize, out NeedSize))
                    {
                        ServiceCtrlAPI.QUERY_SERVICE_CONFIG NativeCfg = new ServiceCtrlAPI.QUERY_SERVICE_CONFIG();
                        Marshal.PtrToStructure(Buf, NativeCfg);
                        cfg.ServiceType = NativeCfg.dwServiceType;
                        cfg.StartType = NativeCfg.dwStartType;
                        cfg.ErrorControl = NativeCfg.dwErrorControl;
                        cfg.BinaryPathName = (NativeCfg.lpBinaryPathName != IntPtr.Zero) ? Marshal.PtrToStringAnsi(NativeCfg.lpBinaryPathName) : string.Empty;
                        cfg.LoadOrderGroup = (NativeCfg.lpLoadOrderGroup != IntPtr.Zero) ? Marshal.PtrToStringAnsi(NativeCfg.lpLoadOrderGroup) : string.Empty;
                        cfg.TagId = NativeCfg.dwTagId;
                        cfg.Dependencies = (NativeCfg.lpDependencies != IntPtr.Zero) ? Marshal.PtrToStringAnsi(NativeCfg.lpDependencies) : string.Empty;
                        cfg.ServiceStartName = (NativeCfg.lpServiceStartName != IntPtr.Zero) ? Marshal.PtrToStringAnsi(NativeCfg.lpServiceStartName) : string.Empty;
                        cfg.DisplayName = (NativeCfg.lpDisplayName != IntPtr.Zero) ? Marshal.PtrToStringAnsi(NativeCfg.lpDisplayName) : string.Empty;
                    }
                    Marshal.FreeHGlobal(Buf);
                }
            }
            Config = cfg;
        }

    }
}
