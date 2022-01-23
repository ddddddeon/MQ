# MQ
An asynchronous TCP message queueing system written in C#

## Broker/Client

The broker is a TCP server that exposes queues that can be enqueued to, and dequeued from, by a TCP client. The client is a wrapper for the simple "protocol" that the server understands. When a client opens a socket connection to the server, a channel is created. If the client specifies a queue name, the server fetches or creates the queue and allows the client to run enqueue and dequeue commands. 

## The protocol

The "protocol" is relatively simple. An example of an enqueue command that the client sends is as follows:

```
EN;NAME=myqueue;this is a messageEND\n;
```

- `EN` specifies that the client wants to enqueue a message.
- `NAME=myqueue` sets the queue name. If a queue by that name does not exist, the server will create it. The server will then attach the queue to the channel object, associating any further requests from that client to that queue, unless and until the client specifies a new queue name.
- `this is a messageEND\n`: The message always ends in the terminator string `END;\n` and will be stripped out when dequeued by the client. The client can send long messages spanning multiple TCP packets and the server will concatenate them as one message in the queue, as long as the terminator string appears at the end of the last packet. 

To dequeue a message from the queue, the client sends a command such as this:

```
DE;END;\n
```

or, if the queue name hasn't yet been specified,

```
DE;NAME=queuename;END;
```

This will return a string with the headers and terminator string stripped out. 

## The client API

A thin client library is included that wraps these string commands and exposes an asynchronous API. 

```csharp
var client = new Client("localhost", 9000);
client.QueueName = "myqueue";

bool connected = await client.Connect();
if (!connected) {
    Environment.Exit(1);
}

string response;
await client.Enqueue("first message");
await client.Enqueue("second message");

response = await client.Dequeue();
Console.WriteLine(response); // first message

response = await client.Dequeue();
Console.WriteLine(response); // second message

await client.Disconnect();

```

# IMPORTANT
This software is educational and is absolutely **NOT** production-ready! It barely validates client data at all and the socket connection is **NOT encrypted**. Do **not** use this for any real-world applications! You have been warned.