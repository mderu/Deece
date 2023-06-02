namespace Deece
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            await new Client("127.0.0.1", 63_378).StartAsync();
        }
    }
}
