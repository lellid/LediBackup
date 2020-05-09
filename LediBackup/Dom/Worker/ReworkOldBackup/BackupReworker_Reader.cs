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

namespace LediBackup.Dom.Worker.ReworkOldBackup
{
  public partial class BackupReworker
  {

    public class Reader
    {

      public const int MaxItemsInInputQueue = 10;

      /// <summary>
      /// The blocking input queue handles the output of the collector.
      /// </summary>
      private BlockingCollection<WorkerItem> InputQueue { get; }

      /// <summary>
      /// The nonblocking input queue stores items that come back from the hasher,
      /// because the reading is not complete yet.
      /// </summary>
      private BlockingCollection<WorkerItem> NonblockingInputQueue { get; }

      /// <summary>
      /// Occurs when a <see cref="WorkerItem"/> is available.
      /// </summary>
      public event Action<WorkerItem>? OutputAvailable;

      public bool IsDisposed { get; private set; }

      public Reader()
      {
        InputQueue = new BlockingCollection<WorkerItem>(MaxItemsInInputQueue);
        NonblockingInputQueue = new BlockingCollection<WorkerItem>();
        Task.Factory.StartNew(WorkerLoop, TaskCreationOptions.LongRunning);
      }

      public bool HasEmptyInputQueue => 0 == _numberOfWorkingWorkerLoops && InputQueue.Count == 0 && NonblockingInputQueue.Count == 0;

      public int NumberOfItemsInInputQueue => InputQueue.Count + NonblockingInputQueue.Count;

      public void Dispose()
      {
        IsDisposed = true;
      }



      public void WaitForEnqueue(WorkerItem item)
      {
        InputQueue.Add(item ?? throw new ArgumentNullException(nameof(item)));
      }


      public void Enqueue(WorkerItem item)
      {
        NonblockingInputQueue.Add(item ?? throw new ArgumentNullException(nameof(item)));
      }


      private int _numberOfWorkingWorkerLoops;
      private void WorkerLoop()
      {
        var bothQueues = new BlockingCollection<WorkerItem>[] { NonblockingInputQueue, InputQueue };
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
            if (BlockingCollection<WorkerItem>.TryTakeFromAny(bothQueues, out var readerItem) >= 0)
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
