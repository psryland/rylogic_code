using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Common
{
	/// <summary>
	/// Represents a connection between a property on a data source object and
	/// a collection of objects with properties that should be updated with the
	/// value whenever it changes.<para/>
	/// For example:<para/>
	///   public class Person { public string Name {get; set;} }<para/>
	///   var name_binding = new Binding<Person, string>(null, x => x.Name);<para/>
	///   name_binding.Add(person_name_textbox, x => x.Text);<para/>
	///   <para/>
	///   name_binding.DataSource = new Person("Joe");<para/>
	///   Assert.Equal(person_name_textbox.Text, "Joe");<para/>
	///   <para/>
	///   See unit tests for more detailed examples</para>
	/// </para>
	///  NOTE: Does *not* automatically detected changes in the source data unless the source
	///  implements 'INotifyPropertyChanged' and raises the 'PropertyChanged' event when the bound
	///  value is changed.</summary>
	public class DataBind<TSrc, TValue> where TSrc : class
	{
		/// <summary>Prefer Binding.Make()</summary>
		public DataBind(TSrc? data_source, string member_name, TValue def_value = default!, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
		{
			// Order is important here
			m_get = null;
			m_member_name = string.Empty;
			m_bound = new List<Link>();
			DefaultValue = def_value;
			Flags = flags;
			MemberName = member_name;
			DataSource = data_source;
		}

		/// <summary>Construct a binding from an expression</summary>
		public DataBind(TSrc? data_source, Expression<Func<TSrc, TValue>> expression, TValue def_value = default!, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
			: this(data_source, R<TSrc>.Name(expression), def_value, flags)
		{ }

		/// <summary>The collection of objects attached to this binding</summary>
		private List<Link> m_bound;
		private class Link
		{
			private readonly MethodInfo m_set;
			public Link(object target, MethodInfo set)
			{
				Target = target;
				m_set = set;
			}
			public object Target { get; }
			public void Set(object? value)
			{
				m_set.Invoke(Target, new[] { value });
			}
		}

		/// <summary>The getter for the binding source value</summary>
		private MethodInfo? m_get;

		/// <summary>The name of the property on the data source that provides the value</summary>
		public string MemberName
		{
			get => m_member_name;
			set
			{
				if (m_member_name == value) return;
				m_member_name = value;
				m_get = m_member_name.HasValue() ? typeof(TSrc).GetProperty(m_member_name, Flags)?.GetGetMethod() : null;
				ResetBindings();
			}
		}
		private string m_member_name;

		/// <summary>The object that is the source of the value</summary>
		public TSrc? DataSource
		{
			get => m_data_source;
			set
			{
				if (m_data_source == value) return;
				if (m_data_source is INotifyPropertyChanged npc0)
				{
					npc0.PropertyChanged -= ResetBindings;
				}
				m_data_source = value;
				if (m_data_source is INotifyPropertyChanged npc1)
				{
					npc1.PropertyChanged += ResetBindings;
				}
				ResetBindings();
			}
		}
		private TSrc? m_data_source;

		/// <summary>The value to use when 'DataSource' is null</summary>
		public TValue DefaultValue { get; set; }

		/// <summary>Get the current value from the 'DataSource' or null if not yet bound</summary>
		public TValue Value => (m_get != null && DataSource != null) ? (TValue)m_get.Invoke(DataSource, null)! : DefaultValue;

		/// <summary>The binding flags used to find the property on the data source</summary>
		public BindingFlags Flags { get; set; }

		/// <summary>Enumerate the collection of objects attached to this binding</summary>
		public IEnumerable<object> BoundObjects { get { return m_bound.Select(x => x.Target); } }

		/// <summary>Connect 'obj.property_or_method_name' to the binding value. Returns this binding for method chaining</summary>
		public DataBind<TSrc, TValue> Add(object obj, string property_or_method_name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
		{
			var ty = obj.GetType();

			// Look for a property with the name 'property_or_method_name'
			var prop = ty.GetProperty(property_or_method_name, flags);
			if (prop != null)
			{
				// Property type must match the bound value type
				if (!prop.PropertyType.IsAssignableFrom(typeof(TValue)))
					throw new Exception($"Property type mismatch. Binding source type {typeof(TValue).Name} is not assignable to a property with type {prop.PropertyType.Name}.");

				// Add the binding
				var set = prop.GetSetMethod() ?? throw new Exception($"Property {property_or_method_name} has not setter");
				var bind = new Link(obj, set);
				m_bound.Add(bind);

				// Apply the current value to the newly bound object
				bind.Set(Value);
				return this;
			}

			// Look for a method with the name 'property_or_method_name'
			var meth = ty.GetMethod(property_or_method_name, flags);
			if (meth != null)
			{
				// Method must take a single parameter that is the property
				var parms = meth.GetParameters();
				if (parms.Length != 1 || !parms[0].ParameterType.IsAssignableFrom(typeof(TValue)))
					throw new Exception($"Method signature mismatch. The bound method must take a single parameter of type {typeof(TValue).Name}.");

				// Add the binding
				var bind = new Link(obj, meth);
				m_bound.Add(bind);

				// Apply the current value to the newly bound object
				bind.Set(Value);
				return this;
			}

			throw new Exception($"Property or Method '{property_or_method_name}' not found on object type {ty.Name}");
		}
		public DataBind<TSrc, TValue> Add<U, Ret>(U obj, Expression<Func<U, Ret>> expression, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) where U : notnull
		{
			return Add(obj, R<U>.Name(expression), flags);
		}

		/// <summary>Disconnect 'obj' from the binding value</summary>
		public void Remove(object obj)
		{
			m_bound.RemoveAll(x => x.Target == obj);
		}

		/// <summary>Push a change in the binding value out to the bound objects</summary>
		public void ResetBindings(object? sender = null, EventArgs? args = null)
		{
			var value = Value;
			m_bound.ForEach(x => x.Set(value));
			ValueChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Raised when 'ResetBindings' is called on this binding.
		/// If the bound object implements INotifyPropertyChanged and raises PropertyChanged events
		/// then this event will be raised automatically when the bound value changes.
		/// Note: 'sender' == this binding, not 'DataSource'</summary>
		public event EventHandler? ValueChanged;
	}

	public static class DataBind
	{
		public static DataBind<TSrc, TValue> Make<TSrc, TValue>(TSrc data_source, string member_name, TValue def_value = default, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance) where TSrc:class
		{
			return new DataBind<TSrc, TValue>(data_source, member_name, def_value!, flags);
		}
	}
}


#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture] public class TestDataBind
	{
		private class Thing :INotifyPropertyChanged
		{
			public string? Value
			{
				get => m_value;
				set
				{
					if (m_value == value) return;
					m_value = value;
					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs(nameof(Value)));
				}
			}
			private string? m_value;
			public event PropertyChangedEventHandler? PropertyChanged;
		}
		private class TextBox
		{
			public string? Text { get; set; }
		}
		private class ComboBox
		{
			public string? Text { get; set; }
		}

		[Test] public void Binding()
		{
			// Bind Thing.Value to tb.Text and cb.Text
			var thing1 = new Thing{Value = "One"};
			var thing2 = new Thing{Value = "Two"};
			var tb = new TextBox();
			var cb = new ComboBox();

			// Create a binding to "Value" on an object
			var binding = new DataBind<Thing, string>(null, nameof(Thing.Value));
			
			// Connect the text box and combo box to the binding
			binding.Add(tb, x => x.Text);
			binding.Add(cb, x => x.Text);

			// Change the binding data source
			binding.DataSource = thing1;
			Assert.True(tb.Text == thing1.Value);
			Assert.True(cb.Text == thing1.Value);
			binding.DataSource = thing2;
			Assert.True(tb.Text == thing2.Value);
			Assert.True(cb.Text == thing2.Value);

			// Change the source value
			thing2.Value = "222";
			//binding.ResetBindings(); <- not needed because Thing implements INotifiyPropertyChanged
			Assert.True(tb.Text == thing2.Value);
			Assert.True(cb.Text == thing2.Value);

			// Remove 'tb' from the binding
			binding.Remove(tb);
			binding.DataSource = thing1;
			Assert.True(tb.Text == thing2.Value);
			Assert.True(cb.Text == thing1.Value);
		}
	}
}
#endif
