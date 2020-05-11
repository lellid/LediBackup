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
  /// This class consist of an input queue with limited capacity (a <see cref="BlockingCollection{T}"/>), a queue with unlimited capacity,
  /// and one or more threads that take items from first the unlimited (nonblocking) queue and then the blocking queue.
  /// After taking an item,  the <see cref="ItemAvailable"/> event is fired.
  /// The handler that is called by this event can then process the item in the context of the worker thread(s).
  /// Note that under no circumstances the handler must throw an (uncatched) exception, because than that worker thread is ended.
  /// </summary>
  /// <typeparam name="TItem">The type of the item.</typeparam>
  public class BlockingThreadedPumpWithNonblockingSideChannel<TItem>
  {
    private int _numberOfWorkingWorkerLoops;

    /// <summary>
    /// The blocking input queue handles the output of the collector.
    /// </summary>
    private BlockingCollection<TItem> _inputQueue;

    /// <summary>
    /// The nonblocking input queue stores items that come back from the hasher,
    /// because the reading is not complete yet.
    /// </summary>
    private BlockingCollection<TItem> _nonblockingInputQueue;

    /// <summary>
    /// Occurs when a <see cref="ReaderItem"/> is available.
    /// </summary>
    public event Action<TItem>? ItemAvailable;

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockingThreadedPump{TItem}"/> class.
    /// </summary>
    /// <param name="numberOfWorkerThreads">The number of worker threads. Must be equal to or greater than 1.</param>
    /// <param name="maxNumberOfItemsInInputQueue">The maximum number of items in the blocking input queue. Must be equal to or greater than 1. Note: if you need an unlimited input queue, use <see cref="BlockingThreadedPump{TItem}"/> with -1 for maximumNumberOfItemsInInputQueue.</param>
    /// <exception cref="ArgumentException">
    /// Number of concurrent task must at least be 1
    /// or
    /// Maximum number of items in the input queue must at least be 1
    /// </exception>
    public BlockingThreadedPumpWithNonblockingSideChannel(int numberOfWorkerThreads, int maxNumberOfItemsInInputQueue)
    {
      if (numberOfWorkerThreads < 1)
        throw new ArgumentException("Number of worker threads must at least be 1", nameof(numberOfWorkerThreads));
      if (maxNumberOfItemsInInputQueue < 1)
        throw new ArgumentException("Maximum number of items in the input queue must at least be 1", nameof(maxNumberOfItemsInInputQueue));

      _inputQueue = new BlockingCollection<TItem>(maxNumberOfItemsInInputQueue);
      _nonblockingInputQueue = new BlockingCollection<TItem>();
      for (int i = 0; i < numberOfWorkerThreads; ++i) // create some worker loops
        Task.Factory.StartNew(WorkerLoop, TaskCreationOptions.LongRunning);
    }

    public bool HasEmptyInputQueue => 0 == _numberOfWorkingWorkerLoops && _inputQueue.Count == 0 && _nonblockingInputQueue.Count == 0;

    public int NumberOfItemsInInputQueue => _inputQueue.Count + _nonblockingInputQueue.Count + _numberOfWorkingWorkerLoops;

    public void Dispose()
    {
      IsDisposed = true;
    }

    public void EnqueueBlocking(TItem item)
    {
      _inputQueue.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }


    public void EnqueueNonblocking(TItem item)
    {
      _nonblockingInputQueue.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }


    private void WorkerLoop()
    {
      var bothQueues = new BlockingCollection<TItem>[] { _nonblockingInputQueue, _inputQueue };
      for (; !IsDisposed;)
      {
        {
          Interlocked.Increment(ref _numberOfWorkingWorkerLoops);
          while (_nonblockingInputQueue.TryTake(out var readerItem, 0))
          {
            ItemAvailable?.Invoke(readerItem);
          }
          Interlocked.Decrement(ref _numberOfWorkingWorkerLoops);
        }
        {
          if (BlockingCollection<TItem>.TryTakeFromAny(bothQueues, out var readerItem) >= 0)
          {
            Interlocked.Increment(ref _numberOfWorkingWorkerLoops);
            ItemAvailable?.Invoke(readerItem!);
            Interlocked.Decrement(ref _numberOfWorkingWorkerLoops);
          }
        }
      }
    }
  }

}
