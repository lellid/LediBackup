/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LediBackup.Dom.Worker.Backup
{
  public partial class BackupWorker
  {

    public class Reader
    {

      public const int MaxItemsInInputQueue = 10;

      /// <summary>
      /// The blocking input queue handles the output of the collector.
      /// </summary>
      private BlockingCollection<ReaderItem> InputQueue { get; }

      /// <summary>
      /// The nonblocking input queue stores items that come back from the hasher,
      /// because the reading is not complete yet.
      /// </summary>
      private BlockingCollection<ReaderItem> NonblockingInputQueue { get; }

      /// <summary>
      /// Occurs when a <see cref="ReaderItem"/> is available.
      /// </summary>
      public event Action<ReaderItem>? OutputAvailable;

      public bool IsDisposed { get; private set; }

      public Reader()
      {
        InputQueue = new BlockingCollection<ReaderItem>(MaxItemsInInputQueue);
        NonblockingInputQueue = new BlockingCollection<ReaderItem>();
        Task.Factory.StartNew(WorkerLoop, TaskCreationOptions.LongRunning);
      }

      public bool HasEmptyInputQueue => 0 == _numberOfWorkingWorkerLoops && InputQueue.Count == 0 && NonblockingInputQueue.Count == 0;

      public int NumberOfItemsInInputQueue => InputQueue.Count + NonblockingInputQueue.Count + _numberOfWorkingWorkerLoops;

      public void Dispose()
      {
        IsDisposed = true;
      }



      public void WaitForEnqueue(ReaderItem item)
      {
        InputQueue.Add(item ?? throw new ArgumentNullException(nameof(item)));
      }


      public void Enqueue(ReaderItem item)
      {
        NonblockingInputQueue.Add(item ?? throw new ArgumentNullException(nameof(item)));
      }


      private int _numberOfWorkingWorkerLoops;
      private void WorkerLoop()
      {
        var bothQueues = new BlockingCollection<ReaderItem>[] { NonblockingInputQueue, InputQueue };
        for (; !IsDisposed;)
        {
          {
            Interlocked.Increment(ref _numberOfWorkingWorkerLoops);
            while (NonblockingInputQueue.TryTake(out var readerItem, 0))
            {
              readerItem.Read();
              OutputAvailable?.Invoke(readerItem);
            }
            Interlocked.Decrement(ref _numberOfWorkingWorkerLoops);
          }
          {
            if (BlockingCollection<ReaderItem>.TryTakeFromAny(bothQueues, out var readerItem) >= 0)
            {
              Interlocked.Increment(ref _numberOfWorkingWorkerLoops);
              readerItem!.Read();
              OutputAvailable?.Invoke(readerItem);
              Interlocked.Decrement(ref _numberOfWorkingWorkerLoops);
            }
          }
        }
      }
    }
  }
}
