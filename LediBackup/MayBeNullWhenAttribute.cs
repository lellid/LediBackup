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
using System.Threading.Tasks;

namespace LediBackup
{
#if NETFRAMEWORK
    /// <summary>
    /// Fake MayBeNullWhen Attribute that avoids compile errors when using the .NET framework.
    /// </summary>
    public class MaybeNullWhenAttribute : Attribute
    {
        public MaybeNullWhenAttribute(bool _)
        {

        }
    }
#endif
}
