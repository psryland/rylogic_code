﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	/// <summary>Extend DataGridTemplateColumn to alloow binding</summary>
	public class DataGridBoundTemplateColumn : DataGridTemplateColumn
	{
		// A DataGrid template column that forwards it's Binding to each Cell.
		// To use this:
		//  Define a 'DataTemplate' containing UI elements that contain bindings to some type or interface
		//  Use the 'DataGridBoundTemplateColumn' in your xaml 'DataGrid.Columns'
		//  The column type is basically a factory for cells in that column, the 'GenerateElement' method
		//  forwards the binding on to each created cell. The binding can contain a converter that converts
		//  the row type into some other type that is used by the bindings in the 'DataTemplate'.
		static DataGridBoundTemplateColumn()
		{
			BindingProperty = Gui_.DPRegister<DataGridBoundTemplateColumn>(nameof(Binding));
		}

		/// <summary>The binding forwarded to the 'CellTemplate' instances</summary>
		public Binding Binding
		{
			get { return (Binding)GetValue(BindingProperty); }
			set { SetValue(BindingProperty, value); }
		}
		public static readonly DependencyProperty BindingProperty;

		// Cell factory methods
		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
		{
			var element = base.GenerateEditingElement(cell, dataItem);
			element.SetBinding(ContentPresenter.ContentProperty, Binding);
			return element;
		}
		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			var element = base.GenerateElement(cell, dataItem);
			element.SetBinding(ContentPresenter.ContentProperty, Binding);
			return element;
		}
	}
}