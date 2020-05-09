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

namespace LediBackup.Dom.Filter
{
  public class FilterItemCollection : ObservableCollection<FilterItem>, ICloneable
  {

    #region Serialization

    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(FilterItemCollection), 0)]
    private class SerializationSurrogate0 : IXmlSerializationSurrogate
    {
      public void Serialize(object o, IXmlSerializationInfo info)
      {
        var s = (FilterItemCollection)o ?? throw new ArgumentNullException(nameof(o));

        info.CreateArray("FilterItems", s.Count);

        for (int i = 0; i < s.Count; ++i)
          info.AddValue("FilterItem", s[i]);

        info.CommitArray();
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        var s = o as FilterItemCollection ?? new FilterItemCollection();
        s.Clear();
        var count = info.OpenArray("FilterItems");
        for (int i = 0; i < count; ++i)
        {
          var entry = (FilterItem)(info.GetValue("FilterItem", s) ?? throw new InvalidOperationException());
          s.Add(entry);
        }
        info.CloseArray(count);
        return s;
      }
    }

    #endregion

    public void CopyFrom(FilterItemCollection from)
    {
      this.Clear();
      foreach (var it in from)
      {
        this.Add(it);
      }
    }

    public object Clone()
    {
      var result = new FilterItemCollection();
      result.CopyFrom(this);
      return result;
    }
  }
}
