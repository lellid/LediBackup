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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LediBackup.Dom.Filter;

namespace LediBackup.Dom.Worker.Backup
{
  public partial class BackupWorker : IBackupWorker
  {
    private const int MaxSizeOfFileToBeReadIntoArray = 1024 * 1024 * 1024;

    private Dom.DirectoryEntryReadonly[] _directoriesToBackup;

    /// <summary>
    /// The backup central storage folder. This folder contains all files of all backups.
    /// The files are names as the hashcode of its contents, without extension.
    /// To avoid too many files in a single folder, there are some subfolders below this folder,
    /// according to the first letters of the hash code.
    /// </summary>
    private string _backupCentralContentStorageFolder;

    /// <summary>
    /// The backup central storage folder. This folder contains all files of all backups.
    /// The files are names as the hashcode of its contents, without extension.
    /// To avoid too many files in a single folder, there are some subfolders below this folder,
    /// according to the first letters of the hash code.
    /// </summary>
    private string _backupCentralNameStorageFolder;

    private BackupMode _backupMode = BackupMode.Fast;

    /// <summary>
    /// The folder which is used for the current backup.
    /// </summary>
    private string _todaysBackupFolder;

    private ItemProducerCollection _producerCollection;
    private BlockingThreadedPumpWithNonblockingSideChannel<WorkerItem> _readerQueue;
    private BlockingThreadedPump<WorkerItem> _hasherQueue;
    private BlockingThreadedPump<WorkerItem> _writerQueue;
    private SHA256Pool _sha256Pool;
    private BufferPool _bufferPool;


    private ConcurrentBag<(FileInfo FileInfo, Exception Ex)> _failedFiles = new ConcurrentBag<(FileInfo FileInfo, Exception Ex)>();

    private ConcurrentQueue<string> _errorMessages = new ConcurrentQueue<string>();


    private int _numberOfItemsCreated;
    private int _numberOfItemsProcessed;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    /// <summary>
    /// Gets the cancellation token source. Can be used to cancel the backup process.
    /// </summary>
    /// <value>
    /// The cancellation token source.
    /// </value>
    public CancellationTokenSource CancellationTokenSource => _cancellationTokenSource;

    SupervisedFileOperations FileOperations { get; } = new SupervisedFileOperations(6, TimeSpan.FromSeconds(10));

    #region Diagnostics

    public int NumberOfItemsInReader => _readerQueue.NumberOfItemsInInputQueue;
    public int NumberOfItemsInHasher => _hasherQueue.NumberOfItemsInInputQueue;
    public int NumberOfItemsInWriter => _writerQueue.NumberOfItemsInInputQueue;

    public int NumberOfProcessedFiles => _numberOfItemsProcessed;
    public int NumberOfFailedFiles => _failedFiles.Count;

    public string NameOfProcessedFile { get; private set; } = string.Empty;

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

    #endregion Diagnostics

    public BackupWorker(BackupDocument doc)
    {
      // test the short names for uniqueness
      _directoriesToBackup = doc.Directories.Where(x => x.IsEnabled).Select(xx => new Dom.DirectoryEntryReadonly(xx)).ToArray();
      _backupMode = doc.BackupMode;
      var shortNames = new HashSet<string>(
          _directoriesToBackup
          .Select(entry => entry.DestinationDirectory));

      if (shortNames.Contains(string.Empty))
        throw new ArgumentException($"One of the enabled entries has an empty destination folder!");

      if (_directoriesToBackup.Length != shortNames.Count)
        throw new ArgumentException($"There are multiple enabled entries with the same destination folder!");

      string backupMainFolder = doc.BackupMainFolder;

      if (!Directory.Exists(backupMainFolder))
        throw new ArgumentException($"The base folder \"{backupMainFolder}\" does not exist!");

      _backupCentralContentStorageFolder = System.IO.Path.Combine(backupMainFolder, Current.BackupContentFolderName);
      _backupCentralNameStorageFolder = System.IO.Path.Combine(backupMainFolder, Current.BackupNameFolderName);
      _todaysBackupFolder = Path.Combine(backupMainFolder, doc.GetBackupTodaysDirectoryName());

      // we allow that today's backup folder already exist in order to make it possible to backup files from different
      // computers to the same today's backup directory
      // but what we will not allow is to backup files to already existing subdirectories of the today's backup folder
      foreach (var dirToBackup in _directoriesToBackup)
      {
        var dir = Path.Combine(_todaysBackupFolder, dirToBackup.DestinationDirectory);
        if (Directory.Exists(dir))
          throw new ArgumentException($"The destination backup folder {dir} exists already! Overwriting an existing backup folder is not supported. Please choose another folder for today's backup!");
      }

      _sha256Pool = new SHA256Pool();
      _bufferPool = new BufferPool();

      _producerCollection = new ItemProducerCollection(this, _directoriesToBackup);
      _readerQueue = new BlockingThreadedPumpWithNonblockingSideChannel<WorkerItem>(numberOfWorkerThreads: 1, maxNumberOfItemsInInputQueue: 10);
      _hasherQueue = new BlockingThreadedPump<WorkerItem>(numberOfWorkerThreads: 8, maxNumberOfItemsInInputQueue: 20);
      _writerQueue = new BlockingThreadedPump<WorkerItem>(numberOfWorkerThreads: 1, maxNumberOfItemsInInputQueue: 10);

      _producerCollection.ItemAvailable += EhProducer_ItemAvailable;
      _readerQueue.ItemAvailable += EhReaderQueue_ItemAvailable;
      _hasherQueue.ItemAvailable += EhHasherQueue_ItemAvailable;
      _writerQueue.ItemAvailable += EhWriterQueue_ItemAvailable;
    }

