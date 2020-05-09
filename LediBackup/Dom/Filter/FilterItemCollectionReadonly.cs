/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using LediBackup.Serialization.Xml;

namespace LediBackup.Dom.Filter
{
  public class FilterItemCollectionReadonly
  {
    private FilterItem[] _filterItems;


    public FilterItemCollectionReadonly(FilterItemCollection from)
    {
      _filterItems = from.ToArray();
    }

    /// <summary>
    /// Determines whether the specified path is included. The path is always a relative path (relative to
    /// the backup directory, but should start with a DirectorySeparatorChar.
    /// </summary>
    /// <param name="relPathName">Name of the relative path.</param>
    /// <returns>
    ///   <c>true</c> if the path should be included in the action; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPathIncluded(string relPathName)
    {
      for (int i = 0; i < _filterItems.Length; ++i)
      {
        var action = _filterItems[i].DoesMatch(relPathName);
        switch (action)
        {
          case FilterAction.Exclude:
            return false;
          case FilterAction.Include:
            return true;
        }
      }

      return true;
    }
  }
}
