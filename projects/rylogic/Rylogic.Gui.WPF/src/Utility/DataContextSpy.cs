﻿using System;
using System.Windows;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	// Notes:
	//  - This doesn't seem to work how I thought it would.
	//    Use 'DataContextRef' instead.
#if false
	public class DataContextSpy : Freezable
	{
		// Credit: Josh Smith 2008
		//  https://www.codeproject.com/Articles/27432/Artificial-Inheritance-Contexts-in-WPF

		// Notes:
		//  - DataContextSpy observes the DataContext of an element tree so that external elements can bind
		//    against it to gain access to the DataContext. Add a DataContextSpy to the Resources collection
		//    of any element, except the element on which the DataContext was set, and its DataContext property
		//    will expose the DataContext of that element.
		//  - Freezable adds ElementName and DataContext bindings
		//
		// Example Use:
		//  - MyConverter is: class MyConverter :DependencyObject, IValueConverter {...} with a dependency property
		//   called 'SourceValue'. Converter's don't have an inheritable DataContext like normal UIElements so we
		//   need to specify the 'Source' for the binding, which is where 'DataContextSpy' comes in.
		//	<TextBox>
		//		<TextBox.Resources>
		//			<gui:DataContextSpy x:Key="DataContextSpy" />
		//		</TextBox.Resources>
		//		<TextBox.Text>
		//			<Binding Path = "YourProperty" ConverterParameter="1">
		//				<Binding.Converter>
		//					<gui:MyConverter SourceValue = "{Binding Source={StaticResource DataContextSpy}, Path=DataContext.YourProperty}"/>
		//				</Binding.Converter>
		//			</Binding>
		//		</TextBox.Text>
		//	</TextBox>

		public DataContextSpy()
		{
			// This binding allows the spy to inherit a DataContext.
			BindingOperations.SetBinding(this, SpiedCtxProperty, new Binding());
			IsSynchronizedWithCurrentItem = true;
		}
		protected override Freezable CreateInstanceCore()
		{
			// We are required to override this abstract method.
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets/sets whether the spy will return the CurrentItem of the ICollectionView that wraps
		/// the data context, assuming it is a collection of some sort.  If the data context is not a 
		/// collection, this property has no effect. The default value is true.</summary>
		public bool IsSynchronizedWithCurrentItem { get; set; }

		/// <summary>The captured DataContext from </summary>
		public object SpiedCtx
		{
			get => GetValue(SpiedCtxProperty);
			set => SetValue(SpiedCtxProperty, value);
		}
		private static object DataContext_Coerce(DependencyObject depObj, object value)
		{
			if (!(depObj is DataContextSpy spy))
				return value;

			if (spy.IsSynchronizedWithCurrentItem)
			{
				var view = CollectionViewSource.GetDefaultView(value);
				if (view != null)
					return view.CurrentItem;
			}

			return value;
		}

		/// <summary>Borrow the DataContext dependency property from FrameworkElement.</summary>
		public static readonly DependencyProperty SpiedCtxProperty =
			FrameworkElement.DataContextProperty.AddOwner(typeof(DataContextSpy), new PropertyMetadata(null, null, DataContext_Coerce));
	}
#endif
}
