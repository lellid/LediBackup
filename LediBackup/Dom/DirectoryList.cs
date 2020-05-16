/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using LediBackup.Serialization.Xml;

namespace LediBackup.Dom
{
  public class DirectoryList : ObservableCollection<DirectoryEntry>
  {
    #region Serialization

    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(DirectoryList), 0)]
    private class SerializationSurrogate0 : IXmlSerializationSurrogate
    {
      public void Serialize(object o, IXmlSerializationInfo info)
      {
        var s = (DirectoryList)o ?? throw new ArgumentNullException(nameof(o));

        info.CreateArray("DirectoryList", s.Count);

        for (int i = 0; i < s.Count; ++i)
          info.AddValue("DirectoryEntry", s[i]);

        info.CommitArray();
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        var s = o as DirectoryList ?? new DirectoryList();
        s.Clear();
        var count = info.OpenArray("DirectoryList");
        for (int i = 0; i < count; ++i)
        {
          var entry = (DirectoryEntry)(info.GetValue("DirectoryEntry", s) ?? throw new InvalidOperationException());
          s.Add(entry);
        }
        info.CloseArray(count);
        return s;
      }
    }

    #endregion


  }
}
