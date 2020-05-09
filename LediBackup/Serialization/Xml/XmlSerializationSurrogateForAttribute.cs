/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;

namespace LediBackup.Serialization.Xml
{
  /// <summary>
  /// Used to point to the target type for which this class provides a serialization surrogate.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
  public class XmlSerializationSurrogateForAttribute : Attribute
  {
    protected int _version;
    protected System.Type? _serializationType;
    protected string? _assemblyName;
    protected string? _typeName;

    /// <summary>
    /// Constructor. The class this attribute is applied provides a serialization surrogate for the type <code>serializationtype</code>, version <code>version.</code>.
    /// </summary>
    /// <param name="serializationtype">The type this class provides a surrogate for.</param>
    /// <param name="version">The version of the class for which this surrogate is intended.</param>
    public XmlSerializationSurrogateForAttribute(Type serializationtype, int version)
    {
      _version = version;
      _serializationType = serializationtype;
    }

    /// <summary>
    /// Constructor. Used when the target type is deprecated and no longer available. The class this attribute is applied for is then
    /// responsible for deserialization
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="typename"></param>
    /// <param name="version"></param>
    public XmlSerializationSurrogateForAttribute(string assembly, string typename, int version)
    {
      _version = version;
      _assemblyName = assembly;
      _typeName = typename;
    }

    /// <summary>
    /// returns the version of the class, for which the surrogate is intended
    /// </summary>
    public int Version
    {
      get { return _version; }
    }

    /// <summary>
    ///Returns the target type for which the class this attribute is applied for is the serialization surrogate.
    /// </summary>
    public System.Type? SerializationType
    {
      get { return _serializationType; }
    }

    /// <summary>
    /// Returns the assembly name (short form) of the target class type.
    /// </summary>
    public string AssemblyName
    {
      get
      {
        if (null != _serializationType)
        {
          if (_serializationType.Assembly.FullName is { } fullName)
            return (_serializationType.Assembly.FullName.Split(new char[] { ',' }, 2))[0];
          else
            throw new InvalidOperationException($"No FullName available for assembly {_serializationType.Assembly}");
        }
        else if (_assemblyName is { } _)
        {
          return _assemblyName;
        }
        else
        {
          throw new InvalidOperationException("Either type or AssemblyName should be != null");
        }
      }
    }

    /// <summary>
    /// Returns the name of the target type (the full name inclusive namespaces).
    /// </summary>
    public string TypeName
    {
      get
      {
        return _serializationType?.ToString() ?? _typeName ?? throw new InvalidOperationException();
      }
    }
  } // end class SerializationSurrogateForAttribute
}
