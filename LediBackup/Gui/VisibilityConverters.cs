/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows;
using System.Windows.Data;

namespace LediBackup.Gui
{
  /// <summary>
  /// Converts a boolean value of true to <see cref="Visibility.Collapsed"/> and a value of false to <see cref="Visibility.Visible"/>
  /// </summary>
  [ValueConversion(typeof(bool), typeof(Visibility))]
  public class VisibilityCollapsedForTrueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((bool)value) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((Visibility)value) == Visibility.Collapsed;
    }
  }

  /// <summary>
  /// Converts a boolean value of false to <see cref="Visibility.Collapsed"/> and a value of true to <see cref="Visibility.Visible"/>
  /// </summary>
  [ValueConversion(typeof(bool), typeof(Visibility))]
  public class VisibilityCollapsedForFalseConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((Visibility)value) == Visibility.Visible;
    }
  }

  /// <summary>
  /// Converts a boolean value of false to <see cref="Visibility.Collapsed"/> and a value of true to <see cref="Visibility.Visible"/>
  /// </summary>
  [ValueConversion(typeof(bool), typeof(Visibility))]
  public class VisibilityHiddenForFalseConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((bool)value) ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((Visibility)value) == Visibility.Visible;
    }
  }

  /// <summary>
  /// Converts a boolean value of false to <see cref="Visibility.Collapsed"/> and a value of true to <see cref="Visibility.Visible"/>
  /// </summary>
  [ValueConversion(typeof(bool), typeof(Visibility))]
  public class VisibilityHiddenForTrueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((bool)value) ? Visibility.Hidden : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((Visibility)value) == Visibility.Visible;
    }
  }

  /// <summary>
  /// Converts true to Visibility.Visible and false to Visibility.Hidden.
  /// </summary>
  [ValueConversion(typeof(bool?), typeof(Visibility))]
  public class NullableBoolToVisibilityConverter : IValueConverter
  {
    private bool _visibleWhenFalse;

    public bool VisibleWhenFalse
    {
      get
      {
        return _visibleWhenFalse;
      }
      set
      {
        _visibleWhenFalse = value;
      }
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var val = (bool?)value;
      if (_visibleWhenFalse)
        return false == val ? Visibility.Visible : Visibility.Hidden;
      else
        return true == val ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var val = (Visibility)value;
      return val == Visibility.Visible ? true : false;
    }
  }
}
