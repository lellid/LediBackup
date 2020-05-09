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
  /// Defines the encoding used to store Arrays of primitive types
  /// </summary>
  public enum XmlArrayEncoding
  {
    /// <summary>
    /// Use a xml element for every array element.
    /// </summary>
    Xml,

    /// <summary>
    /// Store the array data in binary form using Base64 encoding.
    /// </summary>
    Base64,

    /// <summary>
    /// Store th array data in binary form using BinHex encoding.
    /// </summary>
    BinHex
  }
}
