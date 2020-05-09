/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LediBackup.Dom.Worker.Backup;
using LediBackup.Serialization.Xml;

namespace LediBackup.Dom
{
  public class BackupDocument : INotifyPropertyChanged
  {
    private DirectoryList _directories;
    private string _backupMainFolder;
    private BackupMode _backupMode;
    public bool _isDirty;


    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public BackupDocument()
    {
      _backupMainFolder = string.Empty;
      _backupMode = BackupMode.Fast;
      _directories = new DirectoryList();
      _directories.CollectionChanged += (s, e) => IsDirty = true;
    }

    public void CopyFrom(BackupDocument other)
    {
      if (other is null) throw new ArgumentNullException(nameof(other));

      BackupMainFolder = other.BackupMainFolder;
      _directories.CopyFrom(other._directories);
      BackupMode = other.BackupMode;
      IsDirty = other.IsDirty;
    }


    #region Serialization

    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(BackupDocument), 0)]
    private class SerializationSurrogate0 : IXmlSerializationSurrogate
    {
      public void Serialize(object o, IXmlSerializationInfo info)
      {
        var s = (BackupDocument)o ?? throw new ArgumentNullException(nameof(o));

        info.AddValue("BackupMode", s._backupMode);
        info.AddValue("BackupMainFolder", s._backupMainFolder);
        info.AddValue("BackupDirectories", s._directories);
        s.IsDirty = false;
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        var s = o as BackupDocument ?? new BackupDocument();

        if (info.CurrentElementName == "BackupMode")
          s.BackupMode = (BackupMode)info.GetEnum("BackupMode", typeof(BackupMode));

        s.BackupMainFolder = info.GetString("BackupMainFolder");

        var dirs = (DirectoryList)(info.GetValue("BackupDirectories", s) ?? throw new InvalidOperationException());

        s._directories.Clear();
        foreach (var d in dirs)
          s._directories.Add(d);

        s.IsDirty = false;

        return s;
      }
    }

    #endregion


    public DirectoryList Directories
    {
      get
      {
        return _directories;
      }
    }

    /// <summary>
    /// Gets or sets the directory's short name, in which a backup should be stored.
    /// This is only a single name, not a path.
    /// </summary>
    /// <value>
    /// The destination directory's short name
    /// </value>
    public string BackupMainFolder
    {
      get => _backupMainFolder;
      set
      {
        if (!(_backupMainFolder == value))
        {
          _backupMainFolder = value;
          OnPropertyChanged(nameof(BackupMainFolder));
          IsDirty = true;
        }
      }
    }

    /// <summary>
    /// Gets or sets the backup mode.
    /// </summary>
    /// <value>
    /// The backup mode.
    /// </value>
    public BackupMode BackupMode
    {
      get => _backupMode;
      set
      {
        if (!(_backupMode == value))
        {
          _backupMode = value;
          OnPropertyChanged(nameof(BackupMode));
          IsDirty = true;
        }
      }
    }

    public bool IsDirty
    {
      get => _isDirty;
      set
      {
        if (!(_isDirty == value))
        {
          _isDirty = value;
          OnPropertyChanged(nameof(IsDirty));
        }
      }
    }
  }
}
