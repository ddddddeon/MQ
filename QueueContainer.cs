using System.Collections.Generic;

namespace Broker
{
    class QueueContainer
    {
        public string Name;
        public Queue<string> Queue;

        public QueueContainer(string name)
        {
            Name = name;
            Queue = new Queue<string>();
        }
    }
}