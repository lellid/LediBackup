/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LediBackup.Serialization.Xml
{
  /// <summary>
  /// Contains serialization surrogate classes for serialization of basic types from the System namespace. These serialization surrogates are only needed when the type to serialize is unknown, for instance the entries in an untyped set.
  /// Otherwise, it is preferable to use the special functions of <see cref="IXmlSerializationInfo"/> to serialize and of <see cref="IXmlDeserializationInfo"/> to deserialize those types.
  /// </summary>
  internal static class SerializationOfBasicTypes
  {
    /// <summary>
    /// 2015-06-30 Initial version
    /// </summary>
    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(string), 0)]
    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor("System.Private.CoreLib", "System.String", 0)] // Deserialization for .Net core type
    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor("mscorlib", "System.String", 0)] // Deserialization for .Net framework type
    private class XmlSerializationSurrogateForSystemString : IXmlSerializationSurrogate
    {
      public void Serialize(object obj, LediBackup.Serialization.Xml.IXmlSerializationInfo info)
      {
        var s = (string)obj;
        info.AddValue("e", s);
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        return info.GetString("e");
      }
    }

    /// <summary>
    /// 2015-06-30 Initial version
    /// </summary>
    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(double), 0)]
    private class XmlSerializationSurrogateForSystemDouble : IXmlSerializationSurrogate
    {
      public void Serialize(object obj, LediBackup.Serialization.Xml.IXmlSerializationInfo info)
      {
        var s = (double)obj;
        info.AddValue("e", s);
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        return info.GetDouble("e");
      }
    }

    /// <summary>
    /// 2015-06-30 Initial version
    /// </summary>
    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(int), 0)]
    private class XmlSerializationSurrogateForSystemInt32 : IXmlSerializationSurrogate
    {
      public void Serialize(object obj, LediBackup.Serialization.Xml.IXmlSerializationInfo info)
      {
        var s = (int)obj;
        info.AddValue("e", s);
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        return info.GetInt32("e");
      }
    }

    /// <summary>
    /// 2015-06-30 Initial version
    /// </summary>
    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(long), 0)]
    private class XmlSerializationSurrogateForSystemInt64 : IXmlSerializationSurrogate
    {
      public void Serialize(object obj, LediBackup.Serialization.Xml.IXmlSerializationInfo info)
      {
        var s = (long)obj;
        info.AddValue("e", s);
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        return info.GetInt64("e");
      }
    }

    /// <summary>
    /// 2015-06-30 Initial version
    /// </summary>
    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(DateTime), 0)]
    private class XmlSerializationSurrogateForSystemDateTime : IXmlSerializationSurrogate
    {
      public void Serialize(object obj, LediBackup.Serialization.Xml.IXmlSerializationInfo info)
      {
        var s = (DateTime)obj;
        info.AddValue("e", s);
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        return info.GetDateTime("e");
      }
    }

    /// <summary>
    /// 2018-04-11 Initial version
    /// </summary>
    [LediBackup.Serialization.Xml.XmlSerializationSurrogateFor(typeof(bool), 0)]
    private class XmlSerializationSurrogateForBoolean : IXmlSerializationSurrogate
    {
      public void Serialize(object obj, LediBackup.Serialization.Xml.IXmlSerializationInfo info)
      {
        var s = (bool)obj;
        info.AddValue("e", s);
      }

      public object Deserialize(object? o, IXmlDeserializationInfo info, object? parentobject)
      {
        return info.GetBoolean("e");
      }
    }
  }
}
