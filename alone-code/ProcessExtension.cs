using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace GeneralKit
{
    /// <summary>
    /// Miscellaneous toolkit for process
    /// </summary>
    public abstract class ProcessExtension
    {
        /// <summary>
        /// check if current process is running in administrator authority
        /// </summary>
        /// <returns></returns>
        public static bool IsAdministrator()
        {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static Process StartAsDesktopUser(string _exeFile, string _arguments, string _workDirectory = null)
        {
            IntPtr desktopShellToken = IntPtr.Zero;
            try
            {
                foreach (Process proc in Process.GetProcessesByName("explorer"))
                {
                    using (proc)
                    {
                        Win32ProcApis.OpenProcessToken(proc.Handle, Win32ProcApis.TOKEN_DUPLICATE, out desktopShellToken);
                        break;
                    }
                }

                if (desktopShellToken == IntPtr.Zero)
                {
                    throw new Exception("Cannot open token of the desktop shell");
                }

                IntPtr primaryToken = IntPtr.Zero;
                Win32CommonApis.SECURITY_ATTRIBUTES sa = new Win32CommonApis.SECURITY_ATTRIBUTES();
                sa.Length = (uint)Marshal.SizeOf(sa);
                if (!Win32ProcApis.DuplicateTokenEx(
                        desktopShellToken,
                        Win32ProcApis.GENERIC_ALL_ACCESS,
                        ref sa,
                        Win32CommonApis.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                        Win32ProcApis.TOKEN_TYPE.TokenPrimary,
                        out primaryToken))
                {
                    throw new Exception($"DuplicateTokenEx Error: {Marshal.GetLastWin32Error()}");
                }

                try
                {
                    string cmdline;
                    if (string.IsNullOrWhiteSpace(_arguments))
                    {
                        cmdline = $"\"{_exeFile}\"";
                    }
                    else
                    {
                        cmdline = $"\"{_exeFile}\" {_arguments}";
                    }
                    Win32ProcApis.PROCESS_INFORMATION pi = new Win32ProcApis.PROCESS_INFORMATION();
                    Win32ProcApis.STARTUPINFO si = new Win32ProcApis.STARTUPINFO();
                    si.cb = Marshal.SizeOf(si);
                    if (!Win32ProcApis.CreateProcessWithTokenW(
                            primaryToken,
                            Win32ProcApis.LOGON_WITH_PROFILE,
                            null,
                            cmdline,
                            Win32ProcApis.NORMAL_PRIORITY_CLASS | Win32ProcApis.CREATE_UNICODE_ENVIRONMENT,
                            IntPtr.Zero,
                            _workDirectory,
                            ref si,
                            ref pi))
                    {
                        throw new Exception($"CreateProcessAsUser Error: {Marshal.GetLastWin32Error()}");
                    }

                    Process targetProc = Process.GetProcessById(pi.dwProcessID);

                    Win32CommonApis.CloseHandle(pi.hProcess);
                    Win32CommonApis.CloseHandle(pi.hThread);

                    return targetProc;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    Win32CommonApis.CloseHandle(primaryToken);
                }
            }
            catch (Exception err)
            {
                Trace.WriteLine(err.ToString());
                return null;
            }
            finally
            {
                if (IntPtr.Zero != desktopShellToken)
                {
                    Win32CommonApis.CloseHandle(desktopShellToken);
                }
            }
        }

        public static Process StartAsAdministrator(string _fileName, string _argument, string _workDir)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = _fileName;
                if (!string.IsNullOrWhiteSpace(_argument))
                    startInfo.Arguments = _argument;
                if (!string.IsNullOrWhiteSpace(_workDir))
                    startInfo.WorkingDirectory = _workDir;
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                //如果不是管理员，则启动UAC 
                return Process.Start(startInfo);
            }
            catch (Exception err)
            {
                TraceOut.Print("Cannot start process as adnimistrator({0})", err.Message);
                return null;
            }
        }

        public static bool RunAsAdministrator(string _fileName, string _argument, string _workDir)
        {
            try
            {
                using (Process proc = StartAsAdministrator(_fileName, _argument, _workDir))
                {
                    return (proc != null);
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool RunAsAdministrator(string _szCmdLine)
        {
            try
            {
                int cmdLen = _szCmdLine.Length;
                int paramPos = 0;
                bool InDot = false;
                for (; paramPos < cmdLen; paramPos++)
                {
                    char item = _szCmdLine[paramPos];
                    if (char.IsWhiteSpace(item) && (!InDot))
                    {
                        break;
                    }
                    else if (item == '\"')
                    {
                        InDot = !InDot;
                    }
                }
                string appPath = _szCmdLine.Substring(0, paramPos);
                string appArgs = _szCmdLine.Substring(paramPos);

                return RunAsAdministrator(appPath, appArgs, null);
            }
            catch (Exception err)
            {
                TraceOut.Print("Cannot start process as adnimistrator({0})", err.Message);
                return false;
            }
        }
    }
}
