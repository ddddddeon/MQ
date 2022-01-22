using System.Net.Sockets;
using System.Text;

namespace MQ.Broker
{
    class Server
    {
        public int Port { get; private set; }
        public bool IsRunning { get; set; }
        public List<Channel> channels = new List<Channel>();
        public List<QueueContainer> queueContainers = new List<QueueContainer>();
        public List<TcpClient> tcpClients = new List<TcpClient>();

        public Server(int port)
        {
            Port = port;
        }

        public async Task Start()
        {
            try
            {
                TcpListener listener = TcpListener.Create(9000);
                listener.Start();
                IsRunning = true;

                Console.WriteLine("Server listening on port {0}", Port);

                while (IsRunning)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    tcpClients.Append(client);

                    CreateChannel(client);
                }

            }
            catch (Exception e)
            {
                IsRunning = false;
                Console.WriteLine(e.ToString());
            }
        }

        private async Task CreateChannel(object obj)
        {
            var client = (TcpClient)obj;
            NetworkStream ns = client.GetStream();

            // TODO make queueContainers a singleton?
            Channel channel = new Channel(ns, queueContainers);
            channels.Append(channel);

            await channel.Send("> ");
            await channel.ReadAndRespond();
        }
    }
}