using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Glint.Networking.Utils.Collections {
    /// <summary>
    /// provides a ring-buffer interface wrapping ConcurrentQueue
    /// </summary>
    public class ConcurrentRingQueue<T> : IReadOnlyCollection<T> {
        private ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        public readonly int capacity;
        public bool full => queue.Count >= capacity;

        public ConcurrentRingQueue(int capacity) {
            this.capacity = capacity;
        }

        public void enqueue(T item) {
            // check capacity. if we're at capacity, we drop an item
            while (full) {
                queue.TryDequeue(out _);
            }

            // now that we're below capacity again, we add the item
            queue.Enqueue(item);
        }

        public bool tryDequeue(out T item) {
            return queue.TryDequeue(out item);
        }

        public T peekAt(int position) {
            return queue.ElementAt(position);
        }

        public IEnumerator<T> GetEnumerator() => queue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => queue.GetEnumerator();

        public int Count => queue.Count;
    }
}