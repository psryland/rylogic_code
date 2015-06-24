using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using pr.extn;
using pr.util;

namespace pr.container
{
	/// <summary>
	/// Represents a connection between a property on a data source object and
	/// a collection of objects with properties that should be updated with the
	/// value whenever it changes</summary>
	public class Binding<TSrc, TValue> where TSrc:class
	{
		/// <summary>The collection of objects attached to this binding</summary>
		private List<Link> m_bound;
		private class Link
		{
			private object m_target;
			private MethodInfo m_set;

			public Link(object target, MethodInfo set)
			{
				m_target = target;
				m_set = set;
			}
			public object Target
			{
				get { return m_target; }
			}
			public void Set(object value)
			{
				m_set.Invoke(m_target, new[]{value});
			}
		}

		/// <summary>The getter for the binding source value</summary>
		private MethodInfo m_get;

		/// <summary>Prefer Binding.Make()</summary>
		public Binding(TSrc data_source, string member_name, TValue def_value = default(TValue), BindingFlags flags = BindingFlags.Public|BindingFlags.Instance)
		{
			// Order is important here
			m_bound      = new List<Link>();
			DefaultValue = def_value;
			Flags        = flags;
			MemberName   = member_name;
			DataSource   = data_source;
		}

		/// <summary>Construct a binding from an expression</summary>
		public Binding(TSrc data_source, Expression<Func<TSrc,TValue>> expression, TValue def_value = default(TValue), BindingFlags flags = BindingFlags.Public|BindingFlags.Instance)
			:this(data_source, R<TSrc>.Name(expression), def_value, flags)
		{}

		/// <summary>The name of the property on the data source that provides the value</summary>
		public string MemberName
		{
			get { return m_member_name; }
			set
			{
				if (m_member_name == value) return;
				m_member_name = value;
				m_get = m_member_name.HasValue() ? typeof(TSrc).GetProperty(m_member_name, Flags).GetGetMethod() : null;
				ResetBindings();
			}
		}
		private string m_member_name;

		/// <summary>The object that is the source of the value</summary>
		public TSrc DataSource
		{
			get { return m_data_source; }
			set
			{
				if (m_data_source == value) return;
				if (m_data_source is INotifyPropertyChanged)
				{
					m_data_source.As<INotifyPropertyChanged>().PropertyChanged -= ResetBindings;
				}
				m_data_source = value;
				if (m_data_source is INotifyPropertyChanged)
				{
					m_data_source.As<INotifyPropertyChanged>().PropertyChanged += ResetBindings;
				}
				ResetBindings();
			}
		}
		private TSrc m_data_source;

		/// <summary>The value to use when 'DataSource' is null</summary>
		public TValue DefaultValue { get; set; }

		/// <summary>The binding flags used to find the property on the data source</summary>
		public BindingFlags Flags { get; set; }

		/// <summary>Enumerate the collection of objects attached to this binding</summary>
		public IEnumerable<object> BoundObjects { get { return m_bound.Select(x => x.Target); } }

		/// <summary>Connect 'obj.property_name' to the binding value</summary>
		public void Add(object obj, string property_name, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance)
		{
			var prop = obj.GetType().GetProperty(property_name, flags);
			if (prop == null)
				throw new Exception("Property '{0}' not found on object type {1}".Fmt(property_name, obj.GetType().Name));
			if (!prop.PropertyType.IsAssignableFrom(typeof(TValue)))
				throw new Exception("Property type mismatch. Binding source type {0} is not assignable to a property with type {1}.".Fmt(typeof(TValue).Name, prop.PropertyType.Name));

			var set = prop.GetSetMethod();
			m_bound.Add(new Link(obj, set));
		}
		public void Add<U,Ret>(U obj, Expression<Func<U,Ret>> expression, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance)
		{
			Add(obj, R<U>.Name(expression), flags);
		}

		/// <summary>Disconnect 'obj' from the binding value</summary>
		public void Remove(object obj)
		{
			m_bound.RemoveAll(x => x.Target == obj);
		}

		/// <summary>Push a change in the binding value out to the bound objects</summary>
		public void ResetBindings(object sender = null, EventArgs args = null)
		{
			if (m_get != null && DataSource != null)
			{
				var value = m_get.Invoke(DataSource, null);
				m_bound.ForEach(x => x.Set(value));
			}
			else
			{
				m_bound.ForEach(x => x.Set(DefaultValue));
			}
		}
	}
}


#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Windows.Forms;
	using container;

	[TestFixture] public class TestBinding
	{
		private class Thing :INotifyPropertyChanged
		{
			public string Value
			{
				get { return m_value; }
				set
				{
					if (m_value == value) return;
					m_value = value;
					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs(R<Thing>.Name(x=>x.Value)));
				}
			}
			private string m_value;
			public event PropertyChangedEventHandler PropertyChanged;
		}

		[Test] public void Binding()
		{
			// Bind Thing.Value to tb.Text and cb.Text
			var thing1 = new Thing{Value = "One"};
			var thing2 = new Thing{Value = "Two"};
			var tb = new TextBox();
			var cb = new ComboBox();

			// Create a binding to "Value" on an object
			var binding = new Binding<Thing, string>(null, x => x.Value);
			
			// Connect the tb and cb to the binding
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
