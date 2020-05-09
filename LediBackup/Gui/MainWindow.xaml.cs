/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LediBackup.Gui
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();

      Loaded += EhLoaded;
    }

    private void EhLoaded(object sender, RoutedEventArgs e)
    {
      Current.BackupDocumentController = new BackupDocumentController(Current.Project, _guiBackupControl);
      Current.PropertyChanged += EhCurrent_PropertyChanged;
      UpdateMainWindowTitle();
    }

    private void EhCurrent_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(Current.IsDirty):
          UpdateMainWindowTitle();
          break;
        case nameof(Current.FileName):
          UpdateMainWindowTitle();
          break;
      }
    }

    public void UpdateMainWindowTitle()
    {
      Title = string.Concat(
        Current.ApplicationName,
        " - ",
        string.IsNullOrEmpty(Current.FileName) ? "Untitled" : Current.FileName,
        Current.IsDirty ? "*" : ""
        );
    }
  }
}
