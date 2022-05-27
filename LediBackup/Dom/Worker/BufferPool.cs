/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace LediBackup.Dom.Worker
{
  public class BufferPool : IDisposable
  {
    private static readonly int _bigSize = 16 * 1024 * 1024; // 16 MB
    private static readonly int _mediumSize = 4 * 1024 * 1024; // 4 MB
    private static readonly int _smallSize = 1 * 1024 * 1024; // 1 MB

    private ConcurrentBag<byte[]> _bigSizedBuffers = new ConcurrentBag<byte[]>();
    private ConcurrentBag<byte[]> _mediumSizedBuffers = new ConcurrentBag<byte[]>();
    private ConcurrentBag<byte[]> _smallSizedBuffers = new ConcurrentBag<byte[]>();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicallyInstalledSystemMemory(out ulong MemoryInKilobytes);

    static BufferPool()
    {
      // we assume that we have at max 40 items in the queue (10 input + 20 hasher + 10 output)
      // but in order not to use up all the system memory, we assume we have maximal 64 items
      const int maxNumberOfItemsInAllQueues = 64;
      // even when there is a lot of memory installed, bigsize should not be greater than 1 GB because of the indexing with 32 bit integers
      const ulong maxMemoryInKilobytesForBackup = maxNumberOfItemsInAllQueues * 1024UL * 1024UL; // maximal 64 GB memory is used for backup
      const ulong minMemoryInKilobytesForBackup = 256UL * 1024UL; // minimal 256 MB memory is used for backup
      const ulong memoryInKilobytesForOperatingSystem = 4096 * 1024; // Windows needs at least 4 GB before swapping

      ulong systemMemoryInKilobytes;

      try
      {
        if (!GetPhysicallyInstalledSystemMemory(out systemMemoryInKilobytes))
          systemMemoryInKilobytes = memoryInKilobytesForOperatingSystem;
      }
      catch (Exception)
      {
        systemMemoryInKilobytes = memoryInKilobytesForOperatingSystem;
      }

      var memoryInKilobytesForBackup = (systemMemoryInKilobytes + minMemoryInKilobytesForBackup) <= memoryInKilobytesForOperatingSystem ? minMemoryInKilobytesForBackup : systemMemoryInKilobytes - memoryInKilobytesForOperatingSystem;
      if (memoryInKilobytesForBackup > maxMemoryInKilobytesForBackup)
      {
        memoryInKilobytesForBackup = maxMemoryInKilobytesForBackup;
      }

      _bigSize = (int)(1024 * (memoryInKilobytesForBackup / maxNumberOfItemsInAllQueues));
      _mediumSize = _bigSize / 4;
      _smallSize = _mediumSize / 4;
    }


    public byte[] LendFromPool(long minimumSize)
    {
      if (minimumSize > _mediumSize)
      {
        if (_bigSizedBuffers.TryTake(out var buffer))
        {
          return buffer;
        }
        else
        {
          return new byte[_bigSize];
        }
      }
      else if (minimumSize > _smallSize)
      {
        if (_mediumSizedBuffers.TryTake(out var buffer))
        {
          return buffer;
        }
        else
        {
          return new byte[_mediumSize];
        }
      }
      else
      {
        if (_smallSizedBuffers.TryTake(out var buffer))
        {
          return buffer;
        }
        else
        {
          return new byte[_smallSize];
        }
      }
    }

    public void ReturnToPool(ref byte[]? buffer)
    {
      if (buffer is { } b && b.Length > 0)
      {
        ReturnToPool(buffer);
      }

      buffer = null;
    }

    public void ReturnToPool(byte[] buffer)
    {
      if (buffer.Length >= _bigSize)
        _bigSizedBuffers.Add(buffer);
      else if (buffer.Length >= _mediumSize)
        _mediumSizedBuffers.Add(buffer);
      else if (buffer.Length >= _smallSize)
        _smallSizedBuffers.Add(buffer);
      else
        throw new InvalidOperationException("Returned buffer has not one of the designated sizes");
    }

    public void Dispose()
    {
      _bigSizedBuffers = new ConcurrentBag<byte[]>();
      _mediumSizedBuffers = new ConcurrentBag<byte[]>();
      _smallSizedBuffers = new ConcurrentBag<byte[]>();
      GC.Collect(3);
    }
  }
}
