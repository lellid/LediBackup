/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
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
  /// Interaction logic for MainListControl.xaml
  /// </summary>
  public partial class BackupDocumentControl : UserControl, IBackupDocumentView
  {
    public BackupDocumentControl()
    {
      InitializeComponent();
    }
  }
}
