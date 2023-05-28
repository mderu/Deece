using DeeceWorkerApi;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ProcessInjection
{
    public class InjectedWorkerApi : IWorkerApi
    {
        private readonly WorkerApi workerApi;
        private readonly ConcurrentQueue<Action> queue;

        public InjectedWorkerApi(WorkerApi workerApi)
        {
            this.workerApi = workerApi;
            queue = new ConcurrentQueue<Action>();
        }

        /// <summary>
        /// Starts a thread that manages the IPC channel.
        /// </summary>
        /// <param name="lambda"></param>
        public void Start()
        {
            while (true)
            {
                // TODO: Use a better API that supports pushes instead of polling.
                Thread.Sleep(500);

                while (queue.TryDequeue(out Action lambda))
                {
                    lambda?.Invoke();
                }
                workerApi.Ping();
            }
        }

        /// <inheritdoc/>
        public void LogMessage(string message)
        {
            queue.Enqueue(() => workerApi.LogMessage(message));
        }

        /// <inheritdoc/>
        public void Ping()
        {
            queue.Enqueue(() => workerApi.Ping());
        }
    }
}
