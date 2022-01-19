using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Broker
{
    class Channel
    {
        StringBuilder FullMessage = new StringBuilder();
        public int Id;
        public NetworkStream Stream { get; private set; }
        public Queue<string> Queue { get; private set; }
        private List<QueueContainer> QueueContainers { get; set; }
        private bool IsConnected = false;
        private int Recv;
        private byte[] Data;
        private string MessageTerminator = "END;\n";
        private string ChannelTerminator = "DISCONNECT;\n";
        private string QueueNameHeaderString = "QUEUE_NAME=";
        private Regex QueueNameRegex = new Regex(@"^QUEUE_NAME\=[a-zA-Z0-9_-]+;");

        public Channel(NetworkStream stream, List<QueueContainer> queueContainers)
        {
            Stream = stream;
            QueueContainers = queueContainers;
            IsConnected = true;
        }

        public void Send(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            Stream.Write(bytes);
        }

        private void CreateQueueContainer(string queueName)
        {
            QueueContainer newQueueContainer = new QueueContainer(queueName);
            Queue = newQueueContainer.Queue;
            QueueContainers.Append(newQueueContainer);
        }

        private void HandleHeaders(string message)
        {
            // Check beginning of message for QUEUE_NAME header
            var matches = QueueNameRegex.Matches(message);
            if (matches.Count == 0)
            {
                // TODO: make unique
                string uniqueQueueName = "foobar";
                CreateQueueContainer(uniqueQueueName);
            }
            else
            {
                string headerString = matches[0].Value;
                string queueName = headerString.Replace(QueueNameHeaderString, "");
                QueueContainer existingQueueContainer = QueueContainers.Find(q => q.Name == queueName);

                if (existingQueueContainer != null)
                {
                    Queue = existingQueueContainer.Queue;
                }
                else
                {
                    CreateQueueContainer(queueName);
                }

                //strip out the queue name
                message = message.Replace(headerString, "");
            }

            FullMessage.Append(message);
        }

        private bool IsPresentAtEnd(string message, string str)
        {
            int index = message.IndexOf(str);
            return (index > -1 && index == message.Length - str.Length);
        }

        private void HandleMessage(string message)
        {
            // respond only if terminator string is actually at the end of the message
            if (IsPresentAtEnd(message, MessageTerminator))
            {
                FullMessage.Replace(MessageTerminator, "");
                string fullMessageString = FullMessage.ToString();
                Console.WriteLine("Reached end of message! {0}", fullMessageString);

                Queue.Enqueue(fullMessageString);
                Data = Encoding.ASCII.GetBytes("> ");
                Stream.Write(Data, 0, Data.Length);

                var item = Queue.Dequeue();
                Console.WriteLine(item);
            }
        }

        private void HandleDisconnect(string message)
        {
            if (IsPresentAtEnd(message, ChannelTerminator))
            {
                IsConnected = false;
                Console.WriteLine("Channel closed!");
            }
        }

        public void ReadAndRespond()
        {
            while (IsConnected)
            {
                Data = new byte[1024];
                Recv = Stream.Read(Data, 0, Data.Length);
                if (Recv == 0)
                {
                    break;
                }

                string message = Encoding.ASCII.GetString(Data, 0, Recv);
                HandleHeaders(message);
                HandleMessage(message);
                HandleDisconnect(message);
            }
        }
    }
}