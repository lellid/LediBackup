/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
  /// Interaction logic for HelpAboutControl.xaml
  /// </summary>
  public partial class HelpAboutControl : UserControl, IHelpAboutView
  {
    public HelpAboutControl()
    {
      InitializeComponent();

      var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

      _guiVersionText.Text = string.Format("Version {0}", version);
    }

    private void EhOpenGitHub(object sender, RequestNavigateEventArgs e)
    {
      var url = e.Uri.ToString();
      var psi = new ProcessStartInfo
      {
        FileName = url,
        UseShellExecute = true
      };
      Process.Start(psi);
    }
  }
}
