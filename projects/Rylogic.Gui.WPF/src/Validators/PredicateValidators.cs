using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.Validators
{
	/// <summary>Validator based on a predicate</summary>
	public class PredicateValidator :ValidationRule
	{
		// Notes:
		//  - Attached property for dependency objects that have a TextProperty
		// Usage:
		//   <MyControl
		//       Price="{Binding Price}"
		//       gui:PredicateValidator.Pred="{Binding ValidatePrice}"
		//       />
		//   //ViewModel.cs
		//   public decimal Price
		//   {
		//       get => m_price;
		//       set => m_price = value;
		//   }
		//   private decimal m_price;
		//   public Func<object, ValidationResult> ValidatePrice
		//   {
		//       get => x =>
		//       {
		//           if (x is not decimal price) return new ValidationResult(false, $"Value '{x}' is not of the expected type");
		//           if (x <= 0) return new ValidationResult(false, "Price must be positive number");
		//           return ValidationResult.ValidResult;
		//       };
		//   }
		public PredicateValidator(Func<object, ValidationResult> pred) => Pred = pred;
		public Func<object, ValidationResult> Pred { get; }

		/// <inheritdoc/>
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			// value is not T ty => new ValidationResult(false, $"Value '{value}' is not of the expected type ({typeof(T).Name})");
			return Pred(value);
		}

		/// <summary>Create an attached property called 'Validate' that can be bound to a property of type Func(T,ValidationResult)</summary>
		public static readonly DependencyProperty PredProperty = Gui_.DPRegisterAttached<PredicateValidator>("Pred", null, Gui_.EDPFlags.None);
		public static Func<object, ValidationResult> GetPred(DependencyObject obj) => (Func<object, ValidationResult>)obj.GetValue(PredProperty);
		public static void SetPred(DependencyObject obj, Func<object, ValidationResult> value) => obj.SetValue(PredProperty, value);
		private static void Pred_Changed(DependencyObject obj, Func<object, ValidationResult> new_value, Func<object, ValidationResult> old_value)
		{
			// Update the set of validation rules
			Binding binding;
			switch (obj)
			{
				case TextBox tb:
				{
					binding = BindingOperations.GetBinding(tb, TextBox.TextProperty);
					break;
				}
				default:
				{
					binding = BindingOperations.GetBinding(obj, PredProperty);
					break;
				}
			}

			// Add/Replace/Remove the predicate validator
			if (old_value != null)
				binding.ValidationRules.RemoveIf(x => x is PredicateValidator pred && pred.Pred == old_value);
			if (new_value != null)
				binding.ValidationRules.Add(new PredicateValidator(new_value));
		}
	}

	/// <summary>A validator for dependency objects that have a TextProperty dependency property</summary>
	public class TextValidator :ValidationRule
	{
		// Notes:
		//  - Attached property for dependency objects that have a TextProperty
		// Usage:
		//   <TextBox
		//       Text="{Binding Price}"
		//       gui:TextValidator.Pred="{Binding ValidatePrice}"
		//       />
		//   //ViewModel.cs
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
		public TextValidator(Func<string, ValidationResult> pred) => Pred = pred;
		public Func<string, ValidationResult> Pred { get; }

		/// <inheritdoc/>
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			return value is string str ? Pred(str) : new ValidationResult(false, $"Value '{value}' is not of type string");
		}

		/// <summary>The predicate for validating text</summary>
		public static readonly DependencyProperty PredProperty = Gui_.DPRegisterAttached<TextValidator>(nameof(Pred), null, Gui_.EDPFlags.None);
		public static Func<string, ValidationResult> GetPred(DependencyObject obj) => (Func<string, ValidationResult>)obj.GetValue(PredProperty);
		public static void SetPred(DependencyObject obj, Func<string, ValidationResult> value) => obj.SetValue(PredProperty, value);
		private static void Pred_Changed(DependencyObject obj, Func<string, ValidationResult> new_value, Func<string, ValidationResult> old_value)
		{
			// Reflect on 'obj' for the TextProperty dependency property
			var fi = obj.GetType().GetField(nameof(TextBox.TextProperty), BindingFlags.Static | BindingFlags.Public);
			if (fi == null)
				throw new Exception($"Type {obj.GetType().Name} does not have a static dependency property '{nameof(TextBox.TextProperty)}'");

			// Get the binding to the TextProperty
			var dep = (DependencyProperty?)fi.GetValue(null);
			var binding = BindingOperations.GetBinding(obj, dep);

			// Add/Replace/Remove the predicate validator
			if (old_value != null)
				binding.ValidationRules.RemoveIf(x => x is TextValidator tv && tv.Pred == old_value);
			if (new_value != null)
				binding.ValidationRules.Add(new TextValidator(new_value));
		}
	}

}
