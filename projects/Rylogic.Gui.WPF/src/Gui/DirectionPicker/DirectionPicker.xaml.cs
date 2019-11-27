using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public partial class DirectionPicker :UserControl, INotifyPropertyChanged
	{
		public DirectionPicker()
		{
			InitializeComponent();
			m_root.DataContext = this;
		}

		/// <summary>The selected direction vector</summary>
		public v4 Direction
		{
			get => (v4)GetValue(DirectionProperty);
			set => SetValue(DirectionProperty, value);
		}
		private void Direction_Changed()
		{
			NotifyPropertyChanged(nameof(Origin));
		}
		public static readonly DependencyProperty DirectionProperty = Gui_.DPRegister<DirectionPicker>(nameof(Direction));

		/// <summary>The sign of the X component of the direction vector</summary>
		public double ScaleX
		{
			get => (double)GetValue(ScaleXProperty);
			set => SetValue(ScaleXProperty, value);
		}
		private void ScaleX_Changed()
		{
			NotifyPropertyChanged(nameof(Direction));
		}
		public static readonly DependencyProperty ScaleXProperty = Gui_.DPRegister<DirectionPicker>(nameof(ScaleX), +1.0);

		/// <summary>The sign of the Y component of the direction vector</summary>
		public double ScaleY
		{
			get => (double)GetValue(ScaleYProperty);
			set => SetValue(ScaleYProperty, value);
		}
		private void ScaleY_Changed()
		{
			NotifyPropertyChanged(nameof(Direction));
		}
		public static readonly DependencyProperty ScaleYProperty = Gui_.DPRegister<DirectionPicker>(nameof(ScaleY), +1.0);

		/// <summary>The sign of the Z component of the direction vector</summary>
		public double ScaleZ
		{
			get => (double)GetValue(ScaleZProperty);
			set => SetValue(ScaleZProperty, value);
		}
		private void ScaleZ_Changed()
		{
			NotifyPropertyChanged(nameof(Direction));
		}
		public static readonly DependencyProperty ScaleZProperty = Gui_.DPRegister<DirectionPicker>(nameof(ScaleZ), -1.0);

		/// <summary></summary>
		public Point Origin
		{
			get
			{
				var x = 0.5 * (+Direction.x / ScaleX + 1.0);
				var y = 0.5 * (-Direction.y / ScaleY + 1.0);
				return new Point(x, y);
			}
		}

		/// <summary>Colour around the outside of the ellipse</summary>
		public Colour32 PerimeterColour
		{
			get => (Colour32)GetValue(MyPropertyProperty);
			set => SetValue(MyPropertyProperty, value);
		}
		public static readonly DependencyProperty MyPropertyProperty = Gui_.DPRegister<DirectionPicker>(nameof(PerimeterColour), def:new Colour32(0xFF404040));

		/// <summary>True to display the direction vector in a Textbox below the 'dome'</summary>
		public bool ShowTextValue
		{
			get { return (bool)GetValue(ShowTextValueProperty); }
			set { SetValue(ShowTextValueProperty, value); }
		}
		public static readonly DependencyProperty ShowTextValueProperty = Gui_.DPRegister<DirectionPicker>(nameof(ShowTextValue));

		/// <summary></summary>
		private void HandleMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is Ellipse ellipse && e.LeftButton == MouseButtonState.Pressed)
			{
				ellipse.CaptureMouse();
				PointToDirection(ellipse, e.GetPosition(ellipse));
			}
		}
		private void HandleMouseMove(object sender, MouseEventArgs e)
		{
			if (sender is Ellipse ellipse && e.LeftButton == MouseButtonState.Pressed)
				PointToDirection(ellipse, e.GetPosition(ellipse));
		}
		private void HandleMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (sender is Ellipse ellipse)
				ellipse.ReleaseMouseCapture();
		}
		private void PointToDirection(Ellipse ellipse, Point pt)
		{
			var x = -1.0 + 2.0 * (pt.X / ellipse.ActualWidth);
			var y = +1.0 - 2.0 * (pt.Y / ellipse.ActualHeight);

			double z;
			var xy_sq = Math_.Len2Sq(x, y);
			if (xy_sq <= 1.0)
			{
				z = Math.Sqrt(1.0 - xy_sq);
			}
			else
			{
				var xy = Math.Sqrt(xy_sq);
				pt.X /= xy;
				pt.Y /= xy;
				z = 0.0;
			}
			Direction = new v4((float)(ScaleX * x), (float)(ScaleY * y), (float)(ScaleZ * z), 0f);
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