    public async Task Backup()
    {
      if (_producerCollection.IsDisposed || _readerQueue.IsDisposed || _writerQueue.IsDisposed || _hasherQueue.IsDisposed)
        throw new ObjectDisposedException("Worker is disposed already");

      _cancellationTokenSource = new CancellationTokenSource();


      if (!Directory.Exists(_backupCentralContentStorageFolder))
        Directory.CreateDirectory(_backupCentralContentStorageFolder);

      if (!Directory.Exists(_todaysBackupFolder))
        Directory.CreateDirectory(_todaysBackupFolder);

      var stopWatch = new Stopwatch();
      stopWatch.Start();

      await _producerCollection.Start(_cancellationTokenSource.Token);

      while (!_readerQueue.HasEmptyInputQueue || !_hasherQueue.HasEmptyInputQueue || !_writerQueue.HasEmptyInputQueue)
      {
        await Task.Delay(100);
      }

      while (_numberOfItemsCreated != _numberOfItemsProcessed)
      {
        await Task.Delay(100);
      }
      Duration = stopWatch.Elapsed;
      stopWatch.Stop();



      _producerCollection.Dispose();
      _readerQueue.Dispose();
      _hasherQueue.Dispose();
      _writerQueue.Dispose();

      _sha256Pool.Dispose();
      _bufferPool.Dispose();


    }

    #region Logic that links Collector, Reader, Hasher and Writer

    private void EhProducer_ItemAvailable(WorkerItem item)
    {
      if (item.IsFailed)
      {

      }
      else
      {
        Interlocked.Increment(ref _numberOfItemsCreated);
        NameOfProcessedFile = item.FullFileName;

        switch (item.NextStation)
        {
          case NextStation.Reader:
            _readerQueue.EnqueueBlocking(item);
            break;
          case NextStation.Writer:
            _writerQueue.EnqueueBlocking(item);
            break;
          default:
            _errorMessages.Enqueue($"Unexpected NextStation in CollectorOutput: {item.NextStation}");
            break;
        }

      }
    }

    private void EhReaderQueue_ItemAvailable(WorkerItem item)
    {
      item.Read();

      if (item.IsFailed)
      {
        Interlocked.Increment(ref _numberOfItemsProcessed);
      }
      else
      {
        _hasherQueue.EnqueueBlocking(item);
      }
    }

    private void EhHasherQueue_ItemAvailable(WorkerItem item)
    {
      item.Hash();

      if (item.IsFailed)
      {
        Interlocked.Increment(ref _numberOfItemsProcessed);
      }
      else
      {
        if (item.IsReadingCompleted)
          _writerQueue.EnqueueBlocking(item);
        else
          _readerQueue.EnqueueNonblocking(item);
      }
    }

    private void EhWriterQueue_ItemAvailable(WorkerItem item)
    {
      item.Write();

      switch (item.NextStation)
      {
        case NextStation.Reader:
          _readerQueue.EnqueueBlocking(item);
          break;
        case NextStation.Finished:
          Interlocked.Increment(ref _numberOfItemsProcessed);
          item.Dispose();
          break;
        default:
          _errorMessages.Enqueue($"Unexpected NextStation in WriterOutput: {item.NextStation}");
          break;
      }
    }

    #endregion




  }
}
