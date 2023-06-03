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
            Server server = new Server();

            await server.StartAsync();

            Console.ReadKey();
        }
    }
}
