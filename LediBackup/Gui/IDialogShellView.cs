/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;

namespace LediBackup.Gui
{
  /// <summary>
  /// This interface is intended to provide a "shell" as a dialog which can host a user control.
  /// </summary>
  public interface IDialogShellView
  {
    /// <summary>
    /// Sets if the Apply button should be visible.
    /// </summary>
    bool ApplyVisible { set; }

    /// <summary>
    /// Sets the title
    /// </summary>
    string Title { set; }

    event Action<System.ComponentModel.CancelEventArgs> ButtonOKPressed;

    event Action ButtonCancelPressed;

    event Action ButtonApplyPressed;
  }
}
