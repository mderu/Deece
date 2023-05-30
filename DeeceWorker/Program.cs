using DeeceApi.InternalWorker;
using EasyHook;
using System;
using System.IO;
using System.Runtime.Remoting;

namespace DeeceWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Will contain the name of the IPC server channel
            string channelName = null;
            int childPid = 0;

            RemoteHooking.IpcCreateServer<InternalWorkerApi>(ref channelName, WellKnownObjectMode.Singleton);

            // Get the full path to the assembly we want to inject into the target process
            string injectionLibrary = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "ProcessInjection.dll");

            try
            {
                string fileToRead = "C:/Windows/System32/WindowsPowerShell/v1.0/en-US/default.help.txt";

                // TODO: Redirect stdout/stderr to a file.
                RemoteHooking.CreateAndInject(
                    InEXEPath: "C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe",
                    InCommandLine: $"Powershell Powershell Get-Content \"{fileToRead}\"",
                    InProcessCreationFlags: 0,
                    InOptions: InjectionOptions.DoNotRequireStrongName,
                    InLibraryPath_x86: injectionLibrary,
                    InLibraryPath_x64: injectionLibrary,
                    OutProcessId: out childPid,
                    channelName);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("There was an error while injecting into target:");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.ToString());
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("<Press any key to exit>");
            Console.ResetColor();
            Console.ReadKey();
        }
    }
}
