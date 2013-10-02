//***************************************************
// Copyright © Rylogic Ltd 2013
//***************************************************
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace pr.gui
{
	/// <summary>A panel that covers a form and contains a bitmap of the form contents</summary>
	public sealed class Overlay :Panel
	{
		/// <summary>The form being overlaid</summary>
		private Form m_attachee;

		/// <summary>A snapshot of the form when it was last attached or resized</summary>
		public Bitmap Bitmap { get { return m_bm; } }
		private Bitmap m_bm;

		/// <summary>The dx,dy from the window top/left to its client area top/left</summary>
		private Point m_client_offset;

		public Overlay()
		{
			Dock = DockStyle.Fill;
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Attachee = null;
		}

		/// <summary>Called whenever a snapshot of the attachee form is taken</summary>
		public event EventHandler<SnapShotArgs> SnapShotCaptured;
		public class SnapShotArgs :EventArgs
		{
			/// <summary>
			/// A graphics object for drawing on the snapshot.
			/// Transformed so that 0,0 is the top/left in client space for the attached form</summary>
			public Graphics Gfx { get; private set; }

			/// <summary>The area of the snapshot</summary>
			public Rectangle ClientRectangle { get; private set; }

			public SnapShotArgs(Bitmap snapshot, Point client_offset, Rectangle client_area)
			{
				Gfx                    = Graphics.FromImage(snapshot);
				Gfx.SmoothingMode      = SmoothingMode.HighQuality;
				Gfx.CompositingMode    = CompositingMode.SourceCopy;
				Gfx.CompositingQuality = CompositingQuality.GammaCorrected;
				ClientRectangle        = client_area;
				Gfx.TranslateTransform(-client_offset.X, -client_offset.Y);
				Gfx.SetClip(client_area);
			}
		}
		private void RaiseSnapShotCaptured()
		{
			if (SnapShotCaptured == null) return;
			SnapShotCaptured(this, new SnapShotArgs(m_bm, m_client_offset, m_attachee.ClientRectangle));
		}

		/// <summary>Get/Set the form on which to display this overlay</summary>
		public Form Attachee
		{
			get { return m_attachee; }
			set
			{
				if (value != m_attachee)
				{
					if (m_attachee != null)
					{
						m_attachee.Controls.Remove(this);
						m_attachee = null;
					}
					if (value != null)
					{
						m_attachee = value;
						m_attachee.Controls.Add(this);
						m_client_offset = m_attachee.Location - new Size(m_attachee.PointToScreen(Point.Empty));
						BringToFront();
					}
				}
				SnapShot();
			}
		}

		/// <summary>Records the state of the form into a bitmap</summary>
		public void SnapShot()
		{
			if (m_attachee == null)
				return;

			// Hide this panel while we capture the form to the bitmap
			Visible = false;
			//m_attachee.Refresh();

			// Create a bitmap big enough to draw the whole window in
			var rect = new Rectangle(0, 0, m_attachee.Width, m_attachee.Height);
			if (m_bm == null || m_bm.Width < rect.Width || m_bm.Height < rect.Height)
				m_bm = new Bitmap(rect.Width * 2, rect.Height * 2);

			// Capture the form into the bitmap
			m_attachee.DrawToBitmap(m_bm, rect);
			RaiseSnapShotCaptured();
			//m_bm.Save(@"D:\deleteme\form.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

			Visible = true;
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
		}
		protected override void OnResize(EventArgs eventargs)
		{
			base.OnResize(eventargs);
			SnapShot();
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.DrawImageUnscaled(m_bm, m_client_offset);
		}
	}
}
