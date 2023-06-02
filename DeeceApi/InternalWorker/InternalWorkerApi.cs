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
        // TODO: Have the function that makes one of these return an InternalWorkerApi.
        public static InternalWorkerApi singleton = null;

        public string Channel { get; private set; }
        public InternalWorkerApi()
        {
            if (singleton != null)
            {
                throw new InvalidOperationException("Cannot create another InternalWorkerApi");
            }
            singleton = this;
        }

        /// <summary>
        /// Returns the local file name for the given remote file name.
        /// </summary>
        /// <param name="remoteFileName">The path that exists on the original filesystem.</param>
        /// <param name="pid">The PID of the process requesting this file.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetFileName(string remoteFileName, int pid, int tid)
        {
            // Special case CONOUT$, CONIN$:
            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea#consoles
            // In Windows, regular files are forbidden to be named CONIN$ or CONOUT$
            if (remoteFileName == "CONIN$" || remoteFileName == "CONOUT$")
            {
                return remoteFileName;
            }

            string newFile = InternalWorkerCommunication.Instance.RequestFile(pid, tid, remoteFileName);

            return newFile;
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
