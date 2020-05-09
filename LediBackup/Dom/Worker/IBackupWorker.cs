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
using System.Threading;
using System.Threading.Tasks;

namespace LediBackup.Dom.Worker
{
  public interface IBackupWorker
  {
    Task Backup();

    CancellationTokenSource CancellationTokenSource { get; }

    #region Diagnostics

    int NumberOfItemsInReader { get; }
    int NumberOfItemsInHasher { get; }
    int NumberOfItemsInWriter { get; }

    int NumberOfProcessedFiles { get; }
    int NumberOfFailedFiles { get; }

    string NameOfProcessedFile { get; }

    TimeSpan Duration { get; }

    ObservableCollection<string> ErrorMessages { get; }

    /// <summary>
    /// Updates the error messages (brings them from the thread-safe concurrent queue to the Observable collection
    /// that can be used to bound to a ListView). This call should be made only in the context of the Gui thread.
    /// </summary>
    void UpdateErrorMessages();

    #endregion
  }
}
