﻿/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;

namespace LediBackup.Serialization.Xml
{
  /// <summary>
  /// Responsible for storage and retrieving of the xml surrogate classes.
  /// </summary>
  public class XmlSurrogateSelector
  {
    /// <summary>
    /// Use to store the surrogates for a given class.
    /// </summary>
    /// <remarks>There are two kind of keys here: 1) System.Type objects and 2) strings containing the fully qualified name of a type.
    /// The key strings are used to retrieve the serialization surrogate onto deserialization, and if the class to deserialize no longer exists in the assembly.
    /// The values are instances (!) of type IXmlSerializationSurrogate.</remarks>
    private System.Collections.Hashtable _surrogates = new System.Collections.Hashtable();

    /// <summary>
    /// Used to store the actual serialization versions of the classes.
    /// </summary>
    /// <remarks>The keys for the hashtable are System.Type objects, the values are integers storing the serialization version.</remarks>
    private System.Collections.Hashtable _versions = new System.Collections.Hashtable();

    /// <summary>
    /// Constructs an empty surrogate selector.
    /// </summary>
    public XmlSurrogateSelector()
    {
    }

    /// <summary>
    /// Get the fully qualified name of a type. This includes the short assembly name; the full type name, and the version, separated by a comma.
    /// </summary>
    /// <param name="type">The type for which the name should be returned.</param>
    /// <returns>The fully qualified name of the type.</returns>
    public string GetFullyQualifiedTypeName(System.Type type)
    {
      object? version = _versions[type];
      return GetFullyQualifiedTypeName(type, (version is null ? 0 : (int)version));
    }

    /// <summary>
    /// Get the serialization version  of a type.
    /// </summary>
    /// <param name="type">The type for which the version should be returned.</param>
    /// <returns>The serialization version of the type.</returns>
    public int GetVersion(System.Type type)
    {
      object? version = _versions[type];
      return version is null ? 0 : (int)version;
    }

    /// <summary>
    /// Get the fully qualified name of a type. This includes the short assembly name; the full type name, and the version, separated by a comma.
    /// </summary>
    /// <param name="type">The type for which the name should be returned.</param>
    /// <param name="version">The version of this type.</param>
    /// <returns>The fully qualified name of the type.</returns>
    public static string GetFullyQualifiedTypeName(System.Type type, int version)
    {
      var fullName = type.Assembly.FullName ?? throw new InvalidOperationException($"FullName not available for assembly {type.Assembly}");
      string[] assembly = fullName.Split(new char[] { ',' }, 2);
      return string.Format("{0},{1},{2}", assembly[0], type.ToString(), version.ToString());
    }

    /// <summary>
    /// Adds a surrogate for the type <code>type</code>.
    /// </summary>
    /// <param name="type">The type for which the surrogate is added.</param>
    /// <param name="version">The version of the surrogate (higher version numbers mean more recent versions).</param>
    /// <param name="surrogate">The surrogate used to serialize/deserialize the type.</param>
    public void AddSurrogate(System.Type type, int version, IXmlSerializationSurrogate surrogate)
    {
      // if this attribute cares about a currently existing type,
      // consider the highest value of version among all attributes
      // which care for the same type as the current version of that type
      AddTypeAndVersionIfHigher(type, version, surrogate);

      _surrogates[GetFullyQualifiedTypeName(type, version)] = surrogate;
    }

    /// <summary>
    /// Adds a surrogate for the type specified by assembly name, full type name, and version.
    /// </summary>
    /// <param name="assemblyname">The short name of the assembly.</param>
    /// <param name="typename">The fully qualified type name.</param>
    /// <param name="version">The version.</param>
    /// <param name="surrogate">The surrogate which is responsible to deserialize the type.</param>
    public void AddSurrogate(string assemblyname, string typename, int version, IXmlSerializationSurrogate surrogate)
    {
      _surrogates[assemblyname + "," + typename + "," + version] = surrogate;
    }

    /// <summary>
    /// Adds a surrogate for the type specified in the XmlSerializationForAttribute.
    /// </summary>
    /// <param name="attr">The attribute used to describe the type this surrogate is intended for.</param>
    /// <param name="surrogate">The surrogate used to serialize/deserialize the type.</param>
    public void AddSurrogate(XmlSerializationSurrogateForAttribute attr, IXmlSerializationSurrogate surrogate)
    {
      if (null != attr.SerializationType)
        AddSurrogate(attr.SerializationType, attr.Version, surrogate);
      else
        AddSurrogate(attr.AssemblyName, attr.TypeName, attr.Version, surrogate);
    }

    protected void AddTypeAndVersionIfHigher(System.Type type, int version, IXmlSerializationSurrogate surrogate)
    {
      int storedversion = _versions.ContainsKey(type) ? (int)(_versions[type] ?? 0) : int.MinValue;

      if (version > storedversion)
      {
        _versions[type] = version;
        _surrogates[type] = surrogate;
      }
    }

    /// <summary>
    /// Get a serialization surrogate for the specified type.
    /// </summary>
    /// <param name="type">The full qualified type name (<see cref="GetFullyQualifiedTypeName(System.Type)" />) for which a serialization surrogate should be found.</param>
    /// <returns>The serialization surrogate for the specified type, or null if no surrogate is found.</returns>
    public IXmlSerializationSurrogate? GetSurrogate(string type)
    {
      return (IXmlSerializationSurrogate?)_surrogates[type];
    }

    /// <summary>
    /// Get a serialization surrogate for the spezified type.
    /// </summary>
    /// <param name="type">The type for which a serialization surrogate should be found.</param>
    /// <returns>The serialization surrogate for the specified type, or null if no surrogate is found.</returns>
    public IXmlSerializationSurrogate? GetSurrogate(System.Type type)
    {
      return (IXmlSerializationSurrogate?)_surrogates[type];
    }

    /// <summary>
    /// Scans all momentarily loaded assemblies for xml serialization surrogates.
    /// Only assemblies that are marked with the SupportsSerializationVersioningAttribute are scanned.
    /// </summary>
    public void TraceLoadedAssembliesForSurrogates()
    {
      System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
      foreach (Assembly assembly in assemblies)
      {
        // test if the assembly supports Serialization
        var suppVersioning = Attribute.GetCustomAttribute(assembly, typeof(SupportsSerializationVersioningAttribute));
        if (null == suppVersioning)
          continue; // this assembly don't support this, so skip it

        Type[] definedtypes = assembly.GetTypes();
        foreach (Type definedtype in definedtypes)
        {
          Attribute[] surrogateattributes = Attribute.GetCustomAttributes(definedtype, typeof(XmlSerializationSurrogateForAttribute));

          foreach (XmlSerializationSurrogateForAttribute att in surrogateattributes)
          {
            var obj = Activator.CreateInstance(definedtype);
            if (!(obj is IXmlSerializationSurrogate))
              throw new InvalidProgramException(string.Format("Classes that have the XmlSerializationSurrogateForAttribute applied have to implement IXmlSerializationSurrogate. This is not the case for the type " + definedtype.ToString()));
            if (obj is IXmlSerializationSurrogate)
            {
              AddSurrogate(att, (IXmlSerializationSurrogate)obj);
            }
          }
        } // end foreach type
      } // end foreach assembly
    }
  }
}
