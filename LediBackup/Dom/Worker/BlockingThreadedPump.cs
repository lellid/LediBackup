/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LediBackup.Dom.Worker
{
  /// <summary>
  /// This class consist of an input queue with limited capacity (a <see cref="BlockingCollection{T}"/>),
  /// and one or more threads that take items from the queue and then call <see cref="ItemAvailable"/>.
  /// The handler that is called by this event than can process the item in the context of the one or more worker threads.
  /// Note that under no circumstances the handler must throw an (uncatched) exception, because than the worker thread is ended.
  /// </summary>
  /// <typeparam name="TItem">The type of the item.</typeparam>
  public class BlockingThreadedPump<TItem>
  {
    private BlockingCollection<TItem> _inputQueue;
    private int _numberOfWorkingWorkerLoops;


    /// <summary>
    /// Occurs when a <see cref="TItem"/> was taken from the input queue and should be processed.
    /// You are responsible for catching exceptions that your event handler will throw!
    /// </summary>
    public event Action<TItem>? ItemAvailable;

    public bool IsDisposed { get; private set; }

    public bool HasEmptyInputQueue => _inputQueue.Count == 0 && 0 == _numberOfWorkingWorkerLoops;

    public int NumberOfItemsInInputQueue => _inputQueue.Count + _numberOfWorkingWorkerLoops;


    /// <summary>
    /// Initializes a new instance of the <see cref="BlockingThreadedPump{TItem}"/> class.
    /// </summary>
    /// <param name="numberOfWorkerThreads">The number of worker threads. Must be equal to or greater than 1.</param>
    /// <param name="maxNumberOfItemsInInputQueue">The maximum number of items in input queue. For an unlimited number, provide -1 as argument.</param>
    /// <exception cref="ArgumentException">
    /// Number of concurrent task must at least be 1 - numberOfWorkerThreads
    /// or
    /// Maximum number of items in the input queue must at least be 1 - maxNumberOfItemsInInputQueue
    /// </exception>
    public BlockingThreadedPump(int numberOfWorkerThreads, int maxNumberOfItemsInInputQueue)
    {
      if (numberOfWorkerThreads < 1)
        throw new ArgumentException("Number of concurrent task must at least be 1", nameof(numberOfWorkerThreads));
      if (maxNumberOfItemsInInputQueue < 1 && maxNumberOfItemsInInputQueue != -1)
        throw new ArgumentException("Maximum number of items in the input queue must at least be 1", nameof(maxNumberOfItemsInInputQueue));

      _inputQueue = new BlockingCollection<TItem>(maxNumberOfItemsInInputQueue);

      for (int i = 0; i < numberOfWorkerThreads; ++i) // create some worker loops
        Task.Factory.StartNew(WorkerLoop, TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Enqueues an item. If the maximum capacity of the input queue is currently reached, the calling thread
    /// is blocked, until an item is taken from the input queue by the working thread(s).
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    public void EnqueueBlocking(TItem item)
    {
      _inputQueue.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    private void WorkerLoop()
    {
      while (!IsDisposed)
      {
        if (_inputQueue.TryTake(out var readerItem, 1000))
        {
          Interlocked.Increment(ref _numberOfWorkingWorkerLoops);
          ItemAvailable?.Invoke(readerItem);
          Interlocked.Decrement(ref _numberOfWorkingWorkerLoops);
        }
      }
    }

    public void Dispose()
    {
      IsDisposed = true;
    }
  }

}
