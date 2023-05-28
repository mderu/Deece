using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DeeceWorkerApi;
using EasyHook;
using ProcessInjection.Win32;

namespace ProcessInjection
{
    /// <summary>
    /// The Injection entry point. This is created and executed within the target process
    /// when Inject is called from the main worker thread.
    /// </summary>
    public class InjectionEntryPoint : IEntryPoint
    {
        private readonly InjectedWorkerApi workerApi;
        private readonly string ipcChannelName;

        public InjectionEntryPoint(RemoteHooking.IContext _, string ipcChannelName)
        {
            workerApi = new InjectedWorkerApi(RemoteHooking.IpcConnectClient<WorkerApi>(ipcChannelName));
            this.ipcChannelName = ipcChannelName;
            workerApi.Ping();
        }

        public void Run(RemoteHooking.IContext _, string __)
        {
            var createFileHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "CreateFileW"),
                new PInvoke.DCreateFile(CreateFile_Hook),
                this);
            var writeFileHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "WriteFile"),
                new PInvoke.DWriteFile(WriteFile_Hook),
                this);
            var createProcessHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "CreateProcessW"),
                new PInvoke.DCreateProcess(CreateProcess_Hook),
                this);

            // Don't capture API calls from this thread.
            createFileHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
            writeFileHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
            createProcessHook.ThreadACL.SetExclusiveACL(new int[] { 0 });

            // Wake up the process if it isn't awake already.
            RemoteHooking.WakeUpProcess();

            workerApi.LogMessage(Assembly.GetAssembly(GetType()).Location);

            try
            {
                workerApi.Start();
            }
            finally
            {
                createFileHook.Dispose();
                writeFileHook.Dispose();
                createProcessHook.Dispose();

                // Finalise cleanup of hooks
                LocalHook.Release();
            }
        }

        public IntPtr CreateFile_Hook(
            [In] string lpFileName,
            [In] uint dwDesiredAccess,
            [In] uint dwShareMode,
            [In, Optional] IntPtr lpSecurityAttributes,
            [In] uint dwCreationDisposition,
            [In] uint dwFlagsAndAttributes,
            [In, Optional] IntPtr hTemplateFile)
        {
            string mode = ((DwCreationDisposition)dwCreationDisposition).ToString();

            var retValue = PInvoke.CreateFileW(
                lpFileName,
                dwDesiredAccess,
                dwShareMode,
                lpSecurityAttributes,
                dwCreationDisposition,
                dwFlagsAndAttributes,
                hTemplateFile);

            workerApi.LogMessage(
                $"[{RemoteHooking.GetCurrentProcessId()}, {RemoteHooking.GetCurrentThreadId()}] " +
                $"[CreateFile({mode})] " +
                $"[Handle: {retValue}] " +
                $"{lpFileName}");

            return retValue;
        }

        public bool WriteFile_Hook(
            [In] IntPtr hFile,
            [In] IntPtr lpBuffer,
            [In] uint nNumberOfBytesToWrite,
            [Out, Optional] out uint lpNumberOfBytesWritten,
            [In, Out, Optional] IntPtr lpOverlapped)
        {
            // Call original first so we get lpNumberOfBytesWritten
            bool result = PInvoke.WriteFile(
                hFile,
                lpBuffer,
                nNumberOfBytesToWrite,
                out lpNumberOfBytesWritten,
                lpOverlapped);

            StringBuilder filename = new StringBuilder(255);
            PInvoke.GetFinalPathNameByHandleW(hFile, filename, 255, 0);

            workerApi.LogMessage(
                $"[{RemoteHooking.GetCurrentProcessId()}, {RemoteHooking.GetCurrentThreadId()}] " +
                $"[WriteFile] " +
                $"[Handle: {hFile}] " +
                $"[Bytes Written: {lpNumberOfBytesWritten}] " +
                $"{filename}");

            return result;
        }

        public bool CreateProcess_Hook(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFOEX lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation)
        {
            // TODO: The process should stay suspended iff the original execution called for it to be suspended.
            // Otherwise, the injection should unsuspend the process.
            bool leaveSuspended = (dwCreationFlags & DwCreationFlags.CREATE_SUSPENDED) > 0;

            lpStartupInfo.StartupInfo.wShowWindow = 1;

            // Start the process suspended
            bool result = PInvoke.CreateProcess(
                lpApplicationName,
                lpCommandLine,
                ref lpProcessAttributes,
                ref lpThreadAttributes,
                bInheritHandles,
                dwCreationFlags | DwCreationFlags.CREATE_SUSPENDED,
                lpEnvironment,
                lpCurrentDirectory,
                ref lpStartupInfo,
                out lpProcessInformation);

            string dllFullPath = Assembly.GetAssembly(GetType()).Location;

            // TODO: Expose this through the actual API instead of reflection.
            MethodInfo dynMethod = typeof(RemoteHooking).GetMethod("InjectEx", BindingFlags.NonPublic | BindingFlags.Static);
            dynMethod.Invoke(null, new object[] {
                NativeAPI.GetCurrentProcessId(),
                lpProcessInformation.dwProcessId,
                lpProcessInformation.dwThreadId,
                0x20000000,
                dllFullPath,
                dllFullPath,
                /*InjectionOptions.NoWOW64Bypass*/ false,
                /*InjectionOptions.NoService*/ true,
                /*InjectionOptions.DoNotRequireStrongName*/ true,
                new object[] { ipcChannelName }
            });

            try
            {
                workerApi.LogMessage(
                    $"[{RemoteHooking.GetCurrentProcessId()}, {RemoteHooking.GetCurrentThreadId()}] " +
                    $"[CreateProcess] " +
                    $"{lpApplicationName} {lpCommandLine}");
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return result;
        }
    }
}