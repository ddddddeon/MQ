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
            int recv = 0;
            byte[] data = new byte[1024];
            StringBuilder fullMessage = new StringBuilder();

            TcpListener listener = TcpListener.Create(9000);
            listener.Start();

            Console.WriteLine("Server listening on port {0}", Port);

            TcpClient client = listener.AcceptTcpClient();
            NetworkStream ns = client.GetStream();

            data = Encoding.ASCII.GetBytes("> ");
            ns.Write(data, 0, data.Length);

            while (true)
            {
                data = new byte[1024];
                recv = ns.Read(data, 0, data.Length);
                if (recv == 0)
                {
                    break;
                }

                string message = Encoding.ASCII.GetString(data, 0, recv);
                fullMessage.Append(message);

                string terminatorString = "END;\n";
                int terminatorIndex = message.IndexOf(terminatorString);

                // respond only if terminator string is actually at the end of the message
                if (terminatorIndex > -1 && terminatorIndex == message.Length - 5)
                {
                    string fullMessageString = fullMessage.ToString();
                    Console.WriteLine("Reached end of message! {0}", fullMessageString);

                    var qc = new QueueContainer("test");
                    var queue = qc.Queue;

                    // strip terminator string from message
                    fullMessageString = fullMessageString.Substring(0, fullMessageString.Length - terminatorString.Length);

                    queue.Enqueue(fullMessageString);
                    data = Encoding.ASCII.GetBytes("> ");
                    ns.Write(data, 0, data.Length);

                    var item = queue.Dequeue();
                    Console.WriteLine(item);
                }
            }

        }
    }
}