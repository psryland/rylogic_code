using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rylogic.Gui.WPF.ChartDetail;

namespace Rylogic.Gui.WPF
{
	public partial class ChartLegend :UserControl, INotifyPropertyChanged
	{
		// Notes:
		//  - Binding the ChartLegend is tricky. It works like this:
		//    - ChartControl as a 'Legend' property that can be set to any FrameworkElement.
		//    - Because this element is a child of the ChartControl (via Overlay), it's DataContext is the ChartControl.
		//    - If you want to set the Legend's DataContext to something else, you probably need to use 'DataContextRef'
		//    - If a 'ChartLegend' instance is assigned to the ChartControl's 'Legend' property, then the chart will
		//      initialise the 'ItemsSource' with the chart's 'Elements' collection (unless already set).
		//  - The simple usage is:
		//      <view3d:ChartControl
		//      	>
		//      	<view3d:ChartControl.Legend>
		//      		<view3d:ChartLegend/>
		//      	</view3d:ChartControl.Legend>
		//      </view3d:ChartControl>
		//
		//     ... or you can make the legend a static resource:
		//     <ResourceDictionary>
		//         <gui:DataContextRef x:Key="Ctx" Ctx="{Binding .}"/>
		//         <view3d:ChartLegend x:Key="MyLegend" ItemsSource="{Binding Ctx.MyLegendItems, Source={StaticResource Ctx}}" />
		//     </ResourceDictionary>
		//     ...
		//     <view3d:ChartControl
		//         Legend="{StaticResource MyLegend}"
		//         />
	
		public ChartLegend()
		{
			InitializeComponent();
			ToggleVisibility = Command.Create(this, ToggleVisibilityInternal);
			ItemActivated = Command.Create(this, ItemActivatedInternal);
			// Don't set DataContext, it's inherited from: ChartControl -> Canvas(overlay) -> ChartLegend
		}

		/// <summary>True if the legend is expanded</summary>
		public bool Expanded
		{
			get => (bool)GetValue(ExpandedProperty);
			set => SetValue(ExpandedProperty, value);
		}
		public static readonly DependencyProperty ExpandedProperty = Gui_.DPRegister<ChartLegend>(nameof(Expanded), false);

		/// <summary>True if items in the legend can be reordered</summary>
		public bool CanUsersReorderItems
		{
			get => (bool)GetValue(CanUsersReorderItemsProperty);
			set => SetValue(CanUsersReorderItemsProperty, value);
		}
		public static readonly DependencyProperty CanUsersReorderItemsProperty = Gui_.DPRegister<ChartLegend>(nameof(CanUsersReorderItems), false);

		/// <summary>The elements to display</summary>
		public IEnumerable ItemsSource
		{
			get => (IEnumerable)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}
		public static readonly DependencyProperty ItemsSourceProperty = Gui_.DPRegister<ChartLegend>(nameof(ItemsSource), flags: FrameworkPropertyMetadataOptions.None);

		/// <summary>The selected element</summary>
		public IChartLegendItem? SelectedItem
		{
			get => m_selected_item;
			set
			{
				if (m_selected_item == value) return;
				m_selected_item = value;
				NotifyPropertyChanged(nameof(SelectedItem));
			}
		}
		private IChartLegendItem? m_selected_item;

		/// <summary>Show/Hide the current item</summary>
		public Command ToggleVisibility { get; }
		private void ToggleVisibilityInternal()
		{
			if (SelectedItem == null)
				return;

			SelectedItem.Visible = !SelectedItem.Visible;
		}

		/// <summary></summary>
		public Command ItemActivated { get; }
		private void ItemActivatedInternal()
		{
		}


		private void HandleReordered(object sender, RoutedEventArgs args)
		{

		}

		private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
		{

		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
