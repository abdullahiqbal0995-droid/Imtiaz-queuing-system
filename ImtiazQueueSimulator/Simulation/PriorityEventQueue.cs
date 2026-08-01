namespace ImtiazQueueSimulator.Simulation
{
    using ImtiazQueueSimulator.Models;

    /// <summary>
    /// Min-Heap based priority queue for scheduling simulation events.
    /// Events are ordered by time, with ties broken by sequence number.
    /// </summary>
    public class PriorityEventQueue
    {
        private readonly List<SimEvent> _heap = new();

        public int Count => _heap.Count;
        public bool IsEmpty => _heap.Count == 0;

        public void Enqueue(SimEvent item)
        {
            _heap.Add(item);
            SiftUp(_heap.Count - 1);
        }

        public SimEvent Dequeue()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("Priority queue is empty.");

            var min = _heap[0];
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);

            if (_heap.Count > 0)
                SiftDown(0);

            return min;
        }

        public SimEvent Peek()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("Priority queue is empty.");
            return _heap[0];
        }

        public void Clear()
        {
            _heap.Clear();
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_heap[index].CompareTo(_heap[parent]) < 0)
                {
                    Swap(index, parent);
                    index = parent;
                }
                else break;
            }
        }

        private void SiftDown(int index)
        {
            int count = _heap.Count;
            while (true)
            {
                int smallest = index;
                int left = 2 * index + 1;
                int right = 2 * index + 2;

                if (left < count && _heap[left].CompareTo(_heap[smallest]) < 0)
                    smallest = left;
                if (right < count && _heap[right].CompareTo(_heap[smallest]) < 0)
                    smallest = right;

                if (smallest != index)
                {
                    Swap(index, smallest);
                    index = smallest;
                }
                else break;
            }
        }

        private void Swap(int i, int j)
        {
            var temp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = temp;
        }
    }
}
