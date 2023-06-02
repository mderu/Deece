using DeeceApi.InternalWorker;
using EasyHook;
using System;
using System.Runtime.Remoting;
using System.Threading.Tasks;

namespace DeeceWorker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Will contain the name of the IPC server channel
            string channelName = null;

            var ipcChannel = RemoteHooking.IpcCreateServer<InternalWorkerApi>(
                ref channelName,
                WellKnownObjectMode.Singleton);

            InternalWorkerCommunication.Instance.Channel = channelName;
            Server server = new Server();

            await server.StartAsync();

            Console.ReadKey();
        }
    }
}
