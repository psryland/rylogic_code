using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Rylogic.Gui.WPF.TextEditor;

namespace Rylogic.Gui.WPF
{
	internal sealed class Layer_Caret :Layer
	{
		public Layer_Caret(TextView text_view, Caret caret)
			: base(EType.Caret, text_view)
		{
			Caret = caret;
			IsHitTestVisible = false;
		}
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			if (m_visible && m_blink_state)
			{
				var r = new Rect(
					m_caret_rect.X - TextView.HorizontalOffset,
					m_caret_rect.Y - TextView.VerticalOffset,
					m_caret_rect.Width, m_caret_rect.Height);
				dc.DrawRectangle(Caret.Colour, null, PixelSnap.Round(r, PixelSnap.PixelSize(this)));
			}
		}

		/// <summary>The caret</summary>
		public Caret Caret { get; }

		/// <summary>Enable/Disable caret blinking</summary>
		public bool EnableBlink
		{
			get => m_blink_timer != null;
			set
			{
				// Notes:
				//  - If blinking is disabled at a system level, the BlinkPeriod will be negative
				if (EnableBlink == value) return;
				if (m_blink_timer != null)
				{
					m_blink_timer.Stop();
				}
				m_blink_state = value;
				m_blink_timer = value && Caret.BlinkPeriod.TotalMilliseconds > 0 ? new DispatcherTimer(Caret.BlinkPeriod, DispatcherPriority.Background, HandleTick, Dispatcher) : null;
				if (m_blink_timer != null)
				{
					m_blink_timer.Start();
				}

				// Handler
				void HandleTick(object? sender, EventArgs e)
				{
					m_blink_state = !m_blink_state;
					InvalidateVisual();
				}
			}
		}
		private DispatcherTimer? m_blink_timer;
		private bool m_blink_state;

		/// <summary>Show/Hide the caret</summary>
		public bool Visible
		{
			get => m_visible;
			set
			{
				if (Visible == value) return;
				m_visible = value;
				EnableBlink = value;
				InvalidateVisual();
			}
		}
		private bool m_visible;

		/// <summary>Show the caret at the given location</summary>
		public void Show(Rect caret_rect)
		{
			m_caret_rect = caret_rect;
			Visible = true;
		}
		private Rect m_caret_rect;
	}
}