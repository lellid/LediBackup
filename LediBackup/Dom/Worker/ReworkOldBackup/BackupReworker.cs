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

namespace LediBackup.Dom.Worker.ReworkOldBackup
{
  public partial class BackupReworker : IBackupWorker
  {

    /// <summary>
    /// The backup central storage folder. This folder contains all files of all backups.
    /// The files are names as the hashcode of its contents, without extension.
    /// To avoid too many files in a single folder, there are some subfolders below this folder,
    /// according to the first letters of the hash code.
    /// </summary>
    private string _backupCentralStorageFolder;

    private ItemProducer _producer;
    private BlockingThreadedPumpWithNonblockingSideChannel<WorkerItem> _readerQueue;
    private BlockingThreadedPump<WorkerItem> _writerQueue;
    private BlockingThreadedPump<WorkerItem> _hasherQueue;
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

    public BackupReworker(string backupMainFolder)
    {
      if (!Directory.Exists(backupMainFolder))
        throw new ArgumentException($"The base folder \"{backupMainFolder}\" does not exist!");

      _backupCentralStorageFolder = System.IO.Path.Combine(backupMainFolder, Current.BackupContentFolderName);
      _producer = new ItemProducer(this, backupMainFolder);
      _readerQueue = new BlockingThreadedPumpWithNonblockingSideChannel<WorkerItem>(numberOfWorkerThreads: 1, maxNumberOfItemsInInputQueue: 10);
      _hasherQueue = new BlockingThreadedPump<WorkerItem>(numberOfWorkerThreads: 8, maxNumberOfItemsInInputQueue: 20);
      _writerQueue = new BlockingThreadedPump<WorkerItem>(numberOfWorkerThreads: 1, maxNumberOfItemsInInputQueue: 10);
      _sha256Pool = new SHA256Pool();
      _bufferPool = new BufferPool();

      _producer.ItemAvailable += EhProducer_ItemAvailable;
      _readerQueue.ItemAvailable += EhReaderQueue_ItemAvailable;
      _hasherQueue.ItemAvailable += EhHasherQueue_ItemAvailable;
      _writerQueue.ItemAvailable += EhWriterQueue_ItemAvailable;
    }

    public async Task Backup()
    {
      if (_producer.IsDisposed || _readerQueue.IsDisposed || _writerQueue.IsDisposed || _hasherQueue.IsDisposed)
        throw new ObjectDisposedException("Worker is disposed already");

      _cancellationTokenSource = new CancellationTokenSource();


      if (!Directory.Exists(_backupCentralStorageFolder))
        Directory.CreateDirectory(_backupCentralStorageFolder);

      var stopWatch = new Stopwatch();
      stopWatch.Start();

      await _producer.Start(_cancellationTokenSource.Token);

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



      _producer.Dispose();
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
        _readerQueue.EnqueueBlocking(item);
        NameOfProcessedFile = item.FullFileName; // Diagnostics
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
      item.Dispose();
      Interlocked.Increment(ref _numberOfItemsProcessed);
    }

    #endregion


  }
}
