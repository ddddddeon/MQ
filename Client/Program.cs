using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

namespace MQ.Client
{
    class Program
    {
        public static async Task Main()
        {
            var client = new Client("localhost", 9000) { QueueName = "chris" };
            bool connected = await client.Connect();
            if (!connected)
            {
                Environment.Exit(1);
            }

            string res;

            await client.Write("EN;NAME=chris;asdfEND;\n");
            res = await client.Read();
            Console.WriteLine(res);

            await client.Write("DE;END;\n");
            res = await client.Read();
            Console.WriteLine(res);

            await client.Enqueue("Chris");
            await client.Enqueue("is");
            await client.Enqueue("cool");

            Console.WriteLine(await client.Dequeue());
            Console.WriteLine(await client.Dequeue());
            Console.WriteLine(await client.Dequeue());

            await client.Disconnect();
        }
    }
}