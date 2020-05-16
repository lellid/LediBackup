/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LediBackup.Dom.Worker.PruneCentralContentStorage
{
  public class PruneCentralContentStorageWorker : IBackupWorker
  {
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private string _nameOfProcessedFile = string.Empty;
    private string _centralContentStorageDirectory;
    private string _centralNameDirectory;
    private ConcurrentQueue<string> _errorMessages = new ConcurrentQueue<string>();
    private int _numberOfProcessedFiles;


    public PruneCentralContentStorageWorker(string backupMainFolder)
    {
      if (!Directory.Exists(backupMainFolder))
        throw new ArgumentException($"The base folder \"{backupMainFolder}\" does not exist!");

      _centralContentStorageDirectory = Path.Combine(backupMainFolder, Current.BackupContentFolderName);

      if (!Directory.Exists(_centralContentStorageDirectory))
        throw new ArgumentException($"In the base folder \"{backupMainFolder}\" there is no central content storage directory \"{Current.BackupContentFolderName}\" ");

      _centralNameDirectory = Path.Combine(backupMainFolder, Current.BackupNameFolderName);
    }

    public async Task Backup()
    {
      _cancellationTokenSource = new CancellationTokenSource();

      var stopWatch = new Stopwatch();
      stopWatch.Start();

      await Task.Factory.StartNew(WorkerLoop, TaskCreationOptions.LongRunning);

      Duration = stopWatch.Elapsed;
      stopWatch.Stop();
    }

    public void WorkerLoop()
    {
      var cancellationToken = _cancellationTokenSource.Token;

      // first of all, delete the contents of the central name directory
      if (Directory.Exists(_centralNameDirectory))
      {
        // delete all central name files
        // because in the central name files directory, there can exist multiple files with different names,
        // that are all linked together, we must delete them all
        // the catch is that we have to reset the readonly flag in order to delete the files
        DeleteCentralFiles(_centralNameDirectory, int.MaxValue, cancellationToken);
      }

      // now delete the files in the central content storage, if its link number is equal to or less than 1
      DeleteCentralFiles(_centralContentStorageDirectory, 1, cancellationToken);
    }

    /// <summary>
    /// Deletes the central files, if the number of links falls below given number.
    /// </summary>
    /// <param name="centralDirectory">The central directory (either the central content storage directory, or the central name directory).</param>
    /// <param name="maxNumberOfLinks">The number of links. If the current number of links is equal to or below than this number, the file will be deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private void DeleteCentralFiles(string centralDirectory, int maxNumberOfLinks, CancellationToken cancellationToken)
    {
      var list = new List<FileInfo>();
      for (int i = 0; i < 256; ++i)
      {
        list.Clear();
        for (int j = 0; j < 256; ++j)
        {
          if (cancellationToken.IsCancellationRequested)
            return;
          try
          {
            var dir = new DirectoryInfo(Path.Combine(centralDirectory, i.ToString("X2"), j.ToString("X2")));
            _nameOfProcessedFile = dir.FullName;
            if (dir.Exists)
              list.AddRange(dir.GetFiles());
          }
          catch (Exception ex)
          {
            _errorMessages.Enqueue($"Error listing content of directory {_nameOfProcessedFile}, Message: {ex.Message}");
          }
        }

        foreach (FileInfo fi in list)
        {
          try
          {
            var links = FileUtilities.GetNumberOfLinks(fi.FullName);
            if (links <= maxNumberOfLinks)
            {
              if (fi.IsReadOnly)
              {
                fi.IsReadOnly = false;
              }
              fi.Delete();
              ++_numberOfProcessedFiles;
            }
          }
          catch (Exception ex)
          {
            _errorMessages.Enqueue($"Error inspecting or deleting {fi.FullName}, Message: {ex.Message}");
          }
        }
      }
    }

    public CancellationTokenSource CancellationTokenSource => _cancellationTokenSource;

    public int NumberOfItemsInReader => 0;

    public int NumberOfItemsInHasher => 0;

    public int NumberOfItemsInWriter => 0;

    public int NumberOfProcessedFiles => _numberOfProcessedFiles;

    public int NumberOfFailedFiles => 0;

    public string NameOfProcessedFile => _nameOfProcessedFile;

    public TimeSpan Duration { get; private set; }

    public ObservableCollection<string> ErrorMessages { get; } = new ObservableCollection<string>();

    /// <summary>
    /// Updates the error messages (brings them from the thread-safe concurrent queue to the Observable collection
    /// that can be used to bound to a ListView). This call should be made only in the context of the Gui thread.
    /// </summary>
    public void UpdateErrorMessages()
    {
      while (_errorMessages.TryDequeue(out var result))
        ErrorMessages.Add(result);
    }
  }
}
