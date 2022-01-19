using System.Net.Sockets;
using System.Text;

namespace Broker
{
    class Server
    {
        public int Port { get; set; }
        public Server(int port)
        {
            Port = port;
        }

        public void Start()
        {
            try
            {
                Listen();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Listen()
        {
            TcpListener listener = TcpListener.Create(9000);
            listener.Start();

            Console.WriteLine("Server listening on port {0}", Port);

            TcpClient client = listener.AcceptTcpClient();
            NetworkStream ns = client.GetStream();

            //TODO check if queue name is given in headers, for now just create one
            QueueContainer qc = new QueueContainer("test-queue");
            Channel channel = new Channel(ns, qc);

            channel.Send("> ");
            channel.ReadAndRespond();
        }
    }
}