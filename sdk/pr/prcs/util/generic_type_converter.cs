//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.ComponentModel;

namespace pr.util
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
		public override string ToString() { return StrTxfm.Apply(typeof(T).Name, StrTxfm.ECapitalise.UpperCase, StrTxfm.ECapitalise.LowerCase, StrTxfm.ESeparate.Add, " ", ""); }
		public override bool GetPropertiesSupported    (ITypeDescriptorContext context) { return true; }
		public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) { return true; }
		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) { return TypeDescriptor.GetProperties(value ,attributes); }
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
	}
}
