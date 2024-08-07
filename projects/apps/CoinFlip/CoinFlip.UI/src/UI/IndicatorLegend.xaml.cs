﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class IndicatorLegend :UserControl
	{
		public IndicatorLegend()
		{
			InitializeComponent();
			DataContext = this;
		}

		/// <summary>True if the legend is expanded</summary>
		public bool Expanded
		{
			get => (bool)GetValue(ExpandedProperty);
			set => SetValue(ExpandedProperty, value);
		}
		public static readonly DependencyProperty ExpandedProperty = Gui_.DPRegister<IndicatorLegend>(nameof(Expanded), Boxed.False, Gui_.EDPFlags.TwoWay);

		/// <summary>The indicators to display</summary>
		public ICollectionView Indicators
		{
			get => (ICollectionView)GetValue(IndicatorsProperty);
			set => SetValue(IndicatorsProperty, value);
		}
		public static readonly DependencyProperty IndicatorsProperty = Gui_.DPRegister<IndicatorLegend>(nameof(Indicators), null, Gui_.EDPFlags.None);

		/// <summary>The selected indicator</summary>
		public IIndicatorView SelectedIndicator
		{
			get => (IIndicatorView)Indicators.CurrentItem;
			set => Indicators.MoveCurrentTo(value);
		}

		/// <summary>Raised when the order of indicators is changed in the legend</summary>
		public event EventHandler? IndicatorsReordered;

		/// <summary>Show the property dialog for an indicator</summary>
		private void HandleDoubleClick(object? sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
			{
				// Todo: change this to use a RowStyle with an EventSetter
				var cell = DataGrid_.FindCell((DependencyObject)e.OriginalSource) ?? throw new Exception("The source of the double click is not found");
				var indy = (IIndicatorView)cell.GetRow().Item;
				indy.ShowOptionsUI();
				e.Handled = true;
			}
		}

		/// <summary>Toggle visibility of an indicator</summary>
		private void HandleVisibiliyToggle(object sender, MouseButtonEventArgs e)
		{
			var cell = DataGrid_.FindCell((DependencyObject)e.OriginalSource) ?? throw new Exception("The source of the visibility toggle is not found");
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
