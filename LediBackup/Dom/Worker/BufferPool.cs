/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace LediBackup.Dom.Worker
{
  public class BufferPool : IDisposable
  {
    private const int _bigSize = 512 * 1024 * 1024; // 512 MB
    private const int _mediumSize = 128 * 1024 * 1024; // 128 MB
    private const int _smallSize = 32 * 1024 * 1024; // 32 MB

    private ConcurrentBag<byte[]> _bigSizedBuffers = new ConcurrentBag<byte[]>();
    private ConcurrentBag<byte[]> _mediumSizedBuffers = new ConcurrentBag<byte[]>();
    private ConcurrentBag<byte[]> _smallSizedBuffers = new ConcurrentBag<byte[]>();


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
      switch (buffer.Length)
      {
        case _bigSize:
          _bigSizedBuffers.Add(buffer);
          break;
        case _mediumSize:
          _mediumSizedBuffers.Add(buffer);
          break;
        case _smallSize:
          _smallSizedBuffers.Add(buffer);
          break;
        default:
          throw new InvalidOperationException("Returned buffer has not one of the designated sized");
      }
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
