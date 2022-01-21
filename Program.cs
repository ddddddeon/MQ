namespace Broker
{
    public class Program
    {
        public static async Task Main()
        {
            var server = new Server(9000);
            await server.Start();
        }
    }
}