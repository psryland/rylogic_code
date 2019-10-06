using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.Validators
{
	public class TextValidator : ValidationRule
	{
		// Attached property for dependency objects that have a TextProperty
		//
		// Example Use:
		//  <TextBox
		//      Text="{Binding Price}"
		//      val:TextValidator.Pred="{Binding ValidatePrice}"
		//      />
		// ViewModel:
		//   public string Price
		//   {
		//       get => m_price.ToString();
		//       set => m_price = decimal.Parse(value);
		//   }
		//   private decimal m_price;
		//   public Func<string, ValidationResult> ValidatePrice
		//   {
		//       get => x =>
		//       {
		//           if (!decimal.TryParse(s, out var v) || v <= 0) return new ValidationResult(false, "Price must be positive number");
		//           return ValidationResult.ValidResult;
		//       };
		//   }

		// A validator for dependency objects that have a TextProperty dependency property
		static TextValidator()
		{
			PredProperty = Gui_.DPRegisterAttached<TextValidator>(nameof(Pred), flags: FrameworkPropertyMetadataOptions.None);
		}
		public TextValidator(Func<string, ValidationResult> pred)
		{
			Pred = pred;
		}

		/// <summary>The predicate for validating text</summary>
		public Func<string, ValidationResult> Pred { get; }
		public static readonly DependencyProperty PredProperty;
		public static Func<string, ValidationResult> GetPred(DependencyObject obj)
		{
			return (Func<string, ValidationResult>)obj.GetValue(PredProperty);
		}
		public static void SetPred(DependencyObject obj, Func<string, ValidationResult> value)
		{
			obj.SetValue(PredProperty, value);
		}
		private static void Pred_Changed(DependencyObject obj, Func<string, ValidationResult> new_value, Func<string, ValidationResult> old_value)
		{
			// Reflect on 'obj' for the TextProperty dependency property
			var fi = obj.GetType().GetField(nameof(TextBox.TextProperty), BindingFlags.Static | BindingFlags.Public);
			if (fi == null)
				throw new Exception($"Type {obj.GetType().Name} does not have a static dependency property '{nameof(TextBox.TextProperty)}'");

			// Get the binding to the TextProperty
			var dep = (DependencyProperty)fi.GetValue(null);
			var binding = BindingOperations.GetBinding(obj, dep);
			
			// Add/Replace/Remove the predicate validator
			if (old_value != null)
				binding.ValidationRules.RemoveIf(x => x is TextValidator tv && tv.Pred == old_value);
			if (new_value != null)
				binding.ValidationRules.Add(new TextValidator(new_value));
		}

		/// <summary></summary>
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			return value is string str ? Pred(str) : new ValidationResult(false, $"Value '{value}' is not of type string");
		}
	}




	public class PredicateValidator<T> : ValidationRule
	{
		public PredicateValidator(Func<T, ValidationResult> pred)
		{
			Pred = pred;
		}

		/// <summary>The validation predicate</summary>
		public Func<T, ValidationResult> Pred { get; }

		/// <summary></summary>
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			return value is T ty ? Pred(ty) : new ValidationResult(false, $"Value '{value}' is not of the expected type ({typeof(T).Name})");
		}
	}
	public class PredicateValidator
	{
		/// <summary>Create an attached property called 'Validate' that can be bound to a property of type Func(T,ValidationResult)</summary>
		public static readonly DependencyProperty PredProperty = Gui_.DPRegisterAttached<PredicateValidator>("Pred", flags:FrameworkPropertyMetadataOptions.None);
		public static Func<object, ValidationResult> GetPred(DependencyObject obj)
		{
			return (Func<object, ValidationResult>)obj.GetValue(PredProperty);
		}
		public static void SetPred(DependencyObject obj, Func<object, ValidationResult> value)
		{
			obj.SetValue(PredProperty, value);
		}
		private static void Pred_Changed(DependencyObject obj, Func<object, ValidationResult> new_value, Func<object, ValidationResult> old_value)
		{
			// Update the set of validation rules
			Binding binding;
			switch (obj)
			{
			default:
				{
					binding = BindingOperations.GetBinding(obj, PredProperty);
					break;
				}
			case TextBox tb:
				{
					binding = BindingOperations.GetBinding(tb, TextBox.TextProperty);
					break;
				}
			}

			// Add/Replace/Remove the predicate validator
			if (old_value != null)
				binding.ValidationRules.RemoveIf(x => x is PredicateValidator<object> pred && pred.Pred == old_value);
			if (new_value != null)
				binding.ValidationRules.Add(new PredicateValidator<object>(new_value));
		}
	}
}
