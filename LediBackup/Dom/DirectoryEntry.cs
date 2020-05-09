/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using LediBackup.Serialization.Xml;

namespace LediBackup.Dom
{
  public class DirectoryEntry : INotifyPropertyChanged, ICloneable
  {
    private bool _isEnabled;
    private string _sourceDirectory;
    private string _destinationDirectory;
    private int _maxDepthOfSymbolicLinksToFollow;
    private Filter.FilterItemCollection _excludedFiles;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


    #region Serialization

    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(DirectoryEntry), 0)]
    private class SerializationSurrogate0 : IXmlSerializationSurrogate
    {
      public void Serialize(object o, IXmlSerializationInfo info)
      {
        var s = (DirectoryEntry)o ?? throw new ArgumentNullException(nameof(o));

        info.AddValue("IsEnabled", s.IsEnabled);
        info.AddValue("SourceDirectory", s.SourceDirectory);
        info.AddValue("DestinationDirectory", s.DestinationDirectory);
        info.AddValue("MaxDepthOfSymbolicLinksToFollow", s.MaxDepthOfSymbolicLinksToFollow);
        info.AddValue("Filter", s._excludedFiles);
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        var isEnabled = info.GetBoolean("IsEnabled");
        var sourceDirectory = info.GetString("SourceDirectory");
        var destinationDirectory = info.GetString("DestinationDirectory");
        var maxDepthOfSymbolicLinksToFollow = (info.CurrentElementName == "MaxDepthOfSymbolicLinksToFollow") ? info.GetInt32("MaxDepthOfSymbolicLinksToFollow") : 16;
        var filter = (Filter.FilterItemCollection)(info.GetValue("Filter", null) ?? throw new InvalidOperationException());

        return new DirectoryEntry(sourceDirectory, destinationDirectory)
        {
          IsEnabled = isEnabled,
          _excludedFiles = filter,
          _maxDepthOfSymbolicLinksToFollow = maxDepthOfSymbolicLinksToFollow,
        };
      }
    }

    #endregion

    public DirectoryEntry(string sourceDirectory, string destinationDirectory)
    {
      _sourceDirectory = sourceDirectory ?? throw new ArgumentNullException(nameof(sourceDirectory));
      _destinationDirectory = destinationDirectory ?? throw new ArgumentNullException(nameof(destinationDirectory));
      _maxDepthOfSymbolicLinksToFollow = 4;
      _excludedFiles = new Filter.FilterItemCollection
            {
                new Filter.FilterItem(Filter.FilterAction.Exclude, @"\System Volume Information\*"),
                new Filter.FilterItem(Filter.FilterAction.Exclude, @"\$RECYCLE.BIN\*")
            };
      IsEnabled = true;
    }

    public object Clone()
    {
      var result = new DirectoryEntry(this.SourceDirectory, this.DestinationDirectory)
      {
        _isEnabled = this._isEnabled,
        _excludedFiles = (Filter.FilterItemCollection)this._excludedFiles.Clone()
      };

      return result;
    }


    /// <summary>
    /// Gets or sets the directory (full path), for which a backup should be made.
    /// </summary>
    /// <value>
    /// The directory.
    /// </value>
    public string SourceDirectory
    {
      get => _sourceDirectory;
      set
      {
        TestValidityOfAbsoluteDirectoryName(value, nameof(SourceDirectory));
        if (!(_sourceDirectory == value))
        {
          _sourceDirectory = value;
          OnPropertyChanged(nameof(SourceDirectory));
        }
      }
    }

    public void TestValidityOfAbsoluteDirectoryName(string s, string name)
    {
      if (string.IsNullOrEmpty(s))
        throw new ArgumentException($"{name} is null or empty!", name);
      if (s.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
        throw new ArgumentException($"{name} contains invalid path characters!", name);
      if (!Path.IsPathRooted(s))
        throw new ArgumentException($"{name} path is not rooted (absolute)!", name);
    }



    /// <summary>
    /// Gets or sets the directory's short name, in which a backup should be stored.
    /// This is only a single name, not a path.
    /// </summary>
    /// <value>
    /// The destination directory's short name
    /// </value>
    public string DestinationDirectory
    {
      get => _destinationDirectory;
      set
      {
        TestValidityOfShortDirectoryName(value, nameof(DestinationDirectory));

        if (!(_destinationDirectory == value))
        {
          _destinationDirectory = value;
          OnPropertyChanged(nameof(DestinationDirectory));
        }
      }
    }

    public int MaxDepthOfSymbolicLinksToFollow
    {
      get => _maxDepthOfSymbolicLinksToFollow;
      set
      {
        if (!(_maxDepthOfSymbolicLinksToFollow == value))
        {
          _maxDepthOfSymbolicLinksToFollow = value;
          OnPropertyChanged(nameof(MaxDepthOfSymbolicLinksToFollow));
        }
      }
    }


    public void TestValidityOfShortDirectoryName(string s, string name)
    {
      if (string.IsNullOrEmpty(s))
        throw new ArgumentException($"{name} is null or empty!", name);
      if (s.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
        throw new ArgumentException($"{name} contains invalid path characters!", name);
      if (s.IndexOf(Path.DirectorySeparatorChar) >= 0 || s.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
        throw new ArgumentException($"{name} must not contain a DirectorySeparatorChar ({Path.DirectorySeparatorChar}) or AltDirectorySeparatorChar ({Path.AltDirectorySeparatorChar})!", name);
    }

    /// <summary>
    /// Gets or sets a value, whether this directory should be backuped now.
    /// </summary>
    public bool IsEnabled
    {
      get => _isEnabled;
      set
      {
        if (!(_isEnabled == value))
        {
          _isEnabled = value;
          OnPropertyChanged(nameof(IsEnabled));
        }
      }
    }

    public Filter.FilterItemCollection ExcludedFiles => _excludedFiles;
  }
}
