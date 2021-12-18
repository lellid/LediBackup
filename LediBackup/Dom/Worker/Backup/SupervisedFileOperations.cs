using System;
using System.IO;

namespace LediBackup.Dom.Worker.Backup
{
  public class SupervisedFileOperations
  {
    public int NumberOfRetries { get; }
    public TimeSpan RetryTime { get; }

    public SupervisedFileOperations() : this(6, TimeSpan.FromSeconds(10)) { }
    public SupervisedFileOperations(int numberOfRetries, TimeSpan retryTime)
    {
      NumberOfRetries = numberOfRetries;
      RetryTime = retryTime;
    }

    public bool FileExists(string fileName)
    {
      for (int retries = NumberOfRetries; ; )
      {
        try
        {
          return File.Exists(fileName);
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }

    public bool DirectoryExists(string folderName)
    {
      for (int retries = NumberOfRetries; ;)
      {
        try
        {
          return Directory.Exists(folderName);
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }

    public DirectoryInfo CreateDirectory(string folderName)
    {
      for (int retries = NumberOfRetries; ;)
      {
        try
        {
          return Directory.CreateDirectory(folderName);
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }

    public void FileDelete(string fileName)
    {
      for (int retries = NumberOfRetries; ;)
      {
        try
        {
          File.Delete(fileName);
          return;
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }

    public void FileCopy(string srcFile, string dstFile)
    {
      for (int retries = NumberOfRetries; ;)
      {
        try
        {
          File.Copy(srcFile, dstFile);
          return;
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }

    public void FileSetLastWriteTimeUtc(string srcFile, DateTime lastWriteTime)
    {
      for (int retries = NumberOfRetries; ;)
      {
        try
        {
          File.SetLastWriteTimeUtc(srcFile, lastWriteTime);
          return;
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }

    public void FileSetAttributes(string srcFile, FileAttributes attributes)
    {
      for (int retries = NumberOfRetries; ;)
      {
        try
        {
          File.SetAttributes(srcFile, attributes);
          return;
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }

    public void Execute(Action action)
    {
      for (int retries = NumberOfRetries; ;)
      {
        try
        {
          action();
          return;
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }

    public T Execute<T>(Func<T> action)
    {
      for (int retries = NumberOfRetries; ;)
      {
        try
        {
          return action();
        }
        catch (Exception)
        {
          if (retries > 0)
          {
            --retries;
            System.Threading.Thread.Sleep(RetryTime);
          }
          else
          {
            throw;
          }
        }
      }
    }


  }
}
