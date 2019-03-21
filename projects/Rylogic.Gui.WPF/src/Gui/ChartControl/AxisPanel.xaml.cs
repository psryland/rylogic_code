using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Rylogic.Extn.Windows;

namespace Rylogic.Gui.WPF.ChartDetail
{
	public partial class AxisPanel : Canvas, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - Represents the tick marks and tick labels of an axis.
		//  - This component is intended to be able to go on any size of the graph.

		static AxisPanel()
		{
			SideProperty = Gui_.DPRegister<AxisPanel>(nameof(Side));
			FontFamilyProperty = Gui_.DPRegister<AxisPanel>(nameof(FontFamily), def: new FontFamily("tahoma"));
			FontStyleProperty = Gui_.DPRegister<AxisPanel>(nameof(FontStyle), def: FontStyles.Normal);
			FontWeightProperty = Gui_.DPRegister<AxisPanel>(nameof(FontWeight), def: FontWeights.Normal);
			FontStretchProperty = Gui_.DPRegister<AxisPanel>(nameof(FontStretch), def: FontStretches.Normal);
			FontSizeProperty = Gui_.DPRegister<AxisPanel>(nameof(FontSize), def: 10.0);
		}
		public AxisPanel()
		{
			InitializeComponent();

			// Commands
			ToggleScrollLock = Command.Create(this, () => Axis.AllowScroll = !Axis.AllowScroll);
			ToggleZoomLock = Command.Create(this, () => Axis.AllowZoom = !Axis.AllowZoom);

			DataContext = this;
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			UpdateGraphics();
		}
		public void Dispose()
		{
			Axis = null;
		}

		/// <summary>The side of the chart that this axis is positioned</summary>
		public Dock Side
		{
			get { return (Dock)GetValue(SideProperty); }
			set { SetValue(SideProperty, value); }
		}
		private static DependencyProperty SideProperty;

		/// <summary>Font for tick marks</summary>
		public FontFamily FontFamily
		{
			get { return (FontFamily)GetValue(FontFamilyProperty); }
			set { SetValue(FontFamilyProperty, value); }
		}
		public static readonly DependencyProperty FontFamilyProperty;

		/// <summary>Font style</summary>
		public FontStyle FontStyle
		{
			get { return (FontStyle)GetValue(FontStyleProperty); }
			set { SetValue(FontStyleProperty, value); }
		}
		public static readonly DependencyProperty FontStyleProperty;

		/// <summary>Font weight</summary>
		public FontWeight FontWeight
		{
			get { return (FontWeight)GetValue(FontWeightProperty); }
			set { SetValue(FontWeightProperty, value); }
		}
		public static readonly DependencyProperty FontWeightProperty;

		/// <summary>Font stretch</summary>
		public FontStretch FontStretch
		{
			get { return (FontStretch)GetValue(FontStretchProperty); }
			set { SetValue(FontStretchProperty, value); }
		}
		public static readonly DependencyProperty FontStretchProperty;

		/// <summary>Font size for tick labels</summary>
		public double FontSize
		{
			get { return (double)GetValue(FontSizeProperty); }
			set { SetValue(FontSizeProperty, value); }
		}
		public static readonly DependencyProperty FontSizeProperty;

		/// <summary>Font for tick labels</summary>
		public Typeface Typeface => new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

