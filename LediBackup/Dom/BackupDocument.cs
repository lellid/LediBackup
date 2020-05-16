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
    private string _backupTodaysDirectoryPreText;
    private BackupTodaysDirectoryMiddleTextType _backupTodaysDirectoryMiddleText;
    private string _backupTodaysDirectoryPostText;

    public bool _isDirty;


    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public BackupDocument()
    {
      _backupMode = BackupMode.Fast;
      _backupMainFolder = string.Empty;
      _backupTodaysDirectoryPreText = string.Empty;
      _backupTodaysDirectoryMiddleText = BackupTodaysDirectoryMiddleTextType.YearMonthDay;
      _backupTodaysDirectoryPostText = string.Empty;

      _directories = new DirectoryList();
      _directories.CollectionChanged += (s, e) => IsDirty = true;
    }

    public void CopyFrom(BackupDocument other)
    {
      if (other is null) throw new ArgumentNullException(nameof(other));

      BackupMode = other.BackupMode;
      BackupMainFolder = other.BackupMainFolder;
      BackupTodaysDirectoryPreText = other.BackupTodaysDirectoryPreText;
      BackupTodaysDirectoryMiddleText = other.BackupTodaysDirectoryMiddleText;
      BackupTodaysDirectoryPostText = other.BackupTodaysDirectoryPostText;
      _directories.CopyFrom(other._directories);
      IsDirty = other.IsDirty;
    }


    #region Serialization

    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(BackupDocument), 0)]
    private class SerializationSurrogate0 : IXmlSerializationSurrogate
    {
      public void Serialize(object o, IXmlSerializationInfo info)
      {
        var s = (BackupDocument)o ?? throw new ArgumentNullException(nameof(o));

        info.AddEnum("BackupMode", s._backupMode);
        info.AddValue("BackupMainFolder", s._backupMainFolder);
        info.AddValue("BackupTodaysFolderPreText", s._backupTodaysDirectoryPreText);
        info.AddEnum("BackupTodaysFolderMiddleText", s._backupTodaysDirectoryMiddleText);
        info.AddValue("BackupTodaysFolderPostText", s._backupTodaysDirectoryPostText);
        info.AddValue("BackupDirectories", s._directories);
        s.IsDirty = false;
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        var s = o as BackupDocument ?? new BackupDocument();

        s.BackupMode = (BackupMode)info.GetEnum("BackupMode", typeof(BackupMode));
        s.BackupMainFolder = info.GetString("BackupMainFolder");

        s.BackupTodaysDirectoryPreText = info.GetString("BackupTodaysFolderPreText");
        s.BackupTodaysDirectoryMiddleText = (BackupTodaysDirectoryMiddleTextType)info.GetEnum("BackupTodaysFolderMiddleText", typeof(BackupTodaysDirectoryMiddleTextType));
        s.BackupTodaysDirectoryPostText = info.GetString("BackupTodaysFolderPostText");

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

    public string BackupTodaysDirectoryPreText
    {
      get => _backupTodaysDirectoryPreText;
      set
      {
        if (!(_backupTodaysDirectoryPreText == value))
        {
          _backupTodaysDirectoryPreText = value;
          OnPropertyChanged(nameof(BackupTodaysDirectoryPreText));
          IsDirty = true;
        }
      }
    }

    public string BackupTodaysDirectoryPostText
    {
      get => _backupTodaysDirectoryPostText;
      set
      {
        if (!(_backupTodaysDirectoryPostText == value))
        {
          _backupTodaysDirectoryPostText = value;
          OnPropertyChanged(nameof(BackupTodaysDirectoryPostText));
          IsDirty = true;
        }
      }
    }

    public BackupTodaysDirectoryMiddleTextType BackupTodaysDirectoryMiddleText
    {
      get => _backupTodaysDirectoryMiddleText;
      set
      {
        if (!(_backupTodaysDirectoryMiddleText == value))
        {
          _backupTodaysDirectoryMiddleText = value;
          OnPropertyChanged(nameof(BackupTodaysDirectoryMiddleText));
          IsDirty = true;
        }
      }
    }

    public string GetBackupTodaysDirectoryName()
    {
      switch (_backupTodaysDirectoryMiddleText)
      {
        case BackupTodaysDirectoryMiddleTextType.YearMonthDay_HourMinuteSecond:
          return string.Concat(BackupTodaysDirectoryPreText, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"), BackupTodaysDirectoryPostText);
        case BackupTodaysDirectoryMiddleTextType.YearMonthDay:
          return string.Concat(BackupTodaysDirectoryPreText, DateTime.Now.ToString("yyyy-MM-dd"), BackupTodaysDirectoryPostText);
        case BackupTodaysDirectoryMiddleTextType.None:
          return string.Concat(BackupTodaysDirectoryPreText, BackupTodaysDirectoryPostText);
        default:
          throw new NotImplementedException();
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
