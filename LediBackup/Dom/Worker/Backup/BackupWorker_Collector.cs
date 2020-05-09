/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LediBackup.Dom.Filter;

namespace LediBackup.Dom.Worker.Backup
{
  public partial class BackupWorker
  {

    /// <summary>
    /// Enumerate files in directories and subdirectories, and creates <see cref="ReaderItem"/>s from the file names.
    /// This class utilize one or more <see cref="CollectorItem"/>, each of the items running on an individual task.
    /// When backing up from different physical hard disks, the folders are bundled in a way that the folders belonging 
    /// to the same physical disk are bundled in one <see cref="CollectorItem"/> in order to avoid simulateneous access
    /// to the physical hard disk by more than one thread.
    /// </summary>
    public class Collector
    {
      /// <summary>
      /// Occurs when a <see cref="ReaderItem"/> is available.
      /// </summary>
      public event Action<ReaderItem>? OutputAvailable;

      public bool IsDisposed { get; private set; }

      private CollectorItem[] _collectorItems;

      /// <summary>
      /// Initializes a new instance of the <see cref="Collector"/> class.
      /// </summary>
      /// <param name="parent">The parent worker instance.</param>
      /// <param name="entries">All directory entries that should be backuped.</param>
      public Collector(BackupWorker parent, Dom.DirectoryEntryReadonly[] entries)
      {
        // TODO find out, which entries belong to which hard disk
        // Entries with the same hard disk should be grouped in one CollectorItem, since they should be evaluated sequential
        // Entries with different hard disk can be put in different CollectorItems.

        _collectorItems = new CollectorItem[1];
        var item = new CollectorItem(parent, entries);
        item.OutputAvailable += item => OutputAvailable?.Invoke(item);
        _collectorItems[0] = item;
      }

      /// <summary>
      /// Starts the Task that creates the <see cref="ReaderItem"/>s.
      /// </summary>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns>A task that can be awaited.</returns>
      public Task Start(CancellationToken cancellationToken)
      {
        var tasks = _collectorItems.Select(x => x.Start(cancellationToken)).ToArray();
        return Task.WhenAll(tasks);
      }

      public void Dispose()
      {
        IsDisposed = true;
      }
    }
  }
}
