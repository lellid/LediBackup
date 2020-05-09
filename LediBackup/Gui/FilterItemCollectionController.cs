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
using System.Windows.Input;
using LediBackup.Dom.Filter;

namespace LediBackup.Gui
{
  public class FilterItemCollectionController : INotifyPropertyChanged
  {
    private FilterItemCollection Doc { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


    public FilterItemCollectionController(FilterItemCollection doc)
    {
      Doc = doc ?? throw new ArgumentNullException(nameof(doc));
      _filterPath = string.Empty;

      CmdAddExcludedPath = new RelayCommand(EhAddExcludedPath);
      CmdMoveUp = new RelayCommand(EhMoveUp);
      CmdMoveDown = new RelayCommand(EhMoveDown);
      CmdDelete = new RelayCommand(EhDelete);
    }

    private void EhAddExcludedPath()
    {
      if (!string.IsNullOrEmpty(FilterPath))
        Doc.Add(new FilterItem(FilterAction.Exclude, FilterPath));
    }

    private void EhMoveUp()
    {
      if (SelectedExcludedFile is { } selDir)
      {
        var idx = Doc.IndexOf(selDir);
        if (idx > 0)
        {
          Doc.Move(idx, idx - 1);
        }
      }
    }

    private void EhMoveDown()
    {
      if (SelectedExcludedFile is { } selDir)
      {
        var idx = Doc.IndexOf(selDir);
        if (idx >= 0 && idx < Doc.Count - 1)
        {
          Doc.Move(idx, idx + 1);
        }
      }
    }

    private void EhDelete()
    {
      if (SelectedExcludedFile is { } selDir)
      {
        var idx = Doc.IndexOf(selDir);
        if (idx >= 0)
        {
          Doc.RemoveAt(idx);
          SelectedExcludedFile = null;
        }
      }
    }

    #region Bindings

    private string _filterPath;

    public string FilterPath
    {
      get => _filterPath;
      set
      {
        if (!(_filterPath == value))
        {
          _filterPath = value;
          OnPropertyChanged(nameof(FilterPath));
        }
      }
    }

    public FilterItemCollection ExcludedFiles => Doc;

    private FilterItem? _selectedExcludedFile;

    public FilterItem? SelectedExcludedFile
    {
      get => _selectedExcludedFile;
      set
      {
        if (!object.ReferenceEquals(_selectedExcludedFile, value))
        {
          _selectedExcludedFile = value;
          OnPropertyChanged(nameof(SelectedExcludedFile));
        }
      }
    }

    public ICommand CmdAddExcludedPath { get; }

    public ICommand CmdMoveUp { get; }

    public ICommand CmdMoveDown { get; }

    public ICommand CmdDelete { get; }

    #endregion
  }
}
