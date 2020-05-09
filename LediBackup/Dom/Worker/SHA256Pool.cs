/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LediBackup.Dom.Worker
{
  public class SHA256Pool : IDisposable
  {
    private ConcurrentBag<SHA256> _pool = new ConcurrentBag<SHA256>();

    public SHA256 LendFromPool()
    {
      if (_pool.TryTake(out var result))
      {
        return result;
      }
      else
      {
        return SHA256.Create();
      }
    }

    public void ReturnToPool(ref SHA256? instance)
    {
      if (instance is { } i)
      {

        ReturnToPool(i);
        instance = null;

      }
    }

    public void ReturnToPool(SHA256 instance)
    {
      _pool.Add(instance);
    }


    #region Byte buffer for SHA256

    private const int SizeOfBuffer = 256 / 8;
    private ConcurrentBag<byte[]> _byteBuffers = new ConcurrentBag<byte[]>();


    public byte[] LendBufferFromPool()
    {
      if (_byteBuffers.TryTake(out var buffer))
      {
        return buffer;
      }
      else
      {
        return new byte[SizeOfBuffer];
      }
    }

    private static readonly byte[] _nullBuffer = new byte[0];
    public void ReturnToPool(ref byte[] buffer)
    {
      if (buffer is { } b && b.Length == SizeOfBuffer)
      {
        _byteBuffers.Add(b);
      }
      buffer = _nullBuffer;
    }


    #endregion

    public void Dispose()
    {
      while (_pool.TryTake(out var result))
      {
        result.Dispose();
      }
    }
  }
}
