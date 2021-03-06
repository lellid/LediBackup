﻿/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics.CodeAnalysis;

namespace LediBackup.Gui
{
  /// <summary>
  /// Implements an <see cref="System.Windows.Input.ICommand"/> by storing and executing actions for <see cref="System.Windows.Input.ICommand.Execute(object)"/>
  /// and <see cref="System.Windows.Input.ICommand.CanExecute(object)"/>. Note that you have to manually call <see cref="OnCanExecuteChanged"/> if <see cref="CanExecute(object)"/> will return a different value than before.
  /// </summary>
  /// <seealso cref="System.Windows.Input.ICommand" />
  public class RelayCommand : System.Windows.Input.ICommand
  {
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    #region Constructors

    public RelayCommand(Action execute)
     : this(execute, null)
    {
    }

    public RelayCommand(Action execute, Func<bool>? canExecute)
    {
      _execute = execute ?? throw new ArgumentNullException(nameof(execute));
      _canExecute = canExecute;
    }

    #endregion Constructors

    #region ICommand

    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// If called, will raise the <see cref="CanExecuteChanged"/> event. Call this function manually if <see cref="CanExecute(object)"/> will return a different value than before.
    /// </summary>
    public void OnCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Used to connect to the Wpf CommandManager's RequerySuggested event that is fired if something in the Gui has changed.
    /// </summary>
    /// <param name="sender">The sender (unused).</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data (unused).</param>
    public void EhRequerySuggested(object sender, EventArgs e)
    {
      OnCanExecuteChanged();
    }

    /// <summary>
    /// Defines the method that determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
    /// <returns>
    /// true if this command can be executed; otherwise, false.
    /// </returns>
    public bool CanExecute(object parameter)
    {
      return _canExecute?.Invoke() ?? true;
    }

    /// <summary>
    /// Defines the method to be called when the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
    public void Execute(object parameter)
    {
      _execute();
    }

    #endregion ICommand
  }

  /// <summary>
  /// Implements an <see cref="System.Windows.Input.ICommand"/> by storing and executing actions for <see cref="System.Windows.Input.ICommand.Execute(object)"/>
  /// and <see cref="System.Windows.Input.ICommand.CanExecute(object)"/>. Note that you have to manually call <see cref="OnCanExecuteChanged"/> if <see cref="CanExecute(object)"/> will return a different value than before.
  /// </summary>
  /// <seealso cref="System.Windows.Input.ICommand" />
  public class RelayCommand<T> : System.Windows.Input.ICommand
  {
    private readonly Action<T> _execute;
    private readonly Predicate<T>? _canExecute;

    #region Constructors

    public RelayCommand(Action<T> execute)
      : this(execute, null)
    {
    }

    public RelayCommand(Action<T> execute, Predicate<T>? canExecute)
    {
      _execute = execute ?? throw new ArgumentNullException(nameof(execute));
      _canExecute = canExecute;
    }

    #endregion Constructors

    #region ICommand

    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// If called, will raise the <see cref="CanExecuteChanged"/> event. Call this function manually if <see cref="CanExecute(object)"/> will return a different value than before.
    /// </summary>
    public void OnCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Used to connect to the Wpf CommandManager's RequerySuggested event that is fired if something in the Gui has changed.
    /// </summary>
    /// <param name="sender">The sender (unused).</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data (unused).</param>
    public void EhRequerySuggested(object sender, EventArgs e)
    {
      OnCanExecuteChanged();
    }

    /// <summary>
    /// Defines the method that determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
    /// <returns>
    /// true if this command can be executed; otherwise, false.
    /// </returns>
    public bool CanExecute(object parameter)
    {
      if (null != parameter && !(parameter is T))
        throw new ArgumentException("Argument is expected to be of type " + typeof(T).ToString(), nameof(parameter));

#nullable disable
      return _canExecute?.Invoke((T)parameter) ?? true;
#nullable restore
    }

    /// <summary>
    /// Defines the method to be called when the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
    public void Execute(object parameter)
    {
      if (null != parameter && !(parameter is T))
        throw new ArgumentException("Argument is expected to be of type " + typeof(T).ToString(), nameof(parameter));

#nullable disable
      _execute((T)parameter);
#nullable restore
    }

    #endregion ICommand
  }
}
