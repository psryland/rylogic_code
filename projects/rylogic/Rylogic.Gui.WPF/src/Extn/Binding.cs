using System;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	public static class Binding_
	{
		/// <summary>Clone a binding</summary>
		public static BindingBase Clone(this BindingBase binding, object? source = null, BindingMode? mode = null)
		{
			m_mi_binding_clone ??= typeof(BindingBase).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception("Clone method not found on Binding");
			m_fi_binding_sealed ??= typeof(BindingBase).GetField("_isSealed", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception("_isSealed field not found on Binding");

			mode ??= (binding as Binding)?.Mode ?? BindingMode.Default;
			var bb = (BindingBase?)m_mi_binding_clone.Invoke(binding, new object[] { mode }) ?? throw new Exception("Binding.Clone() failed");
			m_fi_binding_sealed.SetValue(bb, false);
			if (bb is Binding b) b.Source = source;
			return bb;
		}
		private static MethodInfo? m_mi_binding_clone;
		private static FieldInfo? m_fi_binding_sealed;
	
		/// <summary>Evaluate a binding using the given target</summary>
		public static object Eval(this Binding binding, object? target)
		{
			var e = new Evaluator();
			e.SetBinding(Evaluator.ValueProperty, binding.Clone(source: target, mode: BindingMode.OneTime));
			return e.GetValue(Evaluator.ValueProperty);
		}
		private class Evaluator :FrameworkElement
		{
			public object Value
			{
				get => GetValue(ValueProperty);
				set => SetValue(ValueProperty, value);
			}
			public static readonly DependencyProperty ValueProperty = Gui_.DPRegister<Evaluator>(nameof(Value), null, Gui_.EDPFlags.None);
		}
	}
}
