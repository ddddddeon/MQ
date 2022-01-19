using System.Net.Sockets;
using System.Text;

namespace Broker
{
    class Channel
    {
        StringBuilder FullMessage = new StringBuilder();
        public int Id;
        private byte[] Data;
        private int Recv;
        public NetworkStream Stream { get; private set; }
        public Queue<string> Queue { get; private set; }

        public Channel(NetworkStream stream, QueueContainer queueContainer)
        {
            Stream = stream;
            Queue = queueContainer.Queue;
        }

        public void Send(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            Stream.Write(bytes);
        }

        public void ReadAndRespond()
        {
            while (true)
            {
                Data = new byte[1024];
                Recv = Stream.Read(Data, 0, Data.Length);
                if (Recv == 0)
                {
                    break;
                }

                string message = Encoding.ASCII.GetString(Data, 0, Recv);
                FullMessage.Append(message);

                string terminatorString = "END;\n";
                int terminatorIndex = message.IndexOf(terminatorString);

                // respond only if terminator string is actually at the end of the message
                if (terminatorIndex > -1 && terminatorIndex == message.Length - 5)
                {
                    string fullMessageString = FullMessage.ToString();
                    Console.WriteLine("Reached end of message! {0}", fullMessageString);

                    // strip terminator string from message
                    fullMessageString = fullMessageString.Substring(0, fullMessageString.Length - terminatorString.Length);

                    Queue.Enqueue(fullMessageString);
                    Data = Encoding.ASCII.GetBytes("> ");
                    Stream.Write(Data, 0, Data.Length);

                    var item = Queue.Dequeue();
                    Console.WriteLine(item);
                }
            }
        }
    }
}