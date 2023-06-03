using DeeceApi.InternalWorker;
using EasyHook;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessInjection
{
    public class InjectedInternalWorkerApi
    {
        private readonly InternalWorkerCommunication workerApi;
        private readonly BlockingCollection<Task> queue;

        public InjectedInternalWorkerApi(InternalWorkerCommunication workerApi)
        {
            this.workerApi = workerApi;
            queue = new BlockingCollection<Task>();
        }

        /// <summary>
        /// Starts a thread that manages the IPC channel.
        /// </summary>
        /// <param name="lambda"></param>
        public void Start()
        {
            // TODO: Make this cancellable.
            foreach (var lambda in queue.GetConsumingEnumerable(CancellationToken.None))
            {
                lambda?.RunSynchronously();
            }
        }

        public void LogMessage(string message)
        {
            queue.Add(new Task(() => workerApi.LogMessage(message)));
        }

        public string GetFileName(string remoteFileName)
        {
            // Special case CONOUT$, CONIN$:
            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea#consoles
            // In Windows, regular files are forbidden to be named CONIN$ or CONOUT$
            if (remoteFileName == "CONIN$" || remoteFileName == "CONOUT$")
            {
                return remoteFileName;
            }

            int currentPid = RemoteHooking.GetCurrentProcessId();
            int currentTid = RemoteHooking.GetCurrentThreadId();
            return workerApi.RequestFile(currentPid, currentTid, remoteFileName);
        }
    }
}
