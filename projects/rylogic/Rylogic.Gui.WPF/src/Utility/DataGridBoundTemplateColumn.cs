using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	/// <summary>Extend DataGridTemplateColumn to allow binding</summary>
	public class DataGridBoundTemplateColumn : DataGridTemplateColumn
	{
		// A DataGrid template column that forwards it's Binding to each Cell.
		// To use this:
		//  Define a 'DataTemplate' containing UI elements that contain bindings to some type or interface
		//  Use the 'DataGridBoundTemplateColumn' in your xaml 'DataGrid.Columns'
		//  The column type is basically a factory for cells in that column, the 'GenerateElement' method
		//  forwards the binding on to each created cell. The binding can contain a converter that converts
		//  the row type into some other type that is used by the bindings in the 'DataTemplate'.

		/// <summary>The binding forwarded to the 'CellTemplate' instances</summary>
		public Binding Binding
		{
			get => (Binding)GetValue(BindingProperty);
			set => SetValue(BindingProperty, value);
		}
		public static readonly DependencyProperty BindingProperty = Gui_.DPRegister<DataGridBoundTemplateColumn>(nameof(Binding), null, Gui_.EDPFlags.None);

		// Cell factory methods
		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			var element = base.GenerateElement(cell, dataItem);
			if (Binding != null) element.SetBinding(ContentPresenter.ContentProperty, Binding);
			return element;
		}
		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
		{
			var element = base.GenerateEditingElement(cell, dataItem);
			if (Binding != null) element.SetBinding(ContentPresenter.ContentProperty, Binding);
			return element;
		}
	}
}
