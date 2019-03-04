//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.ComponentModel;
using Rylogic.Extn;

namespace Rylogic.Utility
{
	/// <summary>Generic type converter for displaying classes in property grids.</summary>
	/// <remarks>
	/// Usage:
	///   [TypeConverter(typeof(MyType))]
	///   public class MyType :GenericTypeConverter&lt;MyType&gt; {}
	/// or
	///   [TypeConverter(typeof(MyTypeConv))] public struct MyType {}
	///   public class MyTypeConv :GenericTypeConverter&lt;MyType&gt; {}
	/// </remarks>
	public class GenericTypeConverter<T> :TypeConverter where T: new()
	{
		public override string ToString()
		{
			return typeof(T).Name.Txfm(Str_.ECapitalise.UpperCase, Str_.ECapitalise.LowerCase, Str_.ESeparate.Add, " ", "");
		}
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}
		public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
		{
			return AllowCreateInstance;
		}
		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			return TypeDescriptor.GetProperties(value ,attributes);
		}
		public override object CreateInstance(ITypeDescriptorContext context, IDictionary values)
		{
			T t = new T();
			foreach (var prop in typeof(T).GetProperties())
			{
				var value = values[prop.Name];
				if (value != null) prop.SetValue(t, value, null);
			}
			return t;
		}
		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			return base.ConvertTo(context, culture, value, destinationType);
		}

		/// <summary>
		/// Gets whether the property grid will try to clone 'T' rather than changing the original.
		/// Note: if this returns true, the object must be ICloneable or a new instance still won't be created</summary>
		protected virtual bool AllowCreateInstance { get { return false; } }
	}
}
