/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////



using System;
using System.Windows.Data;

namespace LediBackup.Gui
{
  /// <summary>
  /// Converts a boolean value to its negate value.
  /// </summary>
  [ValueConversion(typeof(bool), typeof(bool))]
  public class BooleanNegateConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return !((bool)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return !((bool)value);
    }
  }
}
