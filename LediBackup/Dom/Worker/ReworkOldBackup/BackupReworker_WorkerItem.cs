/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LediBackup.Dom.Worker.ReworkOldBackup
{
  public partial class BackupReworker
  {
    public class WorkerItem
    {
      private BackupReworker _parent;
      private FileInfo _sourceFile;
      private Stream _stream;

      private byte[]? _buffer;
      private long _streamLength;
      private long _bytesReadTotal;
      private int _bytesInBuffer;
      private SHA256? _sha256;
      private string _centralStorageFileName;
      private string _centralStorageFolder;

      public bool IsReadingCompleted => _bytesReadTotal == _streamLength;

      public bool IsFailed { get; private set; }

      public bool IsDisposed { get; private set; }

      public string FullFileName => _sourceFile.FullName;

      public WorkerItem(BackupReworker parent, FileInfo sourceFile)
      {
        _parent = parent;
        _sourceFile = sourceFile;

        _centralStorageFileName = string.Empty;
        _centralStorageFolder = string.Empty;

        _buffer = null;
        _sha256 = null;

        try
        {
          _stream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
          _streamLength = _stream.Length;
        }
        catch (Exception ex)
        {
          _stream = Stream.Null;
          _streamLength = 0;
          OnFailed(ex);
        }
      }


      public void Read()
      {
        try
        {
          _buffer ??= _parent._bufferPool.LendFromPool(_streamLength + FileUtilities.BufferSpaceForLengthWriteTimeAndFileAttributes);
          _bytesInBuffer = 0;
          if (_bytesReadTotal == 0)
          {
            // Because all hard links to a file share the attributes and DateTimes,
            // it is neccessary to include at least creation time, write time and some attributes 
            // in the computed hash.
            // In this way we create some overhead, because files with the same content but different
            // attributes are stored in different files, but at the time of writing, this can not be helped.
            // Symbolic links are no alternative here, because the number of symbolic links in one directory is limited to 31
            _bytesInBuffer = FileUtilities.FillBufferWithLengthWriteTimeAndFileAttributes(_sourceFile, _buffer);
          }

          var bytesRead = _stream.Read(_buffer, _bytesInBuffer, _buffer.Length - _bytesInBuffer);
          _bytesInBuffer += bytesRead;
          _bytesReadTotal += bytesRead;
        }
        catch (Exception ex)
        {
          OnFailed(ex);
        }
      }

      public void Hash()
      {
        try
        {
          _sha256 ??= _parent._sha256Pool.LendFromPool();

          if (IsReadingCompleted)
          {
            _sha256.TransformFinalBlock(_buffer, 0, _bytesInBuffer);
            (_centralStorageFolder, _centralStorageFileName) = FileUtilities.GetNameOfCentralStorageFile(_parent._backupCentralStorageFolder, _sha256.Hash);
            _parent._sha256Pool.ReturnToPool(ref _sha256);

            // we don't need the stream and we don't need the buffer anymore
            _parent._bufferPool.ReturnToPool(ref _buffer);
            _stream?.Close();
            _stream?.Dispose();
            _stream = Stream.Null;
          }
          else
          {
            _sha256.TransformBlock(_buffer, 0, _bytesInBuffer, _buffer, 0);
          }
        }
        catch (Exception ex)
        {
          OnFailed(ex);
        }
      }

      public void Write()
      {
        try
        {
          if (File.Exists(_centralStorageFileName)) // central storage file already exists
          {
            // make a hardlink from the central storage file to the source file
            // but first we have to delete the source file
            if (_sourceFile.IsReadOnly)
            {
              _sourceFile.IsReadOnly = false;
            }
            _sourceFile.Delete();
            var hlr = FileUtilities.CreateHardLink(_centralStorageFileName, _sourceFile.FullName);
            if (hlr != 0)
            {
              if (hlr == FileUtilities.ERROR_TOO_MANY_LINKS)
              {
                // if the hardlink limit is exceeded, we need to make a full copy of the source file
                // effectively replacing the central storage file
                var tempFileName = FileUtilities.FileRenameToTemporaryFileInSameFolder(_centralStorageFileName);
                File.Copy(tempFileName, _centralStorageFileName);
                File.Delete(tempFileName);
                // now, try to create the hardlink again, now there should be no hardlink limit error any more 
                var hlr2 = FileUtilities.CreateHardLink(_centralStorageFileName, _sourceFile.FullName);
                if (hlr2 != 0)
                {
                  throw new System.IO.IOException($"Error create hard link from {_sourceFile.FullName} to {_centralStorageFileName}, ErrorCode: {hlr2}");
                }
              }
              else
              {
                throw new System.IO.IOException($"Error create hard link from {_sourceFile.FullName} to {_centralStorageFileName}, ErrorCode: {hlr}");
              }
            }
          }
          else // Central storage file does not exist yet
          {
            if (!Directory.Exists(_centralStorageFolder))
            {
              Directory.CreateDirectory(_centralStorageFolder);
            }

            // create a hard link from the existing (old) backup file to the central storage file
            int hlr = FileUtilities.CreateHardLink(_sourceFile.FullName, _centralStorageFileName);
            if (hlr != 0)
            {
              if (hlr == FileUtilities.ERROR_TOO_MANY_LINKS)
              {
                // if the hardlink limit is exceeded, we need to make a full copy instead of a
                // hard link
                File.Copy(_sourceFile.FullName, _centralStorageFileName);
              }
              else
              {
                throw new System.IO.IOException($"Error create hard link from {_sourceFile.FullName} to {_centralStorageFileName}");
              }
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
