/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

namespace LediBackup.Serialization.Xml
{
  /// <summary>
  /// This function is used to call back Deserialization surrogates after finishing deserialization
  /// </summary>
  public delegate void XmlDeserializationCallbackEventHandler(IXmlDeserializationInfo info, object documentRoot, bool isFinallyCall);
}
