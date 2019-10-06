using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public class ToolBar : System.Windows.Controls.ToolBar
	{
		private Dictionary<Control, double> m_overflow_controls;
		private ToolBarOverflowPanel? m_overflow_panel;
		private bool m_suppress_overflow_corrections;
		private bool m_overflow_control_width_changed;

		public ToolBar()
		{
			m_overflow_controls = new Dictionary<Control, double>();
		}
		protected override Size MeasureOverride(Size constraint)
		{
			if (m_overflow_control_width_changed)
			{
				IsOverflowOpen = m_suppress_overflow_corrections = true;
				foreach (var cachedItem in m_overflow_controls)
				{
					var control = cachedItem.Key;
					var width = cachedItem.Value;
					control.Width = width;
				}
				IsOverflowOpen = m_suppress_overflow_corrections = false;
				m_overflow_control_width_changed = false;
			}

			return base.MeasureOverride(constraint);
		}
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Set the background of the overflow panel to the same as the tool bar
			//if (OverflowPanelBackground == null)
			OverflowPanelBackground = Background;

			// Attach on loaded handler
			if (ToolBarOverflowPanel != m_overflow_panel)
			{
				if (m_overflow_panel != null)
					m_overflow_panel.Loaded -= ToolBarOverflowPanelOnLoadedHandler;
				m_overflow_panel = ToolBarOverflowPanel;
				if (m_overflow_panel != null)
				{
					m_overflow_panel.Loaded += ToolBarOverflowPanelOnLoadedHandler;
					m_overflow_panel.Background = Background;
				}

				// Handler
				void ToolBarOverflowPanelOnLoadedHandler(object sender, RoutedEventArgs e)
				{
					if (m_suppress_overflow_corrections || m_overflow_panel == null)
						return;

					var width = GetMaxChildWidth(m_overflow_panel);
					if (width > 0)
					{
						foreach (var child in m_overflow_panel.Children)
						{
							if (child is Control control)
							{
								if (!m_overflow_controls.ContainsKey(control))
								{
									m_overflow_controls[control] = control.Width;
								}
								if (control.ActualWidth != width)
								{
									control.Width = width;
									m_overflow_control_width_changed = true;
								}
							}
						}
					}
				}
			}

			// Set the background colour of the border
			if (m_overflow_panel != null)
			{
				if (m_overflow_panel.Parent is Border border)
				{
					border.Background = OverflowPanelBackground;
				}
				else
				{
					m_overflow_panel.Background = OverflowPanelBackground;
					m_overflow_panel.Margin = new Thickness(0);
				}
			}

			// Set the overflow button background to match the tool bar background
			var ovr = OverflowButton;
			if (ovr != null)
			{
				ovr.Background = Background;
			}
		}

		/// <summary>Overflow panel background color</summary>
		public Brush OverflowPanelBackground
		{
			get { return (Brush)GetValue(OverflowPanelBackgroundProperty); }
			set { SetValue(OverflowPanelBackgroundProperty, value); }
		}
		public static readonly DependencyProperty OverflowPanelBackgroundProperty = DependencyProperty.Register(nameof(OverflowPanelBackground), typeof(Brush), typeof(ToolBar), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>Overflow button visibility</summary>
		public Visibility OverflowButtonVisibility
		{
			get { return (Visibility)GetValue(OverflowButtonVisibilityProperty); }
			set { SetValue(OverflowButtonVisibilityProperty, value); }
		}
		public static readonly DependencyProperty OverflowButtonVisibilityProperty = DependencyProperty.Register(nameof(OverflowButtonVisibility), typeof(Visibility), typeof(ToolBar), new FrameworkPropertyMetadata(Visibility.Visible, FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>Access the overflow panel</summary>
		protected ToolBarOverflowPanel? ToolBarOverflowPanel => GetTemplateChild("PART_ToolBarOverflowPanel") as ToolBarOverflowPanel;

		/// <summary>Access the tool bar panel</summary>
		protected ToolBarPanel? ToolBarPanel => GetTemplateChild("PART_ToolBarPanel") as ToolBarPanel;

		/// <summary>Access the gripper</summary>
		protected Thumb? Gripper => GetTemplateChild("ToolBarThumb") as Thumb;

		/// <summary>Access the overflow button</summary>
		protected ToggleButton? OverflowButton => GetTemplateChild("OverflowButton") as ToggleButton;

		/// <summary>Return the maximum width of a child within 'panel'</summary>
		private static double GetMaxChildWidth(Panel panel)
		{
			var width = 0.0;
			foreach (var child in panel.Children)
			{
				if (child is Control control && control.ActualWidth > width)
					width = control.ActualWidth;
			}
			return width;
		}
	}
}