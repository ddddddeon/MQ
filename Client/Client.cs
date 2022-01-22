using System.Net.Sockets;
using System.Net;
using System.Text;

namespace MQ.Client
{
    public class Client
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        public string Host { get; set; }
        public int Port { get; set; }
        public string QueueName { get; set; }
        private byte[] _bytes = new byte[1024];
        private int _size;

        public Client()
        {
            Host = "localhost";
            Port = 9000;
        }

        public Client(string host)
        {
            Host = host;
        }

        public Client(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public async Task<bool> Connect()
        {
            try
            {
                Console.WriteLine("connecting..");
                _client = new TcpClient(Host, Port);
                _stream = _client.GetStream();
                _reader = new StreamReader(_stream);
                _writer = new StreamWriter(_stream) { AutoFlush = true };

                string res = await Read();
                if (res.Contains(">"))
                {
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public async Task Disconnect()
        {
            await Write("DISCONNECT;");
            _client.Close();
        }

        public async Task Write(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            await _writer.WriteLineAsync(message);
        }

        public async Task<string> Read()
        {
            byte[] buf = new byte[1024];
            int len;

            while (true)
            {
                len = await _stream.ReadAsync(buf, 0, buf.Length);
                if (len == 0)
                {
                    break;
                }
                return Encoding.ASCII.GetString(buf, 0, len);
            }
            return "";
        }

        public async Task Enqueue(string message)
        {
            await Write("EN;NAME=chris;" + message + "END;");
            string res = await Read();
            Console.WriteLine(res);
        }

        public async Task<string> Dequeue()
        {
            //TODO check for queue name
            await Write("DE;END;");
            string result = await Read();
            return result;
        }
    }
}