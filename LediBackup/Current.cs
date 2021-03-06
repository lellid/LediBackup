﻿/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LediBackup.Serialization;

namespace LediBackup
{

  public static class Current
  {
    public const string ApplicationName = "LediBackup";
    public const string BackupContentFolderName = "~CCS~";
    public const string BackupNameFolderName = "~CNS~";
    private static Dom.BackupDocument _project = new Dom.BackupDocument();

    public static Dom.BackupDocument Project
    {
      get => _project;
      set
      {
        if (!(object.ReferenceEquals(_project, value ?? throw new ArgumentNullException(nameof(Project)))))
        {
          if (_project is { } _)
            _project.PropertyChanged -= EhProjectPropertyChanged;

          _project = value;

          if (_project is { } _)
            _project.PropertyChanged += EhProjectPropertyChanged;

          OnPropertyChanged(nameof(Project));
        }
      }
    }


    public static Gui.BackupDocumentController? BackupDocumentController { get; set; }

    static Current()
    {
      Project = new Dom.BackupDocument();
    }

    private static void EhProjectPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(Dom.BackupDocument.IsDirty):
          OnPropertyChanged(nameof(IsDirty));
          break;
      }
    }

    private static string _fileName = string.Empty;
    public static string FileName
    {
      get => _fileName;
      set
      {
        if (!(_fileName == value))
        {
          _fileName = value;
          OnPropertyChanged(nameof(FileName));
        }
      }
    }

    public static bool IsDirty => Project.IsDirty;

    public static System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public static void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(null, new System.ComponentModel.PropertyChangedEventArgs(propertyName));


  }
}
