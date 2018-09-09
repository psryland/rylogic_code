using System.Globalization;
using System.Windows.Controls;
using Rylogic.Extn;

namespace RyLogViewer
{
	public class ValidFilepath : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (!(value is string s) || !Path_.IsValidFilepath(s, true))
				return new ValidationResult(false, "Filepath is not a valid");
			else
				return new ValidationResult(true, null);
		}
	}

	public class PositiveDefinite : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (!(value is string s) || !double.TryParse(s, out var val))
				return new ValidationResult(false, "An numeric value is required");
			else if (val <= 0)
				return new ValidationResult(false, "A value greater than zero required");
			else
				return new ValidationResult(true, null);
		}
	}

	public class PositiveDefiniteInteger : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (!(value is string s) || !int.TryParse(s, out var val))
				return new ValidationResult(false, "An numeric value is required");
			else if (val <= 0)
				return new ValidationResult(false, "A value greater than zero required");
			else
				return new ValidationResult(true, null);
		}
	}
}
