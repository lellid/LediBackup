/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LediBackup.Dom;
using LediBackup.Dom.Worker;
using Microsoft.Win32;

namespace LediBackup.Gui
{
  public interface IBackupDocumentView
  {
    object DataContext { set; }
  }

  public class BackupDocumentController : INotifyPropertyChanged
  {
    private BackupDocument _doc;

    private IBackupDocumentView _view;

    public event PropertyChangedEventHandler? PropertyChanged;

    private IBackupWorker? _backupWorker;

    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


    public BackupDocumentController(BackupDocument doc, IBackupDocumentView view)
    {
      Current.PropertyChanged += EhCurrent_PropertyChanged;

      _doc = doc ?? throw new ArgumentNullException(nameof(doc));
      _view = view ?? throw new ArgumentNullException(nameof(view));

      // Bindings
      CmdStartBackup = new RelayCommand(EhStartBackup);
      CmdCancelBackup = new RelayCommand(EhCancelBackup);
      CmdReorganizeOldBackup = new RelayCommand(EhReorganizeOldBackup);
      CmdPruneCentralContentStorageDirectory = new RelayCommand(EhPruneCentralContentStorageDirectory);
      CmdShowHelpAbout = new RelayCommand(EhShowHelpAbout);
      CmdShowHelpManual = new RelayCommand(EhShowHelpManual);
      CmdChooseBackupBaseDirectory = new RelayCommand(EhChooseBackupDirectory);
      CmdNewDirectoryEntry = new RelayCommand(EhNewDirectoryEntry);
      CmdEditDirectoryEntry = new RelayCommand(EhEditDirectoryEntry);
      CmdMoveDirectoryEntryUp = new RelayCommand(EhMoveDirectoryEntryUp);
      CmdMoveDirectoryEntryDown = new RelayCommand(EhMoveDirectoryEntryDown);
      CmdDeleteDirectoryEntry = new RelayCommand(EhDeleteDirectoryEntry);
      CmdFileOpen = new RelayCommand(EhFileOpen);
      CmdFileSave = new RelayCommand(EhFileSave);
      CmdFileSaveAs = new RelayCommand(EhFileSaveAs);

      _view.DataContext = this;
    }

