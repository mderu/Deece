using System;

namespace DeeceWorkerApi
{
    public class WorkerApi : MarshalByRefObject, IWorkerApi
    {
        public WorkerApi()
        {
        }

        /// <summary>
        /// Logs a message to the console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogMessage(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// A no-op message. Useful for keeping the IPC channel open.
        /// </summary>
        public void Ping()
        {
            return;
        }
    }
}
