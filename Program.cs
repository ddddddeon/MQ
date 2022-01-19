namespace Broker
{
    public class Program
    {
        public static void Main()
        {
            var server = new Server(9000);
            server.Start();
        }
    }
}