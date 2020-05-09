﻿/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LediBackup.Dom
{
  public class FileUtilities
  {

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);


    /// <summary>
    /// Creates a hard link. Attention: the order of parameters is the same than in <see cref="System.IO.File.Copy(string, string)"/>, but is different than the C++ API!
    /// 
    /// </summary>
    /// <param name="existingFileName">Name of the existing file.</param>
    /// <param name="destinationFileName">Name of the destination file.</param>
    /// <returns>0 if the function has succeeded; otherwise, an non-zero error code.</returns>
    public static int CreateHardLink(string existingFileName, string destinationFileName)
    {
      return false == CreateHardLink(destinationFileName, existingFileName, IntPtr.Zero) ?
          Marshal.GetLastWin32Error() : 0;
    }

    /// <summary>
    /// The number that <see cref="CreateHardLink(string, string)"/> returns if the link limit of 1021 links is exceeded.
    /// </summary>
    public const int ERROR_TOO_MANY_LINKS = 0x476;


    /// <summary>
    /// Renames an existing file to a new file name in the same folder.
    /// </summary>
    /// <param name="fullFileName">Full name of the existing file.</param>
    /// <returns>The name of the renamed file.</returns>
    public static string FileRenameToTemporaryFileInSameFolder(string fullFileName)
    {
      var dir = Path.GetDirectoryName(fullFileName) ?? string.Empty;
      var newName = Path.Combine(dir, Guid.NewGuid().ToString());
      File.Move(fullFileName, newName);
      return newName;
    }

    /// <summary>
    /// Compares the relevant metadata of two files (length, last write time, and some attributes) and returns true if they match.
    /// </summary>
    /// <param name="file1">The first file.</param>
    /// <param name="file2">The second file.</param>
    /// <returns>True if the metadata match; otherwise, false.</returns>
    public static bool AreMetaDataMatching(FileInfo file1, FileInfo file2)
    {
      return
          file1.Length == file2.Length &&
          file1.LastWriteTime == file2.LastWriteTime &&
          (file1.Attributes & FileAttributeMask) == (file2.Attributes & FileAttributeMask);
    }

    public static readonly FileAttributes FileAttributeMask = (FileAttributes.Normal | FileAttributes.Hidden | FileAttributes.System | FileAttributes.SparseFile | FileAttributes.Encrypted);


    /// <summary>
    /// Fills the buffer with length and file attributes, beginning with buffer offset 0.
    /// </summary>
    /// <param name="sourceFile">The source file information.</param>
    /// <param name="buffer">The buffer.</param>
    /// <returns>The number of bytes written to the buffer.</returns>
    public static int FillBufferWithLengthWriteTimeAndFileAttributes(FileInfo sourceFile, byte[] buffer)
    {
      int maskedAttributes = (int)(sourceFile.Attributes & FileAttributeMask);
#if NETFRAMEWORK
            Buffer.BlockCopy(BitConverter.GetBytes(sourceFile.Length), 0, buffer, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(sourceFile.LastWriteTime.Ticks), 0, buffer, 8, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(maskedAttributes), 0, buffer, 16, 4);

#else
      BitConverter.TryWriteBytes(new Span<byte>(buffer, 0, 8), sourceFile.Length);
      BitConverter.TryWriteBytes(new Span<byte>(buffer, 8, 8), sourceFile.LastWriteTimeUtc.Ticks);
      BitConverter.TryWriteBytes(new Span<byte>(buffer, 16, 4), maskedAttributes);
#endif
      return BufferSpaceForLengthWriteTimeAndFileAttributes;
    }

    public const int BufferSpaceForLengthWriteTimeAndFileAttributes = 20;

  }
}
