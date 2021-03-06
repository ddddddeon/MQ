using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace MQ.Broker
{
    class Channel
    {
        public int Id;
        public NetworkStream Stream { get; private set; }
        public QueueContainer Container { get; private set; }
        public List<QueueContainer> QueueContainers { get; set; }

        private bool IsConnected = false;
        private bool QueueConnected = false;
        private int Recv;
        private byte[] Data;
        private string MessageTerminator = "END;\n";
        private string ChannelTerminator = "DISCONNECT;\n";
        private string QueueNameHeaderString = "QUEUE_NAME=";
        private Regex HeadersRegex = new Regex(@"^(EN|DE)\;(NAME\=([a-zA-Z0-9_-]+)\;)?");
        private StringBuilder FullMessage = new StringBuilder();
        private string Operation;

        public Channel(NetworkStream stream, List<QueueContainer> queueContainers)
        {
            Stream = stream;
            QueueContainers = queueContainers;
            IsConnected = true;
        }

        public async Task Send(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            Stream.Write(bytes);
        }

        public async Task ReadAndRespond()
        {
            await Send(">" + MessageTerminator);

            while (IsConnected)
            {
                Data = new byte[1024];
                Recv = await Stream.ReadAsync(Data, 0, Data.Length);
                if (Recv == 0)
                {
                    break;
                }

                string message = Encoding.ASCII.GetString(Data, 0, Recv);
                HandleHeaders(message);
                await HandleMessage(message);
                HandleDisconnect(message);
            }
        }

        private void HandleHeaders(string message)
        {
            // Check beginning of message for QUEUE_NAME header
            var match = HeadersRegex.Match(message);
            var captureGroups = match.Groups;

            Operation = captureGroups[1].Value.Length > 0 ? captureGroups[1].Value : Operation;

            if (Operation == "EN" && !QueueConnected)
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                CreateQueueContainer(timestamp);
            }
            else if (captureGroups[2].Success && captureGroups[3].Success)
            {
                string queueName = captureGroups[3].Value;
                QueueContainer existingQueueContainer = QueueContainers.Find(q => q.Name == queueName);

                if (existingQueueContainer != null)
                {
                    Container = existingQueueContainer;
                    QueueConnected = true;
                }
                else
                {
                    CreateQueueContainer(queueName);
                }
            }
            //strip out the headers
            if (match.Value.Length > 0)
            {
                message = message.Replace(match.Value, "");
            }
            FullMessage.Append(message);
        }

        private async Task HandleMessage(string message)
        {
            // respond only if terminator string is actually at the end of the message
            if (IsPresentAtEnd(message, MessageTerminator))
            {
                FullMessage.Replace(MessageTerminator, "");
                string fullMessageString = FullMessage.ToString();
                Console.WriteLine("Reached end of message! {0}", fullMessageString);
                Console.WriteLine("queueContainers: {0}", QueueContainers.Count);
                Console.WriteLine("Queue name: {0}", Container.Name);
                Console.WriteLine("Message: {0}", fullMessageString);

                if (Operation == "EN")
                {
                    Container.Queue.Enqueue(fullMessageString);
                    FullMessage.Clear();
                    await Send("OK" + MessageTerminator);
                }
                else if (Operation == "DE")
                {
                    var item = "";
                    if (Container.Queue.Count > 0)
                    {
                        item = Container.Queue.Dequeue();
                    }
                    await Send(item + MessageTerminator);
                }
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

        private void CreateQueueContainer(string queueName)
        {
            QueueContainer newQueueContainer = new QueueContainer(queueName);
            Container = newQueueContainer;
            QueueContainers.Add(newQueueContainer);
            QueueConnected = true;
        }

        private bool IsPresentAtEnd(string message, string str)
        {
            int index = message.IndexOf(str);
            return (index > -1 && index == message.Length - str.Length);
        }
    }
}