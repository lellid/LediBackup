/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LediBackup.Gui
{
  public interface IHelpAboutView
  {
  }

  public class HelpAboutController
  {

    #region Bindings

    public string Version
    {
      get
      {
        return "Version " + Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "Unknown";
      }
    }

    #endregion
  }
}
