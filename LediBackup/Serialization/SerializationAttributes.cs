/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.Serialization;

namespace LediBackup.Serialization
{
  [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module)]
  public class SupportsSerializationVersioningAttribute : Attribute
  {
    public SupportsSerializationVersioningAttribute()
    {
    }
  }
}