    private void EhCurrent_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(Current.Project))
      {
        _doc = Current.Project;
        OnPropertyChanged(nameof(BackupBaseDirectory));
        OnPropertyChanged(nameof(BackupModeIsFast));
        OnPropertyChanged(nameof(BackupModeIsSecure));
        OnPropertyChanged(nameof(BackupTodaysDirectoryPreText));
        OnPropertyChanged(nameof(BackupTodaysDirectoryMiddleText));
        OnPropertyChanged(nameof(BackupTodaysDirectoryPostText));
        OnPropertyChanged(nameof(BackupDirectories));
        OnPropertyChanged(nameof(ErrorMessages));
        OnPropertyChanged(nameof(NameOfProcessedFile));
        OnPropertyChanged(nameof(NumberOfProcessedFiles));
      }
    }

    #region Bindings

    public string BackupBaseDirectory
    {
      get => _doc.BackupMainFolder;
      set
      {
        if (!(_doc.BackupMainFolder == value))
        {
          _doc.BackupMainFolder = value;
          OnPropertyChanged(nameof(BackupBaseDirectory));
        }
      }
    }

    public string BackupTodaysDirectoryPreText
    {
      get => _doc.BackupTodaysDirectoryPreText;
      set
      {
        if (!(_doc.BackupTodaysDirectoryPreText == value))
        {
          _doc.BackupTodaysDirectoryPreText = value;
          OnPropertyChanged(nameof(BackupTodaysDirectoryPreText));
        }
      }
    }

    public string BackupTodaysDirectoryPostText
    {
      get => _doc.BackupTodaysDirectoryPostText;
      set
      {
        if (!(_doc.BackupTodaysDirectoryPostText == value))
        {
          _doc.BackupTodaysDirectoryPostText = value;
          OnPropertyChanged(nameof(BackupTodaysDirectoryPostText));
        }
      }
    }

    public BackupTodaysDirectoryMiddleTextType BackupTodaysDirectoryMiddleText
    {
      get => _doc.BackupTodaysDirectoryMiddleText;
      set
      {
        if (!(_doc.BackupTodaysDirectoryMiddleText == value))
        {
          _doc.BackupTodaysDirectoryMiddleText = value;
          OnPropertyChanged(nameof(BackupTodaysDirectoryMiddleText));
        }
      }
    }

    public object BackupTodaysDirectoryMiddleTextCollection { get; } = Enum.GetValues(typeof(BackupTodaysDirectoryMiddleTextType));

    public ObservableCollection<DirectoryEntry> BackupDirectories => _doc.Directories;

    private DirectoryEntry? _selectedDirectory;
    public DirectoryEntry? SelectedDirectory
    {
      get => _selectedDirectory;
      set
      {
        if (!object.ReferenceEquals(_selectedDirectory, value))
        {
          _selectedDirectory = value;
          OnPropertyChanged(nameof(SelectedDirectory));
        }
      }
    }

    public bool BackupModeIsFast
    {
      get => _doc.BackupMode == Dom.Worker.Backup.BackupMode.Fast;
      set
      {
        if (value && _doc.BackupMode != Dom.Worker.Backup.BackupMode.Fast)
        {
          _doc.BackupMode = Dom.Worker.Backup.BackupMode.Fast;
          OnPropertyChanged(nameof(BackupModeIsFast));
          OnPropertyChanged(nameof(BackupModeIsSecure));
        }
      }
    }
    public bool BackupModeIsSecure
    {
      get => _doc.BackupMode == Dom.Worker.Backup.BackupMode.Secure;
      set
      {
        if (value && _doc.BackupMode != Dom.Worker.Backup.BackupMode.Secure)
        {
          _doc.BackupMode = Dom.Worker.Backup.BackupMode.Secure;
          OnPropertyChanged(nameof(BackupModeIsSecure));
          OnPropertyChanged(nameof(BackupModeIsFast));
        }
      }
    }


    public ICommand CmdStartBackup { get; }
    public ICommand CmdCancelBackup { get; }
    public ICommand CmdReorganizeOldBackup { get; }

    public ICommand CmdPruneCentralContentStorageDirectory { get; }

    public ICommand CmdShowHelpAbout { get; }

    public ICommand CmdShowHelpManual { get; }
    public ICommand CmdChooseBackupBaseDirectory { get; }

    public ICommand CmdNewDirectoryEntry { get; }
    public ICommand CmdEditDirectoryEntry { get; }

    public ICommand CmdMoveDirectoryEntryUp { get; }
    public ICommand CmdMoveDirectoryEntryDown { get; }
    public ICommand CmdDeleteDirectoryEntry { get; }

    public ICommand CmdFileOpen { get; }
    public ICommand CmdFileSave { get; }
    public ICommand CmdFileSaveAs { get; }

    #endregion

    private bool _isBackupActive;
    public bool IsBackupActive
    {
      get => _isBackupActive;
      set
      {
        if (!(_isBackupActive == value))
        {
          _isBackupActive = value;
          OnPropertyChanged(nameof(IsBackupActive));
        }
      }
    }

    private async void EhStartBackup()
    {
      try
      {
        var worker = new Dom.Worker.Backup.BackupWorker(_doc);
        await StartBackup(worker);

        MessageBox.Show(
         $"Backup completed in {worker.Duration.TotalSeconds} s!\r\n\r\n" +
         $"{NumberOfProcessedFiles} items processed,\r\n" +
         $"{worker.ErrorMessages.Count} errors.",
         "Backup completed!",
         MessageBoxButton.OK,
         MessageBoxImage.Information
         );
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Got an exception!\r\n\r\nDetails:\r\n{ex.Message}", "Exception catched!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void EhReorganizeOldBackup()
    {
      if (MessageBoxResult.Yes != MessageBox.Show(
        $"This will reorganize your backups stored in {_doc.BackupMainFolder}\r\n" +
        "This folder should be the main folder that contains all backups on this drive.\r\n" +
        "\r\n" +
        "Do you want to proceed?",
        "Proceed?",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question
        ))
      {
        return;
      }

      try
      {
        var worker = new Dom.Worker.ReworkOldBackup.BackupReworker(_doc.BackupMainFolder);
        await StartBackup(worker);

        MessageBox.Show(
          $"Reorganizing completed in {worker.Duration.TotalSeconds} s!\r\n\r\n" +
          $"{NumberOfProcessedFiles} items processed,\r\n" +
          $"{worker.ErrorMessages.Count} errors.",
          "Reorganizing completed!",
          MessageBoxButton.OK,
          MessageBoxImage.Information
          );
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Got an exception!\r\n\r\nDetails:\r\n{ex.Message}", "Exception catched!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void EhPruneCentralContentStorageDirectory()
    {
      if (MessageBoxResult.Yes != MessageBox.Show(
        "This will walk through your central content storage directory ({Path.Combine(_doc.BackupMainFolder, Current.BackupContentFolderName)}) and delete the files that are no longer in use.\r\n" +
        "Please note that this additionally will require to delete the content of the central name directory ({Path.Combine(_doc.BackupMainFolder, Current.BackupNameFolderName)}).\r\n" +
        "This will slow down the backup speed of the next backup somewhat.\r\n" +
        "\r\n" +
        "Do you want to proceed?",
        "Proceed?",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question
        ))
      {
        return;
      }

      try
      {
        var worker = new Dom.Worker.PruneCentralContentStorage.PruneCentralContentStorageWorker(_doc.BackupMainFolder);
        await StartBackup(worker);

        MessageBox.Show(
          $"Pruning completed in {worker.Duration.TotalSeconds} s!\r\n\r\n" +
          $"{NumberOfProcessedFiles} items processed,\r\n" +
          $"{worker.ErrorMessages.Count} errors.",
          "Reorganizing completed!",
          MessageBoxButton.OK,
          MessageBoxImage.Information
          );
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Got an exception!\r\n\r\nDetails:\r\n{ex.Message}", "Exception catched!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async Task StartBackup(IBackupWorker backupWorker)
    {
      _backupWorker = backupWorker ?? throw new ArgumentNullException(nameof(backupWorker));
      IsBackupActive = true;
      var task = _backupWorker.Backup();

      OnPropertyChanged(nameof(ErrorMessages)); // In order to bind to the right collection

      void UpdateProperties(IBackupWorker worker)
      {
        OnPropertyChanged(nameof(NumberOfItemsInReader));
        OnPropertyChanged(nameof(NumberOfItemsInHasher));
        OnPropertyChanged(nameof(NumberOfItemsInWriter));
        OnPropertyChanged(nameof(NumberOfProcessedFiles));
        OnPropertyChanged(nameof(NumberOfFailedFiles));
        OnPropertyChanged(nameof(NameOfProcessedFile));
        worker.UpdateErrorMessages();
      }

      for (; !task.IsCompleted;)
      {
        UpdateProperties(_backupWorker);
        await Task.Delay(500);
      }
      UpdateProperties(_backupWorker);

      IsBackupActive = false;
    }



    private void EhCancelBackup()
    {
      if (_backupWorker is { } bw)
        bw.CancellationTokenSource.Cancel();
    }

    public int NumberOfItemsInReader => _backupWorker?.NumberOfItemsInReader ?? 0;
    public int NumberOfItemsInHasher => _backupWorker?.NumberOfItemsInHasher ?? 0;
    public int NumberOfItemsInWriter => _backupWorker?.NumberOfItemsInWriter ?? 0;

    public int NumberOfProcessedFiles => _backupWorker?.NumberOfProcessedFiles ?? 0;
    public int NumberOfFailedFiles => _backupWorker?.NumberOfFailedFiles ?? 0;

    public string NameOfProcessedFile => _backupWorker?.NameOfProcessedFile ?? string.Empty;

    public IEnumerable<string> ErrorMessages => (IEnumerable<string>?)_backupWorker?.ErrorMessages ?? (IEnumerable<string>)new string[0];

    private void EhNewDirectoryEntry()
    {
      var controller = new DirectoryEntryController();
      for (; ; )
      {
        var control = new DirectoryEntryControl() { DataContext = controller };
        var dlg = new DialogShellViewWpf(control);
        if (true == dlg.ShowDialog())
        {
          try
          {
            DirectoryEntry entry = controller.GetDocument();
            Current.Project.Directories.Add(entry);
            break;
          }
          catch (Exception ex)
          {
            System.Windows.MessageBox.Show(ex.Message);
          }
        }
        else
        {
          break;
        }
      }
    }

    private void EhEditDirectoryEntry()
    {
      if (SelectedDirectory is { } selDir)
      {
        var idx = _doc.Directories.IndexOf(selDir);
        if (idx < 0) throw new InvalidProgramException();

        var controller = new DirectoryEntryController(selDir);

        for (; ; )
        {
          var control = new DirectoryEntryControl() { DataContext = controller };
          var dlg = new DialogShellViewWpf(control);
          if (true == dlg.ShowDialog())
          {
            try
            {
              var entry = controller.GetDocument();
              _doc.Directories[idx] = entry;
              break;
            }
            catch (Exception ex)
            {
              System.Windows.MessageBox.Show(ex.Message);
            }
          }
          else
          {
            break;
          }
        }
      }
    }

    private void EhMoveDirectoryEntryUp()
    {
      if (SelectedDirectory is { } selDir)
      {
        var idx = _doc.Directories.IndexOf(selDir);
        if (idx > 0)
        {
          _doc.Directories.Move(idx, idx - 1);
        }
      }
    }

    private void EhMoveDirectoryEntryDown()
    {
      if (SelectedDirectory is { } selDir)
      {
        var idx = _doc.Directories.IndexOf(selDir);
        if (idx >= 0 && idx < _doc.Directories.Count - 1)
        {
          _doc.Directories.Move(idx, idx + 1);
        }
      }
    }

    private void EhDeleteDirectoryEntry()
    {
      if (SelectedDirectory is { } selDir)
      {
        var idx = _doc.Directories.IndexOf(selDir);
        if (idx >= 0)
        {
          _doc.Directories.Remove(selDir);
          SelectedDirectory = null;
        }
      }
    }

    private void EhShowHelpAbout()
    {
      var controller = new HelpAboutController();
      var control = new HelpAboutControl { DataContext = controller };
      var dlg = new DialogShellViewWpf(control)
      {
        CancelVisible = false,
        ApplyVisible = false
      };

      dlg.ShowDialog();
    }

    private void EhShowHelpManual()
    {

      var control = new HelpManualControl();
      var dlg = new Window
      {
        Content = control
      };


      dlg.Show();
    }

    private void EhChooseBackupDirectory()
    {
      var dlg = new System.Windows.Forms.FolderBrowserDialog();
      if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        BackupBaseDirectory = dlg.SelectedPath;
      }
    }


    private void EhFileOpen()
    {
      var dlg = new OpenFileDialog
      {
        Filter = "LediBackup files (*.ledibackup)|*.ledibackup"
      };

      if (true == dlg.ShowDialog())
      {
        var fileName = dlg.FileName;
        var s = new LediBackup.Serialization.Xml.XmlStreamDeserializationInfo();

        using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
          s.BeginReading(stream);
          var project = (BackupDocument)(s.GetValue("Project", null) ?? throw new InvalidDataException("Deserializing the project gets not a value"));
          Current.Project = project;
        }

        Current.FileName = fileName;
      }
    }

    private void EhFileSave()
    {
      if (!string.IsNullOrEmpty(Current.FileName))
      {
        Save(Current.FileName);
      }
      else
      {
        EhFileSaveAs();
      }
    }

    private void EhFileSaveAs()
    {
      var dlg = new SaveFileDialog
      {
        DefaultExt = ".ledibackup",
        Filter = "LediBackup files (*.ledibackup)|*.ledibackup|All files (*.*)|*.*"
      };
      if (true == dlg.ShowDialog())
      {
        Save(dlg.FileName);
      }
    }

    private void Save(string fileName)
    {
      var s = new LediBackup.Serialization.Xml.XmlStreamSerializationInfo();

      using (var stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
      {
        s.BeginWriting(stream);
        s.AddValue("Project", Current.Project);
        s.EndWriting();
      }

      Current.FileName = fileName;
    }
  }
}
