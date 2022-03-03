using System.Globalization;
using System.Windows.Controls;
using Rylogic.Common;

namespace Rylogic.Gui.WPF.Validators
{
	/// <summary>Valid if the given path exists</summary>
	public class PathExists : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (!(value is string path))
				return new ValidationResult(false, $"Value '{value}' is not a string");

			if (!Path_.PathExists(path))
				return new ValidationResult(false, $"Path '{path}' does not exist");

			return ValidationResult.ValidResult;
		}
	}

	/// <summary>Valid if the given path is a valid path name</summary>
	public class PathValid : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (!(value is string path))
				return new ValidationResult(false, $"Value '{value}' is not a string");

			if (!Path_.IsValidFilepath(path, false))
				return new ValidationResult(false, $"Path '{path}' is not a valid path");

			return ValidationResult.ValidResult;
		}
	}

	/// <summary>Valid if the given path is a valid full path name</summary>
	public class FullPathValid : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (!(value is string path))
				return new ValidationResult(false, $"Value '{value}' is not a string");

			if (!Path_.IsValidFilepath(path, true))
				return new ValidationResult(false, $"Path '{path}' is not a valid or is a relative path");

			return ValidationResult.ValidResult;
		}
	}
}
