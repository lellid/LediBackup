/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;

namespace LediBackup.Dom.Worker.Backup
{
  /// <summary>
  /// Designates the mode of the backup.
  /// </summary>
  public enum BackupMode
  {
    /// <summary>
    /// Hashes the content only if absolutely neccessary. Hashes the name, and use that information to create a hard link to the name file.
    /// </summary>
    Fast,

    /// <summary>
    /// Hashes always the content, and does not use nor create a name file.
    /// </summary>
    Secure,


  }
}
