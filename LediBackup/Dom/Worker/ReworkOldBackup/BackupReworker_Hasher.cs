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

namespace LediBackup.Dom.Worker.ReworkOldBackup
{
  public partial class BackupReworker
  {
    private class Hasher
    {
      private int _numberOfConcurrentTasks;
      private BlockingCollection<WorkerItem> InputQueue { get; } = new BlockingCollection<WorkerItem>(20);

      /// <summary>
      /// Occurs when a <see cref="WorkerItem"/> is available.
      /// </summary>
      public event Action<WorkerItem>? OutputAvailable;

      public bool IsDisposed { get; private set; }

      public bool HasEmptyInputQueue => InputQueue.Count == 0 && 0 == _numberOfWorkingWorkerLoops;

      public int NumberOfItemsInInputQueue => InputQueue.Count;


      public Hasher()
      {
        _numberOfConcurrentTasks = 8;

        for (int i = 0; i < _numberOfConcurrentTasks; ++i) // create some worker loops
          Task.Factory.StartNew(WorkerLoop, TaskCreationOptions.LongRunning);
      }

      public void Enqueue(WorkerItem item)
      {
        InputQueue.Add(item ?? throw new ArgumentNullException(nameof(item)));
      }

      private int _numberOfWorkingWorkerLoops;
      private void WorkerLoop()
      {

        while (!IsDisposed)
        {
          if (InputQueue.TryTake(out var readerItem, -1))
          {
            Interlocked.Increment(ref _numberOfWorkingWorkerLoops);
            readerItem.Hash();
            OutputAvailable?.Invoke(readerItem);
            Interlocked.Decrement(ref _numberOfWorkingWorkerLoops);
          }
        }
      }

      public void Dispose()
      {
        IsDisposed = true;
        // InputQueue.Dispose();
      }
    }
  }
}
