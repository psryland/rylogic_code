using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public partial class TreeGridColumn : DataGridTemplateColumn
	{
		public TreeGridColumn()
		{
			InitializeComponent();
		}

		/// <summary>The grid that contains this column</summary>
		internal TreeGrid Owner => (TreeGrid)DataGridOwner;

		/// <summary>The binding for the content to display in the cell</summary>
		public BindingBase? Binding
		{
			get => m_binding;
			set
			{
				if (m_binding == value) return;
				m_binding = value;
				CoerceValue(IsReadOnlyProperty);
				CoerceValue(SortMemberPathProperty);
				NotifyPropertyChanged(nameof(Binding));
			}
		}
		private BindingBase? m_binding;

		/// <summary>The binding for the optional image to display in the cell</summary>
		public BindingBase? Image
		{
			get => m_image;
			set
			{
				if (m_image == value) return;
				m_image = value;
				NotifyPropertyChanged(nameof(Image));
			}
		}
		private BindingBase? m_image;

		/// <summary>The binding for accessing the child elements of the node</summary>
		public BindingBase? Children
		{
			get => m_children;
			set
			{
				if (m_children == value) return;
				m_children = value;
				NotifyPropertyChanged(nameof(Children));
			}
		}
		private BindingBase? m_children;

		/// <summary>The size of the indent amount (can be a binding or int)</summary>
		public int IndentSize
		{
			get => (int)GetValue(IndentSizeProperty);
			set => SetValue(IndentSizeProperty, value);
		}
		private void IndentSize_Changed()
		{
			NotifyPropertyChanged(nameof(IndentSize));
			InvalidateProperty(IndentSizeProperty);
		}
		public static readonly DependencyProperty IndentSizeProperty = Gui_.DPRegister<TreeGridColumn>(nameof(IndentSize), 8, Gui_.EDPFlags.None);

		/// <summary>Get/Set the font family for the content of cells in the column.</summary>
		public FontFamily FontFamily
		{
			get => (FontFamily)GetValue(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}
		public static readonly DependencyProperty FontFamilyProperty = Gui_.DPRegister<TreeGridColumn>(nameof(FontFamily), SystemFonts.MessageFontFamily, Gui_.EDPFlags.None);

		/// <summary>Get/Set the font size for the content of cells in the column.</summary>
		[TypeConverter(typeof(FontSizeConverter))]
		[Localizability(LocalizationCategory.None)]
		public double FontSize
		{
			get => (double)GetValue(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}
		public static readonly DependencyProperty FontSizeProperty = Gui_.DPRegister<TreeGridColumn>(nameof(FontSize), SystemFonts.MessageFontSize, Gui_.EDPFlags.None);

		/// <summary>Get/Set the font style for the content of cells in the column.</summary>
		public FontStyle FontStyle
		{
			get => (FontStyle)GetValue(FontStyleProperty);
			set => SetValue(FontStyleProperty, value);
		}
		public static readonly DependencyProperty FontStyleProperty = Gui_.DPRegister<TreeGridColumn>(nameof(FontStyle), SystemFonts.MessageFontStyle, Gui_.EDPFlags.None);

		/// <summary>Get/Set the font weight for the content of cells in the column.</summary>
		public FontWeight FontWeight
		{
			get => (FontWeight)GetValue(FontWeightProperty);
			set => SetValue(FontWeightProperty, value);
		}
		public static readonly DependencyProperty FontWeightProperty = Gui_.DPRegister<TreeGridColumn>(nameof(FontWeight), SystemFonts.MessageFontWeight, Gui_.EDPFlags.None);

		/// <summary>Get/Set the System.Windows.Media.Brush that is used to paint the text contents of cells in the column.</summary>
		public Brush Foreground
		{
			get => (Brush)GetValue(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}
		public static readonly DependencyProperty ForegroundProperty = Gui_.DPRegister<TreeGridColumn>(nameof(Foreground), SystemColors.ControlTextBrush, Gui_.EDPFlags.None);

		/// <summary>The maximum height of the cell</summary>
		public double MaxHeight
		{
			get => (double)GetValue(MaxHeightProperty);
			set => SetValue(MaxHeightProperty, value);
		}
		public static readonly DependencyProperty MaxHeightProperty = Gui_.DPRegister<TreeGridColumn>(nameof(MaxHeight), double.PositiveInfinity, Gui_.EDPFlags.None);

		// Cell factory methods
		protected override FrameworkElement GenerateElement(DataGridCell cell, object data_item)
		{
			// Create the cell element and bind it to the wrapper.
			var element = base.GenerateElement(cell, data_item);
			element.SetBinding(ContentPresenter.ContentProperty, new Binding { Converter = new Converter(Owner) });
			return element;
		}
		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object data_item)
		{
			// Create the cell element and bind it to the wrapper.
			var element = base.GenerateEditingElement(cell, data_item);
			element.SetBinding(ContentPresenter.ContentProperty, new Binding { Converter = new Converter(Owner) });
			return element;
		}

		/// <summary>A converter that turns a 'data_item' into the node that wraps it</summary>
		private class Converter : IValueConverter
		{
			private readonly TreeGrid m_tree_grid;
			public Converter(TreeGrid tree_grid) => m_tree_grid = tree_grid;
			public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return m_tree_grid.List.FindNode(value);
			}
			public object? ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
			{
				return value is TreeGrid.Node node ? node : null;
			}
		}
	}
}
