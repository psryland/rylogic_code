using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public partial class RadialProgressControl :UserControl, INotifyPropertyChanged
	{
		/// <summary>The native radius of the control</summary>
		private const double Radius = 128.0;

		public RadialProgressControl()
		{
			// Don't set default values for dependency properties because it overrides
			// the default value passed to 'DPRegister' when used in other controls.
			InitializeComponent();
		}
		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			Dispatcher.BeginInvoke(new Action(UpdateGfx));
		}
		protected override Size MeasureOverride(Size constraint)
		{
			var desired = base.MeasureOverride(constraint);
			var sz = Math.Min(desired.Width, desired.Height);
			return sz != double.PositiveInfinity ? new Size(sz, sz) : new Size(256, 256);
		}

		/// <summary>Progress minimum value</summary>
		public double Minimum
		{
			get => (double)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}
		private void Minimum_Changed() => UpdateGfx();
		public static readonly DependencyProperty MinimumProperty = Gui_.DPRegister<RadialProgressControl>(nameof(Minimum), 0.0, Gui_.EDPFlags.None);

		/// <summary>Progress maximum value</summary>
		public double Maximum
		{
			get => (double)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}
		private void Maximum_Changed() => UpdateGfx();
		public static readonly DependencyProperty MaximumProperty = Gui_.DPRegister<RadialProgressControl>(nameof(Maximum), 1.0, Gui_.EDPFlags.None);

		/// <summary>Progress value</summary>
		public double Value
		{
			get => (double)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}
		private void Value_Changed() => UpdateGfx();
		public static readonly DependencyProperty ValueProperty = Gui_.DPRegister<RadialProgressControl>(nameof(Value), 0.5, Gui_.EDPFlags.None);

		/// <summary>Background arc colour</summary>
		public Brush BackArcColor
		{
			get => (Brush)GetValue(BackArcColorProperty);
			set => SetValue(BackArcColorProperty, value);
		}
		private void BackArcColor_Changed() => UpdateGfx();
		public static readonly DependencyProperty BackArcColorProperty = Gui_.DPRegister<RadialProgressControl>(nameof(BackArcColor), new Colour32(0x80A0A0A0).ToMediaBrush(), Gui_.EDPFlags.None);

		/// <summary>Width of the background arc</summary>
		public double? BackArcWidth
		{
			get => (double?)GetValue(BackArcWidthProperty);
			set => SetValue(BackArcWidthProperty, value);
		}
		private void BackArcWidth_Changed() => UpdateGfx();
		public static readonly DependencyProperty BackArcWidthProperty = Gui_.DPRegister<RadialProgressControl>(nameof(BackArcWidth), null, Gui_.EDPFlags.None);

		/// <summary>Glow arc colour</summary>
		public Color? GlowArcColor
		{
			get => (Color?)GetValue(GlowArcColorProperty);
			set => SetValue(GlowArcColorProperty, value);
		}
		private void GlowArcColor_Changed() => UpdateGfx();
		public static readonly DependencyProperty GlowArcColorProperty = Gui_.DPRegister<RadialProgressControl>(nameof(GlowArcColor), null, Gui_.EDPFlags.None);

		/// <summary>Width of the glow arc</summary>
		public double? GlowArcWidth
		{
			get => (double?)GetValue(GlowArcWidthProperty);
			set => SetValue(GlowArcWidthProperty, value);
		}
		private void GlowArcWidth_Changed() => UpdateGfx();
		public static readonly DependencyProperty GlowArcWidthProperty = Gui_.DPRegister<RadialProgressControl>(nameof(GlowArcWidth), null, Gui_.EDPFlags.None);

		/// <summary>Arc colour</summary>
		public Color ArcColor
		{
			get => (Color)GetValue(ArcColorProperty);
			set => SetValue(ArcColorProperty, value);
		}
		private void ArcColor_Changed() => UpdateGfx();
		public static readonly DependencyProperty ArcColorProperty = Gui_.DPRegister<RadialProgressControl>(nameof(ArcColor), new Colour32(0xFFA0C0E0).ToMediaColor(), Gui_.EDPFlags.None);

		/// <summary>Arc width</summary>
		public double ArcWidth
		{
			get => (double)GetValue(ArcWidthProperty);
			set => SetValue(ArcWidthProperty, value);
		}
		private void ArcWidth_Changed() => UpdateGfx();
		public static readonly DependencyProperty ArcWidthProperty = Gui_.DPRegister<RadialProgressControl>(nameof(ArcWidth), 6.0, Gui_.EDPFlags.None);

		/// <summary>Start position angle</summary>
		public double BegAngle
		{
			get => (double)GetValue(BegAngleProperty);
			set => SetValue(BegAngleProperty, value);
		}
		private void BegAngle_Changed() => UpdateGfx();
		public static readonly DependencyProperty BegAngleProperty = Gui_.DPRegister<RadialProgressControl>(nameof(BegAngle), 0.0, Gui_.EDPFlags.None);

		/// <summary>Start position angle</summary>
		public double EndAngle
		{
			get => (double)GetValue(EndAngleProperty);
			set => SetValue(EndAngleProperty, value);
		}
		private void EndAngle_Changed() => UpdateGfx();
		public static readonly DependencyProperty EndAngleProperty = Gui_.DPRegister<RadialProgressControl>(nameof(EndAngle), 0.0, Gui_.EDPFlags.None);

		/// <summary>Show the progress text</summary>
		public ERadialProgressTextFormat TextFormat
		{
			get => (ERadialProgressTextFormat)GetValue(TextFormatProperty);
			set => SetValue(TextFormatProperty, value);
		}
		private void TextFormat_Changed() => UpdateGfx();
		public static readonly DependencyProperty TextFormatProperty = Gui_.DPRegister<RadialProgressControl>(nameof(TextFormat), ERadialProgressTextFormat.None, Gui_.EDPFlags.None);

		/// <summary></summary>
		private void UpdateGfx()
		{
			// The scale is somewhat arbitrary, but pen widths are absolute, so
			// drawing as a unit circle doesn't work because the pen width is 1.
			// We can draw with (0,0) at the centre however.

			var centre = new Point(0, 0);
			var radius = Radius;

			var ang0 = 360.0 + (BegAngle % 360.0);
			var ang1 = 360.0 + (EndAngle % 360.0);
			if (ang1 <= ang0) ang1 += 360.0;
			ang1 = Math_.Clamp(ang1, ang0, ang0 + 359.99);

			ang0 = Math_.DegreesToRadians(ang0 - 90);
			ang1 = Math_.DegreesToRadians(ang1 - 90);

			var value = Math_.Clamp(Value, Minimum, Maximum);
			var frac = Math_.Frac(Minimum, value, Maximum);
			var angv = Math_.Lerp(ang0, ang1, frac);
			var a = centre + radius * new Vector(Math.Cos(ang0), Math.Sin(ang0));
			var b = centre + radius * new Vector(Math.Cos(ang1), Math.Sin(ang1));
			var c = centre + radius * new Vector(Math.Cos(angv), Math.Sin(angv));
			var large_arc1 = ang1 - ang0 > Math_.TauBy2;
			var large_arcv = angv - ang0 > Math_.TauBy2;

			var arc_width = ArcWidth;
			var glo_width = GlowArcWidth ?? arc_width * 1.4;
			var bck_width = BackArcWidth ?? arc_width;

			var arc_colour = ArcColor;
			var glo_colour = GlowArcColor ?? arc_colour.Modify(a:0x40);
			var bck_colour = BackArcColor;

			var drawing = new DrawingGroup();

			// Draw a transparent rectangle so that the control bounds don't change
			{
				var rad = radius + Math_.Max(arc_width, glo_width, bck_width) / 2;
				var pen = new Pen(Brushes.Transparent, 0.0);
				var rect = new RectangleGeometry(new Rect(-rad, -rad, 2 * rad, 2 * rad));
				var geom = new GeometryDrawing(Brushes.Transparent, pen, rect);
				drawing.Children.Add(geom);
			}

			// Fill the circular background
			if (Background != Brushes.Transparent)
			{
				var rad = radius + bck_width / 2;
				var pen = new Pen(Brushes.Transparent, 0.0);
				var bkgd = new GeometryDrawing(Background, pen, new EllipseGeometry(centre, rad, rad));
				drawing.Children.Add(bkgd);
			}

			// Background arc
			if (bck_colour != Brushes.Transparent)
			{
				var fig = new PathFigure { IsFilled = false, IsClosed = false, StartPoint = a };
				fig.Segments.Add(new ArcSegment(b, new Size(radius, radius), 0.0, large_arc1, SweepDirection.Clockwise, true));

				var path = new PathGeometry(new[] { fig });
				var pen = new Pen(bck_colour, bck_width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
				var geom = new GeometryDrawing(Brushes.Transparent, pen, path);
				drawing.Children.Add(geom);
			}

			// Glow arc
			if (glo_colour != Colors.Transparent)
			{
				var fig = new PathFigure { IsFilled = false, IsClosed = false, StartPoint = a };
				fig.Segments.Add(new ArcSegment(c, new Size(radius, radius), 0.0, large_arcv, SweepDirection.Clockwise, true));

				var path = new PathGeometry(new[] { fig });
				var pen = new Pen(new SolidColorBrush(glo_colour), glo_width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
				var geom = new GeometryDrawing(Brushes.Transparent, pen, path);
				drawing.Children.Add(geom);
			}

			// Progress arc
			if (arc_colour != Colors.Transparent)
			{
				var fig = new PathFigure { IsFilled = false, IsClosed = false, StartPoint = a };
				fig.Segments.Add(new ArcSegment(c, new Size(radius, radius), 0.0, large_arcv, SweepDirection.Clockwise, true));

				var path = new PathGeometry(new[] { fig });
				var pen = new Pen(new SolidColorBrush(arc_colour), ArcWidth) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
				var geom = new GeometryDrawing(Brushes.Transparent, pen, path);
				drawing.Children.Add(geom);
			}

			// Text
			if (TextFormat != ERadialProgressTextFormat.None)
			{
				var msg = string.Empty;
				try
				{
					msg = TextFormat switch
					{
						ERadialProgressTextFormat.Percentage => frac.ToString(ContentStringFormat ?? "P0"),
						ERadialProgressTextFormat.Value => Value.ToString(ContentStringFormat ?? "N0"),
						ERadialProgressTextFormat.Ratio => Value.ToString(ContentStringFormat ?? "N0") + "/" + Maximum.ToString(ContentStringFormat ?? "N0"),
						_ => throw new Exception($"Unknown text format: {TextFormat}"),
					};
				}
				catch {}
				var text = new FormattedText(msg, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.Typeface(), FontSize * 5.0, Foreground, 96.0) { TextAlignment = TextAlignment.Center };
				var path = text.BuildGeometry(new Point(0, -radius * 0.35));
				var geom = new GeometryDrawing(Brushes.Black, new Pen(Brushes.Transparent, 0), path);
				drawing.Children.Add(geom);
			}

			m_image.Source = new DrawingImage(drawing).Freeze2();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		private void SetProp<T>(ref T prop, T value, string prop_name)
		{
			if (Equals(prop, value)) return;
			prop = value;
			NotifyPropertyChanged(prop_name);
		}
	}

	public enum ERadialProgressTextFormat
	{
		None,
		Percentage,
		Value,
		Ratio,
	}
}
