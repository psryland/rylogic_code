using System;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Interop.Win32;

namespace Rylogic.Gui.WinForms
{
	/// <summary>Provides resizing to a control</summary>
	public class BorderResizer :IDisposable ,IMessageFilter
	{
		private EBoxZone m_mask;
		private Point?   m_grab;
		private Size     m_size;
		private Point    m_loc;

		public BorderResizer()
		{ }
		public BorderResizer(Control target, int border_width)
		{
			Target      = target;
			BorderWidth = border_width;
		}
		public virtual void Dispose()
		{
			Target = null;
		}

		/// <summary>The control being resized</summary>
		public Control Target
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					Application.RemoveMessageFilter(this);
				}
				field = value;
				if (field != null)
				{
					Application.AddMessageFilter(this);
				}
			}
		}

		/// <summary>The size of the region around the edge of the control that triggers resizing</summary>
		public int BorderWidth
		{
			get; set;
		}

		/// <summary>Handle mouse messages over 'Target' and perform resizing</summary>
		public bool PreFilterMessage(ref Message m)
		{
			if (m.HWnd == Target.Handle || Win32.IsChild(Target.Handle, m.HWnd))
			{
				switch (m.Msg)
				{
				case Win32.WM_LBUTTONDOWN:
					#region
					{
						var pt = Control.MousePosition;
						m_mask = Mask(pt);
						if (m_mask != EBoxZone.None)
						{
							Target.Cursor = m_mask.ToCursor();
							m_grab = pt;
							m_size = Target.Size;
							m_loc  = Target.Location;
							Target.Capture = true;
							return true;
						}
						break;
					}
					#endregion
				case Win32.WM_MOUSEMOVE:
					#region
					{
						var pt = Control.MousePosition;
						if (m_grab != null)
						{
							var mn = Target.MinimumSize;
							var mx = Target.MaximumSize != Size.Empty ? Target.MaximumSize : new Size(int.MaxValue, int.MaxValue);
							var delta = Point_.Subtract(pt, m_grab.Value);
							if ((m_mask & EBoxZone.Right)  != 0) { Target.Width  = Math_.Clamp(m_size.Width  + delta.Width , mn.Width , mx.Width ); }
							if ((m_mask & EBoxZone.Bottom) != 0) { Target.Height = Math_.Clamp(m_size.Height + delta.Height, mn.Height, mx.Height); }
							if ((m_mask & EBoxZone.Left)   != 0) { Target.Width  = Math_.Clamp(m_size.Width  - delta.Width , mn.Width , mx.Width ); Target.Left = m_loc.X + m_size.Width  - Target.Width;  }
							if ((m_mask & EBoxZone.Top )   != 0) { Target.Height = Math_.Clamp(m_size.Height - delta.Height, mn.Height, mx.Height); Target.Top  = m_loc.Y + m_size.Height - Target.Height; }
							return true;
						}
						else if (!Win32.IsChild(Target.Handle, m.HWnd))
						{
							var mask = Mask(pt);
							if (mask != m_mask)
							{
								Target.Cursor = mask.ToCursor();
								m_mask = mask;
							}
						}
						else
						{
							Target.Cursor = Cursors.Default;
						}
						break;
					}
					#endregion
				case Win32.WM_LBUTTONUP:
					#region
					{
						if (m_grab != null)
						{
							m_grab = null;
							Target.Capture = false;
							return true;
						}
						break;
					}
					#endregion
				case Win32.WM_MOUSELEAVE:
					#region
					{
						m_mask = EBoxZone.None;
						break;
					}
					#endregion
				}
			}
			return false;
		}

		/// <summary>Return the zone that the mouse is in</summary>
		private EBoxZone Mask(Point screen_pt)
		{
			var pt = Target.PointToClient(screen_pt);
			var rect = Target.ClientRectangle.Inflated(-BorderWidth,-BorderWidth,-BorderWidth,-BorderWidth);
			return rect.GetBoxZone(pt);
		}
	}
}
