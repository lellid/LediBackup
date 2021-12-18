/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace LediBackup.Dom.Worker.Backup
{
  public partial class BackupWorker
  {
    /// <summary>
    /// Designates the next station the item should be processed.
    /// </summary>
    public enum NextStation
    {
      Reader,
      Hasher,
      Writer,
      Finished
    }

    /// <summary>
    /// Item that is used to backup a file, using different stages like creation, reading, hashing, writing.
    /// </summary>
    /// <remarks>
    /// To backup an (original) file, up to three files could be created on the backup drive:
    /// (i) the backup file.
    /// (ii) the central content file, and
    /// (iii) the central name file.
    /// All three files, the backup file, the central content file and the central name file have the same data content and are (ideally) linked by hard links.
    /// The name of the backup file is the same as the name of the original file.
    /// The name of the central content file is determined by the hash of the file content.
    /// The name of the central name file is determined by the hash of the original file's metadata (length, last write time, attributes, and full name).
    /// </remarks>
    public class WorkerItem
    {
      private BackupWorker _parent;
      private FileInfo _sourceFile;
      private Stream _stream;
      private string _destinationFileName;

      private byte[]? _buffer;
      private long _streamLength;
      private long _bytesReadTotal;
      private int _bytesInBuffer;
      private SHA256? _sha256;
      private string _centralContentFileName;
      private string _centralContentFolder;
      private string _centralNameFileName;

      /// <summary>Designates the next station this worker item should be routed to.</summary>
      public NextStation NextStation { get; private set; }

      /// <summary>True if the contents of the file was completely read (and hashed).</summary>
      public bool IsReadingCompleted => _bytesReadTotal == _streamLength;

      /// <summary>True if this worker item has failed (some exception happened).</summary>
      public bool IsFailed { get; private set; }

      /// <summary>True if this worker item has beend disposed of.</summary>
      public bool IsDisposed { get; private set; }

      /// <summary>Full file name of the source file that is backuped.</summary>
      public string FullFileName => _sourceFile.FullName;

      /// <summary>True if the name of the central content file is known (by calculating the hash of the content).</summary>
      public bool IsContentFileNameKnown => !string.IsNullOrEmpty(_centralContentFileName);

      public SupervisedFileOperations FileOps => _parent.FileOperations;

      public WorkerItem(BackupWorker parent, FileInfo sourceFile, string destinationFileName, byte[] nameBufferPreText)
      {
        _parent = parent;
        _sourceFile = sourceFile;
        _destinationFileName = destinationFileName;

        _centralContentFileName = string.Empty;
        _centralContentFolder = string.Empty;
        _centralNameFileName = string.Empty;

        _buffer = null;
        _sha256 = null;

        var backupMode = _parent._backupMode;
        if (backupMode == BackupMode.Fast)
        {
          EvaluateCentralNameFileName(nameBufferPreText);
          NextStation = NextStation.Writer; // we first try to use the central name file
        }
        else
        {
          NextStation = NextStation.Reader; // in all other cases: read and hash the content
        }

        _stream = Stream.Null;
        _streamLength = 0;
      }

      /// <summary>
      /// Evaluates the name of the central name file, by calculating a hash from the name of the file.
      /// Note that metadata like length, dates and attributes are not included in the hash.
      /// This is because they can easily compared using the metadata of the central name file.
      /// </summary>
      public void EvaluateCentralNameFileName(byte[] nameBufferPreText)
      {
        var sha256 = _parent._sha256Pool.LendFromPool();
        var buffer = _parent._bufferPool.LendFromPool(65536);
        Array.Copy(nameBufferPreText, buffer, nameBufferPreText.Length);
        var bufferBytes = nameBufferPreText.Length + UnicodeEncoding.Unicode.GetBytes(_sourceFile.FullName, 0, _sourceFile.FullName.Length, buffer, nameBufferPreText.Length);
        sha256.TransformFinalBlock(buffer, 0, bufferBytes);
        (_, _centralNameFileName) = FileUtilities.GetNameOfCentralStorageFile(_parent._backupCentralNameStorageFolder, sha256.Hash);
        _parent._bufferPool.ReturnToPool(buffer);
        _parent._sha256Pool.ReturnToPool(sha256);
      }

      /// <summary>
      /// Try to link to an existing central name file.
      /// If the central name file exist, and the link limit is not exceeded, the destination file is linked to the central name file, and the return value is true.
      /// In this case, the backup of the destination file is considered finished.
      /// If the central name file does not exist, or linking fails due to an exceeded link limit, the return value is false.
      /// </summary>
      /// <returns>True if linking of the destination file to the existing central name file succeeded;, otherwise, false.</returns>
      /// <exception cref="IOException">Error creating hard link from {_centralNameFileName} to {_destinationFileName}, ErrorCode: {hlr}</exception>
      public bool TryToLinkToExistingNameFile()
      {
        var fileInfoCentralNameFileName = new FileInfo(_centralNameFileName);

        // Compare the FileInfo of the central name file with the FileInfo of the source file
        // only if both match, we can link the destination file name to the centralNameFile
        if (fileInfoCentralNameFileName.Exists && FileUtilities.AreMetaDataMatching(fileInfoCentralNameFileName, _sourceFile))
        {
          var hlr = FileUtilities.CreateHardLink(_centralNameFileName, _destinationFileName);
          if (hlr == 0)
          {
            return true;
          }
          else
          {
            if (hlr != FileUtilities.ERROR_TOO_MANY_LINKS) // ignore TooManyLinks error, instead let the content file be created anew
            {
              throw new IOException($"Error creating hard link from {_centralNameFileName} to {_destinationFileName}, ErrorCode: {hlr}");
            }
          }
        }
        return false;
      }

      /// <summary>
      /// Reads the content of the source file in a buffer, in order to hash the content. 
      /// Since some file metadata are hashed, too, at the first read, the buffer is pre-filled with
      /// file length, last write time and attributes. Then the contents of the file follows.
      /// </summary>
      public void Read()
      {
        try
        {
          if (object.ReferenceEquals(_stream, Stream.Null))
          {
            
            _stream = FileOps.Execute(() => new FileStream(_sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
            _streamLength = _stream.Length;
          }

          _buffer ??= _parent._bufferPool.LendFromPool(_streamLength + FileUtilities.BufferSpaceForLengthWriteTimeAndFileAttributes);
          _bytesInBuffer = 0;
          if (_bytesReadTotal == 0)
          {
            // Because all hard links to a file share the attributes and DateTimes,
            // it is neccessary to include at write time and some attributes in the computed hash.
            // In this way we create some overhead, because files with the same content but different
            // attributes are stored in different files, but at the time of writing, this can not be helped.
            // Symbolic links are no alternative here, because the number of symbolic links in one directory is limited to 31
            _bytesInBuffer = FileUtilities.FillBufferWithLengthWriteTimeAndFileAttributes(_sourceFile, _buffer);
          }

          var bytesRead = _stream.Read(_buffer, _bytesInBuffer, _buffer.Length - _bytesInBuffer);
          _bytesInBuffer += bytesRead;
          _bytesReadTotal += bytesRead;
          NextStation = NextStation.Hasher;
        }
        catch (Exception ex)
        {
          OnFailed(ex);
        }
      }

      /// <summary>
      /// Hashes the content of the buffer. If the content was completely read, the hash is finally calculated and the name of the 
      /// central storage file is determined from the hash. If the content was not completely read, the worker item is given back to the reader.
      /// </summary>
      public void Hash()
      {
        try
        {
          _sha256 ??= _parent._sha256Pool.LendFromPool();

          if (IsReadingCompleted)
          {
            _sha256.TransformFinalBlock(_buffer, 0, _bytesInBuffer);
            (_centralContentFolder, _centralContentFileName) = FileUtilities.GetNameOfCentralStorageFile(_parent._backupCentralContentStorageFolder, _sha256.Hash);
            _parent._sha256Pool.ReturnToPool(ref _sha256);

            // if this was a big file, so that the buffer contains only a part of a file
            // the buffer is of no use any more and can be returned to the pool
            if (_bytesInBuffer != (_streamLength + FileUtilities.BufferSpaceForLengthWriteTimeAndFileAttributes))
              _parent._bufferPool.ReturnToPool(ref _buffer);

            NextStation = NextStation.Writer;
          }
          else
          {
            _sha256.TransformBlock(_buffer, 0, _bytesInBuffer, _buffer, 0);
            NextStation = NextStation.Reader;
          }
        }
        catch (Exception ex)
        {
          OnFailed(ex);
        }
      }

      /// <summary>
      /// Writes the backup file, the central content file and the central name file.
      /// </summary>
      /// <remarks>
      /// All three files, the backup file, the central content file and the central name file have the same data content and are (ideally) linked by hard links.
      /// The name of the backup file is the same as the name of the original file.
      /// The name of the central content file is determined by the hash of the file content.
      /// The name of the central name file is determined by the hash of the original file's metadata (length, last write time, attributes, and full name).
      /// </remarks>
      public void Write()
      {
        try
        {
          var backupMode = _parent._backupMode;
          if (backupMode == BackupMode.Fast && TryToLinkToExistingNameFile())
          {
            NextStation = NextStation.Finished;
          }
          else
          {
            if (!IsContentFileNameKnown)
            {
              NextStation = NextStation.Reader;
            }
            else
            {
              bool isRepeatRequired = false;
              do
              {
                if (!FileOps.FileExists(_centralContentFileName))
                {
                  if (!FileOps.DirectoryExists(_centralContentFolder))
                    FileOps.CreateDirectory(_centralContentFolder);

                  if (_bytesInBuffer == (_streamLength + FileUtilities.BufferSpaceForLengthWriteTimeAndFileAttributes) && _buffer is { } buffer)
                  {
                    FileOps.Execute(() =>
                    {
                      using (var destStream = new FileStream(_centralContentFileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                      {
                        destStream.Write(_buffer, FileUtilities.BufferSpaceForLengthWriteTimeAndFileAttributes, _bytesInBuffer - FileUtilities.BufferSpaceForLengthWriteTimeAndFileAttributes);
                      }
                    }
                    );
                    // Set attributes on the destination file, such as last write time, and attributes.
                    FileOps.FileSetLastWriteTimeUtc(_centralContentFileName, _sourceFile.LastWriteTimeUtc);
                    FileOps.FileSetAttributes(_centralContentFileName, _sourceFile.Attributes & FileUtilities.FileAttributeMask);
                  }
                  else
                  {
                    // we do not use the destination stream here
                    FileOps.FileCopy(_sourceFile.FullName, _centralContentFileName);
                    // note: when copying, attributes and LastWriteTime is already set.
                  }
                }

                {
                  isRepeatRequired = false;
                  // Central storage file now exists, so create a hard link to it in the backup directory
                  var hlr = FileUtilities.CreateHardLink(_centralContentFileName, _destinationFileName);
                  if (hlr != 0)
                  {
                    if (hlr == FileUtilities.ERROR_TOO_MANY_LINKS) // is hard link limit exceeded?
                    {
                      // in order to circumvent the hard link limit, we delete the file from the central storage, and create it anew
                      FileOps.FileDelete(_centralContentFileName);
                      isRepeatRequired = true;
                      continue;
                    }
                    else
                    {
                      throw new IOException($"Error creating hard link {_destinationFileName}, ErrorCode: {hlr}");
                    }
                  }
                }

                // Handle the name file (except when in BackupMode.SecureNoNameFile)
                if (backupMode == BackupMode.Fast && !FileOps.FileExists(_centralNameFileName))
                {
                  var folder = Path.GetDirectoryName(_centralNameFileName);
                  if (!FileOps.DirectoryExists(folder))
                    FileOps.CreateDirectory(folder);
                  var hlr = FileUtilities.CreateHardLink(_centralContentFileName, _centralNameFileName);
                  if (hlr != 0)
                  {
                    if (hlr == FileUtilities.ERROR_TOO_MANY_LINKS)
                      FileOps.FileDelete(_centralNameFileName);
                    else
                      throw new IOException($"Error creating hard link {_destinationFileName}, ErrorCode: {hlr}");
                  }
                }
              } while (isRepeatRequired);
              _stream?.Close();
              _stream?.Dispose();
              NextStation = NextStation.Finished;
            }
          }
        }
        catch (Exception ex)
        {
          OnFailed(ex);
        }
      }

      public void OnFailed(Exception ex)
      {
        IsFailed = true;
        _parent._failedFiles.Add((_sourceFile, ex));
        _parent._errorMessages.Enqueue($"File {_sourceFile.FullName}: {ex.Message}");
        NextStation = NextStation.Finished;
        Dispose();

      }

      public void Dispose()
      {
        if (!IsDisposed)
        {
          _stream?.Close();
          _stream?.Dispose();
          _parent._bufferPool.ReturnToPool(ref _buffer);
          _parent._sha256Pool.ReturnToPool(ref _sha256);
          IsDisposed = true;
        }
      }
    }

  }
}
