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

    private Collector _collector;
    private Reader _reader;
    private Writer _writer;
    private Hasher _hasher;
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

    public int NumberOfItemsInReader => _reader.NumberOfItemsInInputQueue;
    public int NumberOfItemsInHasher => _hasher.NumberOfItemsInInputQueue;
    public int NumberOfItemsInWriter => _writer.NumberOfItemsInInputQueue;

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
      _collector = new Collector(this, backupMainFolder);
      _reader = new Reader();
      _hasher = new Hasher();
      _writer = new Writer();
      _sha256Pool = new SHA256Pool();
      _bufferPool = new BufferPool();

      _collector.OutputAvailable += EhCollector_OutputAvailable;
      _reader.OutputAvailable += EhReader_OutputAvailable;
      _hasher.OutputAvailable += EhHasher_OutputAvailable;
      _writer.OutputAvailable += EhWriter_OutputAvailable;
    }

    public async Task Backup()
    {
      if (_collector.IsDisposed || _reader.IsDisposed || _writer.IsDisposed || _hasher.IsDisposed)
        throw new ObjectDisposedException("Worker is disposed already");

      _cancellationTokenSource = new CancellationTokenSource();


      if (!Directory.Exists(_backupCentralStorageFolder))
        Directory.CreateDirectory(_backupCentralStorageFolder);

      var stopWatch = new Stopwatch();
      stopWatch.Start();

      await _collector.Start(_cancellationTokenSource.Token);

      while (!_reader.HasEmptyInputQueue || !_hasher.HasEmptyInputQueue || !_writer.HasEmptyInputQueue)
      {
        await Task.Delay(100);
      }

      while (_numberOfItemsCreated != _numberOfItemsProcessed)
      {
        await Task.Delay(100);
      }
      Duration = stopWatch.Elapsed;
      stopWatch.Stop();



      _collector.Dispose();
      _reader.Dispose();
      _hasher.Dispose();
      _writer.Dispose();

      _sha256Pool.Dispose();
      _bufferPool.Dispose();


    }

    #region Logic that links Collector, Reader, Hasher and Writer

    private void EhCollector_OutputAvailable(WorkerItem item)
    {
      if (item.IsFailed)
      {

      }
      else
      {
        Interlocked.Increment(ref _numberOfItemsCreated);
        _reader.WaitForEnqueue(item);
        NameOfProcessedFile = item.FullFileName;
      }
    }

    private void EhReader_OutputAvailable(WorkerItem item)
    {
      if (item.IsFailed)
      {
        Interlocked.Increment(ref _numberOfItemsProcessed);
      }
      else
      {
        _hasher.Enqueue(item);
      }
    }

    private void EhHasher_OutputAvailable(WorkerItem item)
    {
      if (item.IsFailed)
      {
        Interlocked.Increment(ref _numberOfItemsProcessed);
      }
      else
      {
        if (item.IsReadingCompleted)
          _writer.WaitForEnqueue(item);
        else
          _reader.Enqueue(item);
      }
    }

    private void EhWriter_OutputAvailable(WorkerItem item)
    {
      Interlocked.Increment(ref _numberOfItemsProcessed);
    }

    #endregion

    /// <summary>
    /// Given the file content hash, gets the full name of the central storage file.
    /// </summary>
    /// <param name="hash">The file content hash.</param>
    /// <returns>The full name of the directory, and the full name of the central storage file).</returns>
    /// <exception cref="NotImplementedException"></exception>
    private (string DirectoryName, string FileFullName) GetNameOfCentralStorageFile(byte[] hash)
    {
      var stb = new StringBuilder();

      stb.Clear();
      stb.Append(_backupCentralStorageFolder);
      stb.Append(Path.DirectorySeparatorChar);

      for (int i = 0; i < 3; ++i)
      {
        stb.Append(hash[i].ToString("X2"));
        stb.Append(Path.DirectorySeparatorChar);
      }

      var directoryName = stb.ToString();

      for (int i = 0; i < hash.Length; ++i)
      {
        stb.Append(hash[i].ToString("X2"));
      }
      return (directoryName, stb.ToString());
    }
  }
}
