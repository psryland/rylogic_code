using Rylogic.Maths;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public partial class DockPanelSplitter : Control
	{
		// Code Project article
		// http://www.codeproject.com/KB/WPF/DockPanelSplitter.aspx
		// 
		// CodePlex project
		// http://wpfcontrols.codeplex.com
		//
		// DockPanelSplitter is a splitter control for DockPanels.
		// Add the DockPanelSplitter after the element you want to resize.
		// Set the DockPanel.Dock to define which edge the splitter should work on.

		private double m_width;             // Desired width of the element. Can be less than MinWidth.   Value is normalised if 'ProportionalResize' is true
		private double m_height;            // Desired height of the element. Can be less than MinHeight. Value is normalised if 'ProportionalResize' is true
		private Point m_start_drag_point;

		static DockPanelSplitter()
		{
			// Proportional resize property
			ProportionalResizeProperty = DependencyProperty.Register(nameof(ProportionalResize), typeof(bool), typeof(DockPanelSplitter), new UIPropertyMetadata(true));

			// Thickness property
			ThicknessProperty = DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(DockPanelSplitter), new UIPropertyMetadata(4.0, ThicknessChanged));
			void ThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
			{
				((DockPanelSplitter)d).UpdateHeightOrWidth();
			}

			// Override the default style key property
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DockPanelSplitter), new FrameworkPropertyMetadata(typeof(DockPanelSplitter)));

			// Override the Background property
			BackgroundProperty.OverrideMetadata(typeof(DockPanelSplitter), new FrameworkPropertyMetadata(Brushes.Transparent));

			// Override the Dock property to get notifications when Dock is changed
			DockPanel.DockProperty.OverrideMetadata(typeof(DockPanelSplitter), new FrameworkPropertyMetadata(Dock.Left, new PropertyChangedCallback(DockChanged)));
			void DockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
			{
				((DockPanelSplitter)d).UpdateHeightOrWidth();
			}
		}
		public DockPanelSplitter()
		{
			InitializeComponent();

			Loaded += DockPanelSplitterLoaded;
			Unloaded += DockPanelSplitterUnloaded;

			m_width = 0.25;
			m_height = 0.25;
			ProportionalResize = true;
			UpdateHeightOrWidth();

			void DockPanelSplitterLoaded(object sender, RoutedEventArgs e)
			{
				if (!(Parent is Panel dp))
					return;

				// Subscribe to the parent's size changed event
				dp.SizeChanged += ParentSizeChanged;

				// Find the target element
				var target = GetTargetElement();
				if (target != null)
				{
					// Record the initial sizes
					RecordCurrentSize(target);
				}
			}
			void DockPanelSplitterUnloaded(object sender, RoutedEventArgs e)
			{
				if (!(Parent is Panel dp))
					return;

				// Unsubscribe
				dp.SizeChanged -= ParentSizeChanged;
			}
			void ParentSizeChanged(object sender, SizeChangedEventArgs e)
			{
				if (Parent is DockPanel dp && ProportionalResize)
				{
					var target = GetTargetElement();
					if (target != null)
					{
						switch (Orientation)
						{
						case Orientation.Horizontal:
							SetTargetHeight(target, m_height * dp.ActualHeight);
							break;
						case Orientation.Vertical:
							SetTargetWidth(target, m_width * dp.ActualWidth);
							break;
						}
					}
				}
			}
		}
		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			if (!IsEnabled) return;
			Cursor =
				Orientation == Orientation.Horizontal ? Cursors.SizeNS :
				Orientation == Orientation.Vertical ? Cursors.SizeWE :
				Cursors.SizeAll;
		}
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (!IsEnabled) return;
			if (!IsMouseCaptured)
			{
				m_start_drag_point = e.GetPosition(Parent as IInputElement);
				m_target = GetTargetElement();
				if (m_target != null)
				{
					RecordCurrentSize(m_target);
					CaptureMouse();
				}
			}

			base.OnMouseDown(e);
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (IsMouseCaptured)
			{
				var pt = e.GetPosition(Parent as IInputElement);
				var delta = new Point(pt.X - m_start_drag_point.X, pt.Y - m_start_drag_point.Y);
				var dock = DockPanel.GetDock(this);

				switch (Orientation) {
				case Orientation.Horizontal: delta.Y = AdjustHeight(m_target, delta.Y, dock); break;
				case Orientation.Vertical: delta.X = AdjustWidth(m_target, delta.X, dock); break;
				}

				// When docked to the bottom or right, the position has changed after adjusting the size
				var isBottomOrRight = (dock == Dock.Right || dock == Dock.Bottom);
				if (isBottomOrRight)
					m_start_drag_point = e.GetPosition(Parent as IInputElement);
				else
					m_start_drag_point = new Point(m_start_drag_point.X + delta.X, m_start_drag_point.Y + delta.Y);
			}

			base.OnMouseMove(e);
		}
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (IsMouseCaptured)
				ReleaseMouseCapture();

			m_target = null;
			base.OnMouseUp(e);
		}
		private FrameworkElement m_target;

		/// <summary>
		/// Resize the target element proportionally with the parent container.
		/// Set to false if you don't want the element to be resized when the parent is resized.</summary>
		public bool ProportionalResize
		{
			get { return (bool)GetValue(ProportionalResizeProperty); }
			set
			{
				if (ProportionalResize == value) return;
				SetValue(ProportionalResizeProperty, value);
				var target = GetTargetElement();
				if (target != null)
					RecordCurrentSize(target);
			}
		}
		public static readonly DependencyProperty ProportionalResizeProperty;

		/// <summary>Height or width of splitter, depends on orientation of the splitter</summary>
		public double Thickness
		{
			get { return (double)GetValue(ThicknessProperty); }
			set { SetValue(ThicknessProperty, value); }
		}
		public static readonly DependencyProperty ThicknessProperty;

		/// <summary>Update the thickness of the splitter</summary>
		private void UpdateHeightOrWidth()
		{
			switch (Orientation)
			{
			case Orientation.Horizontal:
				{
					Height = Thickness;
					Width = double.NaN;
					break;
				}
			case Orientation.Vertical:
				{
					Width = Thickness;
					Height = double.NaN;
					break;
				}
			}
		}

		/// <summary>Get the splitter orientation</summary>
		public Orientation Orientation
		{
			get
			{
				var dock = DockPanel.GetDock(this);
				return
					dock == Dock.Top || dock == Dock.Bottom ? Orientation.Horizontal :
					dock == Dock.Left || dock == Dock.Right ? Orientation.Vertical :
					throw new Exception("Orientation undefined for dock location");
			}
		}

		/// <summary>Update the target element (the element the DockPanelSplitter works on)</summary>
		private FrameworkElement GetTargetElement()
		{
			if (Parent is DockPanel dp && dp.Children.Count != 0)
			{
				// Get the index in the child collection
				// The splitter cannot be the first child of the parent DockPanel
				// The splitter works on the 'previous' sibling 
				var i = dp.Children.IndexOf(this);
				if (i > 0)
					return dp.Children[i - 1] as FrameworkElement;
			}
			return null;
	}

		/// <summary>Record the current width/height of 'target'</summary>
		private void RecordCurrentSize(FrameworkElement target)
		{
			if (!ProportionalResize && target != null)
			{
				m_width = target.ActualWidth;
				m_height = target.ActualHeight;
			}
			else if (Parent is DockPanel dp && target != null)
			{
				m_width = target.ActualWidth / dp.ActualWidth;
				m_height = target.ActualHeight / dp.ActualHeight;
			}
			else
			{
				m_width = 0.25;
				m_height = 0.25;
			}
		}

		/// <summary>Set the width of the target element</summary>
		private void SetTargetWidth(FrameworkElement target, double width)
		{
			// Update the width of the target element
			target.Width = Math_.Clamp(width, target.MinWidth, target.MaxWidth); ;
		}

		/// <summary>Set the height of the target element</summary>
		private void SetTargetHeight(FrameworkElement target, double height)
		{
			// Update the height of the target element
			target.Height = Math_.Clamp(height, target.MinHeight, target.MaxHeight);
		}

		/// <summary>Change the target width by 'dx'</summary>
		private double AdjustWidth(FrameworkElement target, double dx, Dock dock)
		{
			if (dock == Dock.Right)
				dx = -dx;

			if (ProportionalResize && Parent is DockPanel dp)
			{
				m_width = (m_width * dp.ActualWidth + dx) / dp.ActualWidth;
				SetTargetWidth(target, m_width * dp.ActualWidth);
			}
			else
			{
				m_width += dx;
				SetTargetWidth(target, m_width);
			}

			return dx;
		}

		/// <summary>Change the target height by 'dy'</summary>
		private double AdjustHeight(FrameworkElement target, double dy, Dock dock)
		{
			if (dock == Dock.Bottom)
				dy = -dy;

			if (ProportionalResize && Parent is DockPanel dp)
			{
				m_height = (m_height * dp.ActualHeight + dy) / dp.ActualHeight;
				SetTargetHeight(target, m_height * dp.ActualHeight);
			}
			else
			{
				m_height += dy;
				SetTargetHeight(target, m_height);
			}

			return dy;
		}
	}
}
