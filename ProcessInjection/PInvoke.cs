using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ProcessInjection.Win32
{
    /// <summary>
    /// A class that wraps the necessary PInvoke functions ProcessInjection uses to virtualize
    /// the a process remotely.
    /// </summary>
    public static class PInvoke
    {
        /// <summary>
        /// The delegate used to hook
        /// <see cref="CreateFileW(string, uint, uint, IntPtr, uint, uint, IntPtr)"/>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall,
                    CharSet = CharSet.Unicode,
                    SetLastError = true)]
        public delegate IntPtr DCreateFile(
            [In] string lpFileName,
            [In] uint dwDesiredAccess,
            [In] uint dwShareMode,
            [In, Optional] IntPtr lpSecurityAttributes,
            [In] uint dwCreationDisposition,
            [In] uint dwFlagsAndAttributes,
            [In, Optional] IntPtr hTemplateFile);

        /// <summary>
        /// Using P/Invoke to call original method.
        /// https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew.
        /// For more information, see https://www.pinvoke.net/default.aspx/kernel32.CreateFile.
        /// </summary>
        [DllImport("kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateFileW(
            [In] string lpFileName,
            [In] uint dwDesiredAccess,
            [In] uint dwShareMode,
            [In, Optional] IntPtr lpSecurityAttributes,
            [In] uint dwCreationDisposition,
            [In] uint dwFlagsAndAttributes,
            [In, Optional] IntPtr hTemplateFile);

        /// <summary>
        /// The delegate used to hook <see cref="WriteFile(IntPtr, IntPtr, uint, out uint, IntPtr)"/>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool DWriteFile(
            [In] IntPtr hFile,
            [In] IntPtr lpBuffer,
            [In] uint nNumberOfBytesToWrite,
            [Out, Optional] out uint lpNumberOfBytesWritten,
            [In, Out, Optional] IntPtr lpOverlapped);

        /// <summary>
        /// Using P/Invoke to call original WriteFile method. See
        /// https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-writefile.
        /// For more information, see https://www.pinvoke.net/default.aspx/kernel32/WriteFile.html
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(
            [In] IntPtr hFile,
            [In] IntPtr lpBuffer,
            [In] uint nNumberOfBytesToWrite,
            [Out, Optional] out uint lpNumberOfBytesWritten,
            [In, Out, Optional] IntPtr lpOverlapped);

        /// <summary>
        /// P/Invoke to determine the filename from a file handle
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa364962(v=vs.85).aspx
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpszFilePath"></param>
        /// <param name="cchFilePath"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetFinalPathNameByHandleW(
            [In] IntPtr hFile,
            [Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath,
            [In] uint cchFilePath,
            [In] uint dwFlags);

        /// <summary>
        /// The delegate used to hook
        /// <see cref="CreateProcessW(string, string, ref SECURITY_ATTRIBUTES, ref SECURITY_ATTRIBUTES, bool, uint, IntPtr, string, ref STARTUPINFOEX, out PROCESS_INFORMATION)"/>.
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nNumberOfBytesToWrite"></param>
        /// <param name="lpNumberOfBytesWritten"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool DCreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFOEX lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.SysUInt)]
        static extern uint ResumeThread(IntPtr hThread);

        /// <summary>
        /// Using P/Invoke to call original CreateProcessW method. See
        /// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw
        /// For more info, see https://www.pinvoke.net/default.aspx/kernel32/CreateProcess.html
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFOEX lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    public static class DwCreationFlags
    {
        public static uint CREATE_SUSPENDED = 0x00000004U;
    }

    public enum DwCreationDisposition
    {
        CREATE_ALWAYS = 2,
        CREATE_NEW = 1,
        OPEN_ALWAYS = 4,
        OPEN_EXISTING = 3,
        TRUNCATE_EXISTING = 5,
    }
}
