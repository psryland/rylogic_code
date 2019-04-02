using System;
using System.Globalization;
using System.Windows.Controls;

namespace Rylogic.Gui.WPF.Validators
{
	public class PredicateValidator<T> : ValidationRule
	{
		public PredicateValidator(Func<T, bool> pred, string invalid_message_fmt = null)
		{
			Pred = pred;
			InvalidMessageFmt = invalid_message_fmt ?? "'{0}' is not a valid value";
		}

		/// <summary>The validation predicate</summary>
		public Func<T, bool> Pred { get; }

		/// <summary>The string message to use when indicating bad input. Use {0} for current value</summary>
		public string InvalidMessageFmt { get; set; }

		/// <summary></summary>
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (!(value is T t)) return new ValidationResult(false, $"Value '{value}' is not of the expected type ({typeof(T).Name})");
			if (!Pred(t)) return new ValidationResult(false, string.Format(InvalidMessageFmt, value));
			return ValidationResult.ValidResult;
		}
	}
}
