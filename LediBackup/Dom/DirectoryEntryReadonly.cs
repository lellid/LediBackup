/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using LediBackup.Serialization.Xml;

namespace LediBackup.Dom
{
  public class DirectoryEntryReadonly
  {
    public string SourceDirectory { get; }
    public string DestinationDirectory { get; }
    public int MaxDepthOfSymbolicLinksToFollow { get; }
    public Filter.FilterItemCollectionReadonly ExcludedFiles { get; }


    public DirectoryEntryReadonly(DirectoryEntry entry)
    {
      SourceDirectory = entry.SourceDirectory;
      DestinationDirectory = entry.DestinationDirectory;
      MaxDepthOfSymbolicLinksToFollow = entry.MaxDepthOfSymbolicLinksToFollow;
      ExcludedFiles = new Filter.FilterItemCollectionReadonly(entry.ExcludedFiles);
    }
  }
}
