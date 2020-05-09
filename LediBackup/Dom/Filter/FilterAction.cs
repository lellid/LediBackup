/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;

namespace LediBackup.Dom.Filter
{
  /// <summary>
  /// Enumerates the action for a filter item.
  /// </summary>
  [Serializable]
  public enum FilterAction
  {
    /// <summary>
    /// Include the item.
    /// </summary>
    Include = 0,

    /// <summary>
    /// Exclude the item.
    /// </summary>
    Exclude = 1,

    /// <summary>
    /// Ignore the item.
    /// </summary>
    Ignore = 2
  }
}
