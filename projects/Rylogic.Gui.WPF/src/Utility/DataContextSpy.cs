using System;
using System.Windows;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
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
		//			<gui2:DataContextSpy x:Key="DataContextSpy" />
		//		</TextBox.Resources>
		//		<TextBox.Text>
		//			<Binding Path = "YourProperty" ConverterParameter="1">
		//				<Binding.Converter>
		//					<gui2:MyConverter SourceValue = "{Binding Source={StaticResource DataContextSpy}, Path=DataContext.YourProperty}"/>
		//				</Binding.Converter>
		//			</Binding>
		//		</TextBox.Text>
		//	</TextBox>

		public DataContextSpy()
		{
			// This binding allows the spy to inherit a DataContext.
			BindingOperations.SetBinding(this, DataContextProperty, new Binding());
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

		// Borrow the DataContext dependency property from FrameworkElement.
		public object DataContext
		{
			get { return (object)GetValue(DataContextProperty); }
			set { SetValue(DataContextProperty, value); }
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
		public static readonly DependencyProperty DataContextProperty =
			FrameworkElement.DataContextProperty.AddOwner(typeof(DataContextSpy), new PropertyMetadata(null, null, DataContext_Coerce));
	}
}
