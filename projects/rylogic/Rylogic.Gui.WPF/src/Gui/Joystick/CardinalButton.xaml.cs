using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public partial class CardinalButton :Button, INotifyPropertyChanged
	{
		// Usage:
		//  Create a 'Command' binding like this, the command parameter is a vector in the direction clicked:
		//   public ICommand MovePosition { get; }
		//   private void MovePositionInternal(object? value)
		//   {
		//       if (value is Vector dir)
		//           m_position += dir;

		//       NotifyPropertyChanged(nameof(Position));
		//   }

		public CardinalButton()
		{
			InitializeComponent();
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);

			var w = ActualWidth;
			var h = ActualHeight;
			var pts = new Point[]
			{
				new Point(0.50 * w, 0.00 * h),
				new Point(0.75 * w, 0.24 * h),
				new Point(0.75 * w, 0.25 * h),
				new Point(0.61 * w, 0.25 * h),
				new Point(0.61 * w, 0.39 * h),
				new Point(0.39 * w, 0.39 * h),
				new Point(0.39 * w, 0.25 * h),
				new Point(0.25 * w, 0.25 * h),
				new Point(0.25 * w, 0.24 * h),
				new Point(0.50 * w, 0.00 * h),
			};
			var xfm = new Transform[]
			{
				Transform.Identity,
				new RotateTransform( 90.0, 0.5 * w, 0.5 * h),
				new RotateTransform(180.0, 0.5 * w, 0.5 * h),
				new RotateTransform(270.0, 0.5 * w, 0.5 * h),
			};
			
			// Update the clip area
			var clip = new PathGeometry();
			for (int i = 0; i != 4; ++i)
			{
				var fig = new PathFigure { StartPoint = xfm[i].Transform(pts[0]) };
				foreach (var pt in pts)
					fig.Segments.Add(new LineSegment(xfm[i].Transform(pt), false));
				
				clip.Figures.Add(fig);
			}
			
			Clip = clip;
		}

		///// <summary>Command executed on clicked</summary>
		//public ICommand? Command
		//{
		//	get => (ICommand?)GetValue(CommandProperty);
		//	set => SetValue(CommandProperty, value);
		//}
		//public static readonly DependencyProperty CommandProperty = Gui_.DPRegister<CardinalButton>(nameof(Command), null, Gui_.EDPFlags.None);

		/// <summary></summary>
		public Vector Direction
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(Direction));
			}
		}

		/// <summary>Handle mouse clicks</summary>
		private void HandleMouseDown(object sender, MouseButtonEventArgs e)
		{
			// Determine the region that the mouse is in and set the command parameter to the direction
			var pt = e.GetPosition(this);
			pt -= new Vector(ActualWidth/2, ActualHeight/2);

			if (pt.X < -Math.Abs(pt.Y)) Direction = new Vector(-1, 0);
			if (pt.X > +Math.Abs(pt.Y)) Direction = new Vector(+1, 0);
			if (pt.Y < -Math.Abs(pt.X)) Direction = new Vector(0, +1);
			if (pt.Y > +Math.Abs(pt.X)) Direction = new Vector(0, -1);

			// Execute the command when clicked
			if (Command != null && Command.CanExecute(Direction))
				Command.Execute(Direction);

			e.Handled = true;
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
