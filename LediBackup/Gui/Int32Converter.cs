/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace LediBackup.Gui
{
  public class NumericInt32Converter : ValidationRule, IValueConverter
  {
    private System.Globalization.CultureInfo _conversionCulture = System.Globalization.CultureInfo.InvariantCulture;
    private string? _lastConvertedString;

    private int? _lastConvertedValue;

    public int MinimumValue { get; set; }

    public int MaximumValue { get; set; }

    public NumericInt32Converter()
    {
      MinimumValue = int.MinValue;
      MaximumValue = int.MaxValue;
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo cultureDontUseIsBuggy)
    {
      var val = (int)value;

      if (null != _lastConvertedString && val == _lastConvertedValue)
      {
        return _lastConvertedString;
      }

      _lastConvertedValue = val;
      _lastConvertedString = val.ToString(_conversionCulture);
      return _lastConvertedString;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo cultureDontUseIsBuggy)
    {
      var validationResult = ConvertAndValidate(value, out var result);
      if (validationResult.IsValid)
      {
        _lastConvertedString = (string)value;
        _lastConvertedValue = result;
      }
      return result;
    }

    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureDontUseIsBuggy)
    {
      var validationResult = ConvertAndValidate(value, out var result);
      if (validationResult.IsValid)
      {
        _lastConvertedString = (string)value;
        _lastConvertedValue = result;
      }
      return validationResult;
    }

    private ValidationResult ConvertAndValidate(object value, out int result)
    {
      var s = (string)value;

      if (null != _lastConvertedValue && s == _lastConvertedString)
      {
        result = _lastConvertedValue.Value;
        return ValidateSuccessfullyConvertedValue(result);
      }

      if (int.TryParse(s, System.Globalization.NumberStyles.Integer, _conversionCulture, out result))
      {
        return ValidateSuccessfullyConvertedValue(result);
      }

      return new ValidationResult(false, "This string could not be converted to a number");
    }

    private ValidationResult ValidateSuccessfullyConvertedValue(int result)
    {
      if (result < MinimumValue)
        return new ValidationResult(false, string.Format("The entered value is less than the minimum allowed value of {0}.", MinimumValue));
      if (result > MaximumValue)
        return new ValidationResult(false, string.Format("The entered value is greater than the maximum allowed value of {0}.", MaximumValue));

      return ValidationResult.ValidResult;
    }
  }
}
