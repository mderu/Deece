using System;
using System.IO;

namespace DeeceApi.InternalWorker
{
    /// <summary>
    /// The API between the DeeceWorker and the Injected DLLs.
    /// 
    /// This API does not expose any methods to the client to call upon.
    /// </summary>
    public class InternalWorkerApi : MarshalByRefObject, IInternalWorkerApi
    {
        public InternalWorkerApi()
        {
        }

        /// <summary>
        /// Returns the local file name for the given remote file name.
        /// </summary>
        /// <param name="remoteFileName">The path that exists on the original filesystem.</param>
        /// <param name="pid">The PID of the process requesting this file.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetFileName(string remoteFileName, int pid)
        {
            // Special case CONOUT$, CONIN$:
            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea#consoles
            // In Windows, regular files are forbidden to be named CONIN$ or CONOUT$
            if (remoteFileName == "CONIN$" || remoteFileName == "CONOUT$")
            {
                return remoteFileName;
            }

            // TODO: Reach out to the client instead.
            if (pid != 0)
            {
                // TODO: Create an actual lookup table.
                string relativePath = new Uri("C:/").MakeRelativeUri(new Uri(remoteFileName)).ToString();
                string newPath = Path.Combine("D:/Temp/", relativePath);
                return newPath;
            }

            throw new Exception("Not sure how we got here");
        }

        /// <summary>
        /// Logs a message to the console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
