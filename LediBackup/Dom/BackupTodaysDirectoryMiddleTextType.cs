/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

namespace LediBackup.Dom
{
  /// <summary>
  /// Designates the middle part of the today's backup subdirectory name (in the main backup folder on the backup drive).
  /// </summary>
  public enum BackupTodaysDirectoryMiddleTextType
  {
    /// <summary>The subdirectory has the form PreTextYYYY-MM-dd HH-mm-ssPostText</summary>
    YearMonthDay_HourMinuteSecond,

    /// <summary>The subdirectory has the form PreTextYYYY-MM-ddPostText</summary>
    YearMonthDay,

    /// <summary>The subdirectory has the form PreTextPostText without a middle part</summary>
    None
  }
}
