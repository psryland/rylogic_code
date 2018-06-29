
// Updated Ultimate WPF Event Method Binding implementation by Mike Marynowski
// View the article here: http://www.singulink.com/CodeIndex/post/updated-ultimate-wpf-event-method-binding
// Licensed under the Code Project Open License: http://www.codeproject.com/info/cpol10.aspx
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Rylogic.Gui2
{
	public class MethodBindingExtension : MarkupExtension
	{
		private readonly object[] _arguments;
		private readonly List<DependencyProperty> _argument_properties = new List<DependencyProperty>();
		private static readonly List<DependencyProperty> StorageProperties = new List<DependencyProperty>();

		public MethodBindingExtension(object method) : this(new[] { method }) { }
		public MethodBindingExtension(object arg0, object arg1) : this(new[] { arg0, arg1 }) { }
		public MethodBindingExtension(object arg0, object arg1, object arg2) : this(new[] { arg0, arg1, arg2 }) { }
		public MethodBindingExtension(object arg0, object arg1, object arg2, object arg3) : this(new[] { arg0, arg1, arg2, arg3 }) { }
		public MethodBindingExtension(object arg0, object arg1, object arg2, object arg3, object arg4) : this(new[] { arg0, arg1, arg2, arg3, arg4 }) { }
		public MethodBindingExtension(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5) : this(new[] { arg0, arg1, arg2, arg3, arg4, arg5 }) { }
		public MethodBindingExtension(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) : this(new[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 }) { }
		public MethodBindingExtension(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7) : this(new[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 }) { }
		public MethodBindingExtension(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8) : this(new[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }) { }
		private MethodBindingExtension(object[] arguments)
		{
			_arguments = arguments;
		}

		/// <summary></summary>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var provide_value_target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

			// Determine the event handler type
			var event_handler_type = (Type)null;
			if (provide_value_target.TargetProperty is EventInfo ei)
			{
				event_handler_type = ei.EventHandlerType;
			}
			else if (provide_value_target.TargetProperty is MethodInfo mi)
			{
				var parameters = mi.GetParameters();
				if (parameters.Length == 2)
					event_handler_type = parameters[1].ParameterType;
			}

			if (!(provide_value_target.TargetObject is FrameworkElement target) || event_handler_type == null)
				return this;

			foreach (object argument in _arguments)
			{
				var argument_property = SetUnusedStorageProperty(target, argument);
				_argument_properties.Add(argument_property);
			}

			return CreateEventHandler(target, event_handler_type);
		}

		/// <summary></summary>
		private Delegate CreateEventHandler(FrameworkElement element, Type event_handler_type)
		{
			EventHandler handler = (sender, eventArgs) =>
			{
				var arg0 = element.GetValue(_argument_properties[0]);
				if (arg0 == null)
				{
					Debug.WriteLine("[MethodBinding] First method binding argument is required and cannot resolve to null - method name or method target expected.");
					return;
				}

				int method_args_start;
				object method_target;

				// If the first argument is a string then it must be the name of the method to invoke on the data context.
				// If not then it is the explicit method target object and the second argument will be name of the method to invoke.
				if (arg0 is string method_name)
				{
					method_target = element.DataContext;
					method_args_start = 1;
				}
				else if (_argument_properties.Count >= 2)
				{
					method_target = arg0;
					method_args_start = 2;

					var arg1 = element.GetValue(_argument_properties[1]);
					if (arg1 == null)
					{
						Debug.WriteLine($"[MethodBinding] First argument resolved as a method target object of type '{method_target.GetType()}', second argument must resolve to a method name and cannot resolve to null.");
						return;
					}
					method_name = arg1 as string;
					if (method_name == null)
					{
						Debug.WriteLine($"[MethodBinding] First argument resolved as a method target object of type '{method_target.GetType()}', second argument (method name) must resolve to a '{typeof(string)}' (actual type: '{arg1.GetType()}').");
						return;
					}
				}
				else
				{
					Debug.WriteLine($"[MethodBinding] Method name must resolve to a '{typeof(string)}' (actual type: '{arg0.GetType()}').");
					return;
				}

				var arguments = new object[_argument_properties.Count - method_args_start];
				for (int i = method_args_start; i < _argument_properties.Count; i++)
				{
					var arg_value = element.GetValue(_argument_properties[i]);
					switch (arg_value)
					{
					case EventSenderExtension _:
						arg_value = sender;
						break;
					case EventArgsExtension eventArgsEx:
						arg_value = eventArgsEx.GetArgumentValue(eventArgs, element.Language);
						break;
					}
					arguments[i - method_args_start] = arg_value;
				}

				var method_target_type = method_target.GetType();

				// Try invoking the method by resolving it based on the arguments provided
				try
				{
					method_target_type.InvokeMember(method_name, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic| BindingFlags.Instance, null, method_target, arguments);
					return;
				}
				catch (MissingMethodException) { }

				// Couldn't match a method with the raw arguments, so check if we can find a method with the same name
				// and parameter count and try to convert any XAML string arguments to match the method parameter types
				var method = method_target_type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault(m => m.Name == method_name && m.GetParameters().Length == arguments.Length);
				if (method != null)
				{
					var parameters = method.GetParameters();
					for (int i = 0; i < _arguments.Length; i++)
					{
						if (arguments[i] == null)
						{
							if (parameters[i].ParameterType.IsValueType)
							{
								method = null;
								break;
							}
						}
						else if (_arguments[i] is string && parameters[i].ParameterType != typeof(string))
						{
							// The original value provided for this argument was a XAML string so try to convert it
							arguments[i] = TypeDescriptor.GetConverter(parameters[i].ParameterType).ConvertFromString((string)_arguments[i]);
						}
						else if (!parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
						{
							method = null;
							break;
						}
					}
					method?.Invoke(method_target, arguments);
				}
				if (method == null)
					Debug.WriteLine($"[MethodBinding] Could not find a method '{method_name}' on target type '{method_target_type}' that accepts the parameters provided.");
			};
			return Delegate.CreateDelegate(event_handler_type, handler.Target, handler.Method);
		}
		private DependencyProperty SetUnusedStorageProperty(DependencyObject obj, object value)
		{
			var property = StorageProperties.FirstOrDefault(p => obj.ReadLocalValue(p) == DependencyProperty.UnsetValue);
			if (property == null)
			{
				property = DependencyProperty.RegisterAttached("Storage" + StorageProperties.Count, typeof(object), typeof(MethodBindingExtension), new PropertyMetadata());
				StorageProperties.Add(property);
			}
			var markupExtension = value as MarkupExtension;
			if (markupExtension != null)
			{
				var resolvedValue = markupExtension.ProvideValue(new ServiceProvider(obj, property));
				obj.SetValue(property, resolvedValue);
			}
			else
			{
				obj.SetValue(property, value);
			}
			return property;
		}

		private class ServiceProvider : IServiceProvider, IProvideValueTarget
		{
			public object TargetObject { get; }
			public object TargetProperty { get; }
			public ServiceProvider(object targetObject, object targetProperty)
			{
				TargetObject = targetObject;
				TargetProperty = targetProperty;
			}
			public object GetService(Type serviceType)
			{
				return serviceType.IsInstanceOfType(this) ? this : null;
			}
		}
	}

	public class EventSenderExtension : MarkupExtension
	{
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	public class EventArgsExtension : MarkupExtension
	{
		public PropertyPath Path { get; set; }
		public IValueConverter Converter { get; set; }
		public object ConverterParameter { get; set; }
		public Type ConverterTargetType { get; set; }
		[TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
		public CultureInfo ConverterCulture { get; set; }
		public EventArgsExtension()
		{
		}
		public EventArgsExtension(string path)
		{
			Path = new PropertyPath(path);
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
		internal object GetArgumentValue(EventArgs eventArgs, XmlLanguage language)
		{
			if (Path == null)
				return eventArgs;
			object value = PropertyPathHelpers.Evaluate(Path, eventArgs);
			if (Converter != null)
				value = Converter.Convert(value, ConverterTargetType ?? typeof(object), ConverterParameter, ConverterCulture ?? language.GetSpecificCulture());
			return value;
		}
	}

	public static class PropertyPathHelpers
	{
		public static object Evaluate(PropertyPath path, object source)
		{
			var target = new DependencyTarget();
			var binding = new Binding() { Path = path, Source = source, Mode = BindingMode.OneTime };
			BindingOperations.SetBinding(target, DependencyTarget.ValueProperty, binding);
			return target.Value;
		}
		private class DependencyTarget : DependencyObject
		{
			public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(DependencyTarget));
			public object Value
			{
				get => GetValue(ValueProperty);
				set => SetValue(ValueProperty, value);
			}
		}
	}
}