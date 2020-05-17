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
using System.Reflection;
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
  public partial class HelpManualControl : UserControl, IHelpAboutView
  {
    public HelpManualControl()
    {
      InitializeComponent();

      Loaded += EhLoaded;
    }

    private void EhLoaded(object sender, RoutedEventArgs e)
    {
      if (Assembly.GetEntryAssembly() is { } ass)
      {
        var stream = ass.GetManifestResourceStream("LediBackup.Manual.Manual.rtf");

        if (stream is { } _)
        {
          _guiRichTextBox.SelectAll();
          _guiRichTextBox.Selection.Load(stream, DataFormats.Rtf);

          stream.Close();
        }
      }
    }
  }
}
