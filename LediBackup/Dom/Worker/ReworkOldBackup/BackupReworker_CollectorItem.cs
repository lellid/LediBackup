/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LediBackup.Dom.Filter;

namespace LediBackup.Dom.Worker.ReworkOldBackup
{
  public partial class BackupReworker
  {
    /// <summary>
    /// Enumerate files in directories and subdirectories, and creates <see cref="WorkerItem"/>s from the file names.
    /// One single task is used to create the items.
    /// Backup directories that are located on the same physical hard disk
    /// should be bundled in only one CollectorItem (and not in multiple CollectorItems).
    /// This helps avoiding simultaneous disc access from multiple threads.
    /// </summary>
    public class Collector
    {
      private BackupReworker _parent;
      private string _backupDirectory;


      public bool IsDisposed { get; private set; }
      private CancellationToken _cancellationToken;



      /// <summary>
      /// Initializes a new instance of the <see cref="CollectorItem"/> class.
      /// </summary>
      /// <param name="parent">The parent backup worker instance.</param>
      /// <param name="entries">The directory entries that should be bundled in this CollectorItem. See class comment
      /// in which way the DirectoryEntries should be bundled.</param>
      public Collector(BackupReworker parent, string backupDirectory)
      {
        _parent = parent;
        _backupDirectory = backupDirectory ?? throw new ArgumentNullException();
      }

      /// <summary>
      /// Occurs when a <see cref="WorkerItem"/> is available.
      /// </summary>
      public event Action<WorkerItem>? OutputAvailable;


      /// <summary>
      /// Starts the Task that creates the <see cref="WorkerItem"/>s.
      /// </summary>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns>A task that can be awaited.</returns>
      public Task Start(CancellationToken cancellationToken)
      {
        _cancellationToken = cancellationToken;
        return Task.Factory.StartNew(WorkerLoop, TaskCreationOptions.LongRunning);
      }

      public void Dispose()
      {
        IsDisposed = true;
      }

      private void WorkerLoop()
      {
        BackupSingleFolder(new DirectoryInfo(_backupDirectory), 0);
        _cancellationToken = CancellationToken.None;
      }

      public void BackupSingleFolder(DirectoryInfo sourceDirectory, int level)
      {
        Exception? ex1 = null;
        try
        {
          if (level > 0)
          {
            var enumerable = sourceDirectory.EnumerateFiles();


            foreach (var sourceFile in enumerable)
            {
              if (_cancellationToken.IsCancellationRequested)
                break;

              var readerItem = new WorkerItem(_parent, sourceFile);
              OutputAvailable?.Invoke(readerItem);
            }
          }
        }
        catch (Exception ex)
        {
          _parent._errorMessages.Enqueue($"Folder {sourceDirectory.FullName}: {ex.Message}");
          ex1 = ex;
        }



        if (_cancellationToken.IsCancellationRequested)
          return;
        try
        {
          var enumerator = sourceDirectory.EnumerateDirectories();

          foreach (var subSourceDirectory in enumerator)
          {
            if (_cancellationToken.IsCancellationRequested)
              break;

            if (level == 0)
            {
              if (0 == string.Compare(Current.BackupContentFolderName, subSourceDirectory.Name) ||
                  0 == string.Compare(Current.BackupNameFolderName, subSourceDirectory.Name))
                continue;
            }

            BackupSingleFolder(subSourceDirectory, level + 1);
          }
        }
        catch (Exception ex2)
        {
          if (ex2.Message != ex1?.Message)
          {
            _parent._errorMessages.Enqueue($"Folder {sourceDirectory.FullName}: {ex2.Message}");
          }

        }
      }
    }
  }
}
