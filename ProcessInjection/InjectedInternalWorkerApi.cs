using DeeceApi.InternalWorker;
using EasyHook;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessInjection
{
    public class InjectedInternalWorkerApi
    {
        class Pointer<T>
        {
            public T Value { get; set; }
        }

        private readonly InternalWorkerApi workerApi;
        private readonly BlockingCollection<Task> queue;

        public InjectedInternalWorkerApi(InternalWorkerApi workerApi)
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

        /// <inheritdoc/>
        public void LogMessage(string message)
        {
            queue.Add(new Task(() => workerApi.LogMessage(message)));
        }

        /// <inheritdoc/>
        public string GetFileName(string remoteFileName)
        {
            int currentPid = RemoteHooking.GetCurrentProcessId();
            int currentTid = RemoteHooking.GetCurrentThreadId();
            return workerApi.GetFileName(remoteFileName, currentPid, currentTid);
        }
    }
}
