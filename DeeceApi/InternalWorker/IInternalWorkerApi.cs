namespace DeeceApi.InternalWorker
{
    /// <summary>
    /// <see cref="InternalWorkerApi"/>.
    /// </summary>
    public interface IInternalWorkerApi
    {
        string GetFileName(string remoteFileName, int pid, int tid);
        void LogMessage(string message);
    }
}
