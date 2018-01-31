using System;
using System.ComponentModel;

namespace Rylogic.Attrib
{
	public class AbstractControlDescriptionProvider<TAbstract, TBase> :TypeDescriptionProvider
	{
		// Notes:
		// This class is used when a user control has an abstract base class and you
		// want to use the WinForms designer. Add an attribute to the abstract base
		// class like this:
		// [TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<AbstractControl, UserControl>))]
		// public abstract class MyControlBase :UserControl { ... }
	
		public AbstractControlDescriptionProvider()
			:base(TypeDescriptor.GetProvider(typeof(TAbstract)))
		{}

		public override Type GetReflectionType(Type objectType, object instance)
		{
			if (objectType == typeof(TAbstract))
				return typeof(TBase);

			return base.GetReflectionType(objectType, instance);
		}

		public override object CreateInstance(IServiceProvider provider, Type objectType, Type[] argTypes, object[] args)
		{
			if (objectType == typeof(TAbstract))
				objectType = typeof(TBase);

			return base.CreateInstance(provider, objectType, argTypes, args);
		}
	}
}
