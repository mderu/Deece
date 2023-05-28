namespace DeeceWorkerApi
{
    public interface IWorkerApi
    {
        void LogMessage(string message);
        void Ping();
    }
}
