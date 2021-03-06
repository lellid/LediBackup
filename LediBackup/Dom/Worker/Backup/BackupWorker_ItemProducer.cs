﻿/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LediBackup.Dom.Filter;

namespace LediBackup.Dom.Worker.Backup
{
  public partial class BackupWorker
  {
    /// <summary>
    /// Enumerate files in directories and subdirectories, and creates <see cref="WorkerItem"/>s from the file names.
    /// One single task is used to create the items.
    /// Backup directories that are located on the same physical hard disk
    /// should be bundled in only one CollectorItem (and not in multiple CollectorItems).
    /// This helps avoiding simultaneous disc access from multiple threads.
    /// </summary>
    public class ItemProducer
    {
      private BackupWorker _parent;
      private Dom.DirectoryEntryReadonly[] _entries;
      public bool IsDisposed { get; private set; }
      private CancellationToken _cancellationToken;



      /// <summary>
      /// Initializes a new instance of the <see cref="ItemProducer"/> class.
      /// </summary>
      /// <param name="parent">The parent backup worker instance.</param>
      /// <param name="entries">The directory entries that should be bundled in this CollectorItem. See class comment
      /// in which way the DirectoryEntries should be bundled.</param>
      public ItemProducer(BackupWorker parent, Dom.DirectoryEntryReadonly[] entries)
      {
        _parent = parent;
        _entries = entries;
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
        foreach (var e in _entries)
          BackupSingleBase(e);

        _cancellationToken = CancellationToken.None;
      }


      public void BackupSingleBase(Dom.DirectoryEntryReadonly entry)
      {
        if (_cancellationToken.IsCancellationRequested)
          return;

        var sourceDir = entry.SourceDirectory;
        if (!sourceDir.StartsWith(@"\\"))
          sourceDir = @"\\?\" + sourceDir;

        DirectoryInfo sourceDirectoryInfo;
        try
        {
          sourceDirectoryInfo = new DirectoryInfo(sourceDir);
          if (!sourceDirectoryInfo.Exists)
            throw new IOException("Directory does not exist!");
        }
        catch (Exception ex)
        {
          _parent._errorMessages.Enqueue($"Backup of folder {entry.SourceDirectory} failed: {ex.Message}");
          return;
        }


        var destinationDir = Path.Combine(_parent._todaysBackupFolder, entry.DestinationDirectory);
        if (!destinationDir.StartsWith(@"\\"))
          destinationDir = @"\\?\" + destinationDir;

        DirectoryInfo destinationDirectoryInfo;
        try
        {
          destinationDirectoryInfo = new DirectoryInfo(destinationDir);
          if (!destinationDirectoryInfo.Exists)
            destinationDirectoryInfo = Directory.CreateDirectory(destinationDir);
        }
        catch (Exception ex)
        {
          _parent._errorMessages.Enqueue($"Backup of folder {entry.SourceDirectory} failed. Could not create destination directory: {ex.Message}");
          return;
        }

        byte[] destinationNameBuffer;
        {
          // Create a small buffer and encode the destination directory (short form!)
          // that buffer becomes part of the hash of the file name
          // in this way, when we backup C:\Foo on two different computers, we can have different hashes, at least when
          // we use two different destination names
          var buffer = _parent._bufferPool.LendFromPool(65536);
          var destinationNameBufferLength = UnicodeEncoding.Unicode.GetBytes(entry.DestinationDirectory, 0, entry.DestinationDirectory.Length, buffer, 0);
          destinationNameBuffer = new byte[destinationNameBufferLength];
          Array.Copy(buffer, destinationNameBuffer, destinationNameBufferLength);
          _parent._bufferPool.ReturnToPool(buffer);
        }

        BackupSingleFolder(sourceDirectoryInfo, destinationDirectoryInfo, @"\", entry.ExcludedFiles, entry.MaxDepthOfSymbolicLinksToFollow, destinationNameBuffer);
      }

      public void BackupSingleFolder(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, string relativeFolderName, FilterItemCollectionReadonly filter, int symLinkLevel, byte[] destinationNameBuffer)
      {
        Exception? ex1 = null;

        try
        {
          var enumerable = sourceDirectory.EnumerateFiles();


          foreach (var sourceFile in enumerable)
          {
            if (_cancellationToken.IsCancellationRequested)
              break;

            var relativeFileName = string.Concat(relativeFolderName, sourceFile.Name.ToLowerInvariant());
            if (filter.IsPathIncluded(relativeFolderName + sourceFile.Name))
            {
              var readerItem = new WorkerItem(_parent, sourceFile, Path.Combine(destinationDirectory.FullName, sourceFile.Name), destinationNameBuffer);
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

            var relativeSubFolderName = string.Concat(relativeFolderName, subSourceDirectory.Name.ToLowerInvariant(), Path.DirectorySeparatorChar);
            if (filter.IsPathIncluded(relativeSubFolderName))
            {
              var subDestinationDirectory = destinationDirectory.CreateSubdirectory(subSourceDirectory.Name);

              int symLinkLevelLocally = symLinkLevel;
              if ((subSourceDirectory.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                --symLinkLevelLocally;

              if (symLinkLevelLocally >= 0)
              {
                BackupSingleFolder(subSourceDirectory, subDestinationDirectory, relativeSubFolderName, filter, symLinkLevelLocally, destinationNameBuffer);
              }
              else
              {
                _parent._errorMessages.Enqueue($"Folder ignored because user symlink limit was reached: {subSourceDirectory.FullName}");
              }
            }
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
