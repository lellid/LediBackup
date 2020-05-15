/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

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

    /// <summary>
    /// Given the file content hash, gets the full name of the central storage file.
    /// </summary>
    /// <param name="hash">The file content hash.</param>
    /// <returns>The full name of the directory, and the full name of the central storage file).</returns>
    /// <exception cref="NotImplementedException"></exception>
    public static (string DirectoryName, string FileFullName) GetNameOfCentralStorageFile(string backupCentralStorageFolder, byte[] hash)
    {
      if (!_stringBuilderPool.TryTake(out var stb))
        stb = new StringBuilder();

      stb.Clear();
      stb.Append(backupCentralStorageFolder);
      stb.Append(Path.DirectorySeparatorChar);

      for (int i = 0; i < 2; ++i)
      {
        stb.Append(hash[i].ToString("X2"));
        stb.Append(Path.DirectorySeparatorChar);
      }

      var directoryName = stb.ToString();

      for (int i = 0; i < hash.Length; ++i)
      {
        stb.Append(hash[i].ToString("X2"));
      }

      var fileName = stb.ToString();
      _stringBuilderPool.Add(stb);
      return (directoryName, fileName);
    }

    private static readonly ConcurrentBag<StringBuilder> _stringBuilderPool = new ConcurrentBag<StringBuilder>();


    #region Retrieving the number of links

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct BY_HANDLE_FILE_INFORMATION
    {
      public uint FileAttributes;
      public ulong CreationTime;
      public ulong LastAccessTime;
      public ulong LastWriteTime;
      public uint VolumeSerialNumber;
      public uint FileSizeHigh;
      public uint FileSizeLow;
      public uint NumberOfLinks;
      public uint FileIndexHigh;
      public uint FileIndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION fileInfo);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
        IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES structure can be passed
        [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
        IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(SafeFileHandle hObject);

    public static int GetNumberOfLinks(string fullName)
    {
      using (var handle = CreateFile(
         fullName,
         0, // 0: only access to get attributes
         0, // is ignored for only getting attributes
         IntPtr.Zero, // Security attributes
         FileMode.Open,
         FileAttributes.Normal,
         IntPtr.Zero
         ))
      {
        if (handle.IsInvalid)
        {
          Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        if (true == GetFileInformationByHandle(handle, out var fileInfo))
        {
          var numberOfLinks = (int)fileInfo.NumberOfLinks;
          return numberOfLinks;
        }
        else
        {
          var lastError = Marshal.GetLastWin32Error();
          throw new IOException($"Error GetFileInformationByHandle, ErrorCode: {lastError}");
        }
      }
    }

    #endregion
  }
}