		/// <summary>The axis represented by this visual</summary>
		public ChartControl.RangeData.Axis Axis
		{
			get { return (ChartControl.RangeData.Axis)GetValue(AxisProperty); }
			set { SetValue(AxisProperty, value); }
		}
		public static readonly DependencyProperty AxisProperty = Gui_.DPRegister<AxisPanel>(nameof(Axis));
		private void Axis_Changed(ChartControl.RangeData.Axis old_value, ChartControl.RangeData.Axis new_value)
		{
			if (old_value != null)
			{
				Options.PropertyChanged -= HandleOptionChanged;
				old_value.Scroll -= HandleMoved;
				old_value.Zoomed -= HandleMoved;
			}
			if (new_value != null)
			{
				new_value.Zoomed += HandleMoved;
				new_value.Scroll += HandleMoved;
				Options.PropertyChanged += HandleOptionChanged;
			}

			// Invalidate cached values
			m_axis_size = null;

			// Notify options changed
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Options)));

			// Handlers
			void HandleMoved(object sender, EventArgs e)
			{
				if (m_update_graphics_pending) return;
				m_update_graphics_pending = true;
				Dispatcher.BeginInvoke(UpdateGraphics);
			}
			void HandleOptionChanged(object sender, PropertyChangedEventArgs e)
			{
				switch (e.PropertyName)
				{
				case nameof(ChartControl.OptionsData.Axis.AxisColour):
				case nameof(ChartControl.OptionsData.Axis.AxisThickness):
					UpdateGraphics();
					break;
				case nameof(ChartControl.OptionsData.Axis.DrawTickLabels):
				case nameof(ChartControl.OptionsData.Axis.DrawTickMarks):
					m_axis_size = null;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AxisSize)));
					UpdateGraphics();
					break;
				case nameof(ChartControl.OptionsData.Axis.TickTextTemplate):
					m_axis_size = null;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AxisSize)));
					UpdateGraphics();
					break;
				}
			}
		}

		/// <summary>The axis options</summary>
		public ChartControl.OptionsData.Axis Options => Axis?.Options ?? new ChartControl.OptionsData.Axis();

		/// <summary>The size of the axis area</summary>
		public double AxisSize
		{
			get
			{
				if (m_axis_size == null)
				{
					// Assume no axis to start with
					m_axis_size = 0;

					// Add space for tick marks
					if (Options.DrawTickMarks)
						m_axis_size += Options.TickLength;

					// Add space for tick labels
					if (Options.DrawTickLabels)
					{
						var measure = new TextBlock { Text = Options.TickTextTemplate, TextWrapping = TextWrapping.Wrap };
						measure.Typeface(Typeface, FontSize);
						measure.Measure(Size_.Infinity);
						m_axis_size +=
							Side == Dock.Left || Side == Dock.Right ? measure.DesiredSize.Width :
							Side == Dock.Top || Side == Dock.Bottom ? measure.DesiredSize.Height :
							0;
					}
				}
				return m_axis_size.Value;
			}
		}
		private double? m_axis_size;

		/// <summary>Update the tick marks and labels</summary>
		public void UpdateGraphics()
		{
			Children.Clear();
			if (Axis == null)
				return;

			// Get the positions for the tick marks
			Axis.GridLines(out var min, out var max, out var step);

			// Add a line for the Chart axis
			var axis_line = Children.Add2(new Line());
			axis_line.Stroke = new SolidColorBrush(Options.AxisColour.ToMediaColor());
			axis_line.StrokeThickness = Options.AxisThickness;;

			var bsh = new SolidColorBrush(Options.TickColour.ToMediaColor());
			switch (Side)
			{
			case Dock.Left: // Left-side Y-Axis
				{
					// Position the axis line on the right side of this canvas
					axis_line.X1 = ActualWidth;
					axis_line.X2 = ActualWidth;
					axis_line.Y1 = 0;
					axis_line.Y2 = ActualHeight;

					// Add tick marks and labels
					for (var y = min; y < max; y += step)
					{
						var X = ActualWidth;
						var Y = ActualHeight - y * ActualHeight / Axis.Span;
						if (Options.DrawTickMarks)
						{
							Children.Add(new Line { Stroke = bsh, X1 = X, Y1 = Y, X2 = X - Options.TickLength, Y2 = Y });
						}
						if (Options.DrawTickLabels)
						{
							var s = Axis.TickText(y + Axis.Min, step);
							var lbl = Children.Add2(new TextBlock { Text = s, Foreground = bsh });
							lbl.Typeface(Typeface, FontSize);
							lbl.Measure(Size_.Infinity);
							SetLeft(lbl, X - lbl.DesiredSize.Width - Options.TickLength);
							SetTop(lbl, Y - lbl.DesiredSize.Height / 2);
						}
					}
					break;
				}
			case Dock.Bottom: // Bottom-side X-Axis
				{
					// Position the axis line on the top side of this canvas
					axis_line.X1 = 0;
					axis_line.X2 = ActualWidth;
					axis_line.Y1 = 0;
					axis_line.Y2 = 0;

					// Add tick marks and labels
					for (var x = min; x < max; x += step)
					{
						var Y = 0;
						var X = x * ActualWidth / Axis.Span;
						if (Options.DrawTickMarks)
						{
							Children.Add(new Line { Stroke = bsh, X1 = X, Y1 = Y, X2 = X, Y2 = Y + Options.TickLength });
						}
						if (Options.DrawTickLabels)
						{
							var s = Axis.TickText(x + Axis.Min, step);
							var lbl = Children.Add2(new TextBlock { Text = s, Foreground = bsh });
							lbl.Typeface(Typeface, FontSize);
							lbl.Measure(Size_.Infinity);
							SetLeft(lbl, X - lbl.DesiredSize.Width / 2);
							SetTop(lbl, Options.TickLength);
						}
					}
					break;
				}
			case Dock.Right: // Right-side Y-Axis
				{
					// Position the axis line on the left side of this canvas
					axis_line.X1 = 0;
					axis_line.X2 = 0;
					axis_line.Y1 = 0;
					axis_line.Y2 = ActualHeight;

					// Add tick marks and labels
					for (var y = min; y < max; y += step)
					{
						var X = 0;
						var Y = ActualHeight - y * ActualHeight / Axis.Span;
						if (Options.DrawTickMarks)
						{
							Children.Add(new Line { Stroke = bsh, X1 = X, Y1 = Y, X2 = X + Options.TickLength, Y2 = Y });
						}
						if (Options.DrawTickLabels)
						{
							var s = Axis.TickText(y + Axis.Min, step);
							var lbl = Children.Add2(new TextBlock { Text = s, Foreground = bsh });
							lbl.Typeface(Typeface, FontSize);
							lbl.Measure(Size_.Infinity);
							SetLeft(lbl, X + Options.TickLength);
							SetTop(lbl, Y - lbl.DesiredSize.Height / 2);
						}
					}
					break;
				}
			case Dock.Top: // Top-side X-Axis
				{
					// Position the axis line on the bottom side of this canvas
					axis_line.X1 = 0;
					axis_line.X2 = ActualWidth;
					axis_line.Y1 = ActualHeight;
					axis_line.Y2 = ActualHeight;

					// Add tick marks and labels
					for (var x = min; x < max; x += step)
					{
						var Y = ActualHeight;
						var X = x * ActualWidth / Axis.Span;
						if (Options.DrawTickMarks)
						{
							Children.Add(new Line { Stroke = bsh, X1 = X, Y1 = Y, X2 = X, Y2 = Y + Options.TickLength });
						}
						if (Options.DrawTickLabels)
						{
							var s = Axis.TickText(x + Axis.Min, step);
							var lbl = Children.Add2(new TextBlock { Text = s, Foreground = bsh });
							lbl.Typeface(Typeface, FontSize);
							lbl.Measure(Size_.Infinity);
							SetLeft(lbl, X - lbl.DesiredSize.Width / 2);
							SetBottom(lbl, Y - Options.TickLength);
						}
					}
					break;
				}
			}

			m_update_graphics_pending = false;
		}
		private bool m_update_graphics_pending;

		/// <summary>Toggle the scroll locked state of the axis</summary>
		public Command ToggleScrollLock { get; }

		/// <summary>Toggle the scroll locked state of the axis</summary>
		public Command ToggleZoomLock { get; }

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
