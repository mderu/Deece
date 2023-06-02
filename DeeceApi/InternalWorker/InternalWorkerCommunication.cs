using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DeeceApi.InternalWorker
{
    public class InternalWorkerCommunication
    {
        private static InternalWorkerCommunication instance;
        public static InternalWorkerCommunication Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new InternalWorkerCommunication();
                }
                return instance;
            }
        }

        public class ThreadFile
        {
            public int Tid { get; set; }
            public string File { get; set; }
        }

        private Dictionary<int, BlockingCollection<ThreadFile>> fileRequests;
        private Dictionary<long, BlockingCollection<string>> fileResponses;

        private InternalWorkerCommunication()
        {
            fileRequests = new Dictionary<int, BlockingCollection<ThreadFile>>();
            fileResponses = new Dictionary<long, BlockingCollection<string>>();
        }

        public string Channel { get; set; }

        // Injected process call
        public string RequestFile(int pid, int tid, string requestedFile)
        {
            long ptid = (((long)pid) << 32) + tid;
            // TODO: Some cache mechanism here.
            // TODO: Threadsafe dictionary adds. Theoretically a problem, in practice not an issue.
            if (!fileRequests.ContainsKey(pid))
            {
                fileRequests.Add(pid, new BlockingCollection<ThreadFile>());
            }
            if (!fileResponses.ContainsKey(ptid))
            {
                fileResponses.Add(ptid, new BlockingCollection<string>());
            }

            fileRequests[pid].Add(new ThreadFile() { Tid = tid, File = requestedFile });
            return fileResponses[ptid].Take();
        }

        // Worker call
        public bool ReadFileRequest(int pid, out int tid, out string file, CancellationToken cancellationToken)
        {
            if (!fileRequests.ContainsKey(pid))
            {
                fileRequests.Add(pid, new BlockingCollection<ThreadFile>());
            }

            bool result = fileRequests[pid].TryTake(out ThreadFile threadFile, -1, cancellationToken);
            tid = threadFile.Tid;
            file = threadFile.File;
            return result;
        }

        // Worker call
        public void WriteFileResponse(long ptid, string newFilename)
        {
            if (!fileResponses.ContainsKey(ptid))
            {
                fileResponses.Add(ptid, new BlockingCollection<string>());
            }
            // There should only be at most 1 file in any of these collections,
            // so we shouldn't get things mixed up.
            // ptid = (pid << 32 + tid), which means each thread gets its own.
            fileResponses[ptid].Add(newFilename);
        }
    }
}
