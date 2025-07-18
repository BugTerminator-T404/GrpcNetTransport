﻿namespace Concurrency
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A version of <see cref="ConcurrentQueue{T}"/> that you can asynchronously
    /// dequeue items from and asynchronously enumerate over.
    /// </summary>
    /// <typeparam name="T">The element in the queue.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "This class implements a queue.")]
    public class AwaitableConcurrentQueue<T> : IAsyncEnumerable<T>
    {
        private readonly Semaphore _ready = new Semaphore(0);
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        /// <summary>
        /// Enqueue an item into the queue.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
            _ready.Release();
        }

        /// <summary>
        /// The number of items in the queue. This property is not thread-safe
        /// and may return a slightly different value if an item is being added
        /// or removed from the queue at the same time.
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// Dequeues an item from the queue, asynchronously waiting until an item
        /// is available.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the dequeue operation.</param>
        /// <returns>The item that was dequeued.</returns>
        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
        {
            await _ready.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!_queue.TryDequeue(out var result))
            {
                throw new InvalidOperationException("Dequeue failed to pull item off queue. This is an internal bug.");
            }
            return result!;
        }

        /// <inheritdoc />
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerator(this, cancellationToken);
        }

        private sealed class AsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly AwaitableConcurrentQueue<T> _queue;
            private readonly CancellationToken _cancellationToken;
#if !NETCOREAPP3_0_OR_GREATER
            private T _current;
#else
            private T? _current;
#endif
            private bool _currentSet;

            public AsyncEnumerator(AwaitableConcurrentQueue<T> queue, CancellationToken cancellationToken)
            {
                _queue = queue;
                _cancellationToken = cancellationToken;
                _current = default(T);
                _currentSet = false;
            }

            public T Current => _currentSet switch
            {
                true => _current!,
                false => throw new InvalidOperationException("You must call MoveNext first!"),
            };

            public ValueTask DisposeAsync()
            {
#if !NET5_0_OR_GREATER
                return default;
#else
                return ValueTask.CompletedTask;
#endif
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                _current = await _queue.DequeueAsync(_cancellationToken).ConfigureAwait(false);
                _currentSet = true;
                return true;
            }
        }
    }
}
