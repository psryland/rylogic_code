using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI
{
	public partial class IndicatorLegend :UserControl
	{
		static IndicatorLegend()
		{
			ExpandedProperty = Gui_.DPRegister<IndicatorLegend>(nameof(Expanded));
			IndicatorsProperty = Gui_.DPRegister<IndicatorLegend>(nameof(Indicators));
		}
		public IndicatorLegend()
		{
			InitializeComponent();
			DataContext = this;
		}

		/// <summary>True if the legend is expanded</summary>
		public bool Expanded
		{
			get { return (bool)GetValue(ExpandedProperty); }
			set { SetValue(ExpandedProperty, value); }
		}
		public static readonly DependencyProperty ExpandedProperty;

		/// <summary>The indicators to display</summary>
		public ICollectionView Indicators
		{
			get { return (ICollectionView)GetValue(IndicatorsProperty); }
			set { SetValue(IndicatorsProperty, value); }
		}
		public static readonly DependencyProperty IndicatorsProperty;

		/// <summary>The selected indicator</summary>
		public IIndicatorView SelectedIndicator
		{
			get => (IIndicatorView)Indicators.CurrentItem;
			set => Indicators.MoveCurrentTo(value);
		}

		/// <summary>Raised when the order of indicators is changed in the legend</summary>
		public event EventHandler IndicatorsReordered;

		/// <summary>Show the property dialog for an indicator</summary>
		private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
			{
				var cell = DataGrid_.FindCell((DependencyObject)e.OriginalSource);
				var indy = (IIndicatorView)cell.GetRow().Item;
				indy.ShowOptionsUI();
				e.Handled = true;
			}
		}

		/// <summary>Toggle visibility of an indicator</summary>
		private void HandleVisibiliyToggle(object sender, MouseButtonEventArgs e)
		{
			var cell = DataGrid_.FindCell((DependencyObject)e.OriginalSource);
			var indy = (IIndicatorView)cell.GetRow().Item;
			indy.Visible = !indy.Visible;
			e.Handled = true;
		}

		/// <summary>Notification that reordering happened</summary>
		private void HandleReordered(object sender, RoutedEventArgs args)
		{
			IndicatorsReordered?.Invoke(this, EventArgs.Empty);
		}
	}
}
