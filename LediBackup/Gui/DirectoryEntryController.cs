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
using System.Windows.Forms;
using System.Windows.Input;
using LediBackup.Dom;
using LediBackup.Dom.Filter;

namespace LediBackup.Gui
{
  public class DirectoryEntryController : INotifyPropertyChanged
  {
    private string _sourceDirectory;
    private string _destinationFolder;
    private int _maxDepthOfSymbolicLinksToFollow;
    public FilterItemCollection _filterItems;
    private FilterItemCollectionController _filterItemsController;

    private DirectoryEntry _doc;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


    public DirectoryEntryController(DirectoryEntry doc)
    {
      _doc = doc ?? throw new ArgumentNullException(nameof(doc));
      _sourceDirectory = doc.SourceDirectory;
      _destinationFolder = doc.DestinationDirectory;
      _maxDepthOfSymbolicLinksToFollow = doc.MaxDepthOfSymbolicLinksToFollow;
      _filterItems = doc.ExcludedFiles;
      _filterItemsController = new FilterItemCollectionController(_filterItems);
      CmdChooseSourceDirectory = new RelayCommand(EhChooseSourceDirectory);
    }


    public DirectoryEntryController() : this(new DirectoryEntry(string.Empty, string.Empty))
    {
    }

    public DirectoryEntry GetDocument()
    {
      _doc.SourceDirectory = _sourceDirectory;
      _doc.DestinationDirectory = _destinationFolder;
      // doc FilterList should be already up-to-date
      return _doc;

    }

    #region Bindings

    public string SourceDirectory
    {
      get => _sourceDirectory;
      set
      {
        if (!(_sourceDirectory == value))
        {
          _sourceDirectory = value;
          OnPropertyChanged(nameof(SourceDirectory));
        }
      }
    }

    public string DestinationFolder
    {
      get => _destinationFolder;
      set
      {
        if (!(_destinationFolder == value))
        {
          _destinationFolder = value;
          OnPropertyChanged(nameof(DestinationFolder));
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

    public FilterItemCollectionController FilterItemsController => _filterItemsController;

    public ICommand CmdChooseSourceDirectory { get; }

    #endregion



    private void EhChooseSourceDirectory()
    {
      var dlg = new FolderBrowserDialog();
      if (dlg.ShowDialog() == DialogResult.OK)
      {
        SourceDirectory = dlg.SelectedPath;
        DestinationFolder = System.IO.Path.GetFileName(dlg.SelectedPath);
      }
    }

  }
}
