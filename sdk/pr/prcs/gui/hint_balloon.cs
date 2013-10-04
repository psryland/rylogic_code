using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using pr.extn;
using pr.maths;
using pr.util;

namespace pr.gui
{
	/// <summary>A tooltip form that doesn't suck</summary>
	public class HintBalloon :Form
	{
		private readonly object m_lock;
		private GraphicsPath m_path;
		private RichTextBox m_msg;
		private int m_corner;           // The corner that the tip currently points to
		private int m_issue;

		public HintBalloon()
		{
			m_lock             = new object();
			m_corner           = -1;
			m_issue            = 0;
			ShowDelay          = 500;
			Duration           = 2000;
			FadeDuration       = 500;
			CornerRadius       = 10;
			TipBaseWidth       = 20;
			TipLength          = 20;
			PreferredTipCorner = ETipCorner.TopLeft;
			Visible            = false;

			InitializeComponent();

			SetStyle(ControlStyles.AllPaintingInWmPaint         , true);
			SetStyle(ControlStyles.ResizeRedraw                 , true);
			SetStyle(ControlStyles.UserPaint                    , true);

			m_msg.GotFocus += (s,a) => Win32.HideCaret(m_msg.Handle);
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>The period to wait before displaying the hint (in milliseconds)</summary>
		public int ShowDelay { get; set; }

		/// <summary>How long to display the hint for (in milliseconds)</summary>
		public int Duration { get; set; }

		/// <summary>The time to take when fading out (in milliseconds)</summary>
		public int FadeDuration { get; set; }

		/// <summary>The text to display in the hint</summary>
		public override string Text
		{
			get { return m_msg != null ? m_msg.Text : string.Empty; }
			set { if (m_msg != null) m_msg.Text = value; }
		}

		/// <summary>The hint text in rtf</summary>
		public string TextRtf
		{
			get { return m_msg != null ? m_msg.Rtf : string.Empty; }
			set { if (m_msg != null) m_msg.Rtf = value; }
		}

		/// <summary>The (optional) control that the balloon is pinned to</summary>
		public Control PinTo { get; set; }

		/// <summary>The position to point the tool tip at (relative to the associated control)</summary>
		public Point Target { get; set; }

		/// <summary>The radius of the corners of the balloon</summary>
		public int CornerRadius { get; set; }

		/// <summary>The width of the fat end of the pointer</summary>
		public int TipBaseWidth { get; set; }

		/// <summary>The vertical length of the tip</summary>
		public int TipLength { get; set; }

		/// <summary>The preferred orientation of the balloon tip</summary>
		public ETipCorner PreferredTipCorner { get; set; }
		[Flags] public enum ETipCorner { TopLeft = 0, TopRight = 1, BottomLeft = 2, BottomRight = 3 }

		/// <summary>Display the hint balloon</summary>
		public new void Show()
		{
			Show(null, Target);
		}
		public void Show(Control pin_to, Point target)
		{
			Show(pin_to, target, Text);
		}
		public void Show(Control pin_to, Point target, string msg)
		{
			Show(pin_to, target, msg, Duration);
		}
		public void Show(Control pin_to, Point target, string msg, int duration)
		{
			lock (m_lock)
			{
				var issue = ++m_issue;
				Target = target;
				Text = msg;
				Duration = duration;
				this.BeginInvokeDelayed(ShowDelay, () => ShowHintInternal(issue, pin_to));
			}
		}

		/// <summary>Make the hint balloon visible</summary>
		private void ShowHintInternal(int issue, Control pin_to)
		{
			lock (m_lock)
			{
				// Only display if the issue number is still valid
				if (issue != m_issue)
					return;

				if (PinTo != null)
				{
					PinTo.Move   -= UpdateBalloonLocation;
					PinTo.Resize -= UpdateBalloonLocation;
				}
				if (Owner != null)
				{
					Owner.Move       -= UpdateBalloonLocation;
					Owner.Resize     -= UpdateBalloonLocation;
					Owner.FormClosed -= DetachFromOwner;
				}

				Opacity  = 1.0;
				Owner    = pin_to != null ? pin_to.TopLevelControl as Form : null;
				PinTo    = pin_to;

				if (PinTo != null)
				{
					PinTo.Move   += UpdateBalloonLocation;
					PinTo.Resize += UpdateBalloonLocation;
				}
				if (Owner != null)
				{
					Owner.Move       += UpdateBalloonLocation;
					Owner.Resize     += UpdateBalloonLocation;
					Owner.FormClosed += DetachFromOwner;
				}

				// Determine the best size for the balloon
				var size = new Size(640,480);
				double best_aspect_diff = double.MaxValue;
				for (var w = 60; w < 640; w += 40)
				{
					var sz = m_msg.GetPreferredSize(new Size(w, 0));
					var aspect = sz.Height != 0 ? (double)sz.Width / sz.Height : double.MaxValue;
					var aspect_diff = Math.Abs(3.0 - aspect);
					if (aspect_diff < best_aspect_diff)
					{
						best_aspect_diff = aspect_diff;
						size = sz;
					}
				}
				var margin = TipLength + CornerRadius;
				m_msg.Bounds = new Rectangle(margin, margin, size.Width, size.Height);
				Size = new Size(size.Width + 2*margin, size.Height + 2*margin);

				// Generate the boundary of the hint balloon and set its screen position
				m_corner = -1;
				UpdateBalloonLocation();
				Visible = true;

				this.BeginInvokeDelayed(Duration, () => HideHintInternal(issue));
			}
		}

		/// <summary>Hide the hint balloon</summary>
		private void HideHintInternal(int issue)
		{
			lock (m_lock)
			{
				// Only hide if the issue number is still valid
				if (issue != m_issue || !Visible)
					return;

				// Test if the mouse is over the form, if so, wait another 2000
				var pt = PointToClient(Cursor.Position);
				var rect = ClientRectangle.Inflated(-TipLength,-TipLength);
				if (rect.Contains(pt))
				{
					Opacity = 1f;
					this.BeginInvokeDelayed(2000, () => HideHintInternal(issue));
					return;
				}

				// Do fading
				if (FadeDuration > 0)
				{
					Opacity -= Math.Min(0.1, Opacity);
					if (Opacity == 0)
						Visible = false;
					else
						this.BeginInvokeDelayed(FadeDuration / 10, () => HideHintInternal(issue));
					return;
				}

				// Done, hidden
				DetachFromOwner();
			}
		}

		/// <summary>Returns the target point in screen space</summary>
		private Point TargetInScreenSpace
		{
			get { return PinTo != null ? PinTo.PointToScreen(Target) : Target; }
		}

		/// <summary>Sets the position of the balloon given the target point relative to the screen. Returns the tip corner</summary>
		private int SetLocation()
		{
			var pt = TargetInScreenSpace;
			var screen = Screen.FromPoint(pt);
			var loc = pt;
			int corner = (int)PreferredTipCorner;
			if ((corner & 1) != 0) loc.X -= Width;
			if ((corner & 2) != 0) loc.Y -= Height;
			if ((corner & 1) != 0 && loc.X          < screen.WorkingArea.Left  ) { loc.X += Width ; corner &= ~1; }
			if ((corner & 2) != 0 && loc.Y          < screen.WorkingArea.Top   ) { loc.Y += Height; corner &= ~2; }
			if ((corner & 1) == 0 && loc.X + Width  > screen.WorkingArea.Right ) { loc.X -= Width ; corner |=  1; }
			if ((corner & 2) == 0 && loc.Y + Height > screen.WorkingArea.Bottom) { loc.Y -= Height; corner |=  2; }
			Location = loc;

			return corner;
		}

		/// <summary>Generates the boundary of the hint balloon</summary>
		private GraphicsPath GeneratePath(bool region_border)
		{
			var width  = Width  + (region_border ? 1 : 0);
			var height = Height + (region_border ? 1 : 0);
			var tip_length = TipLength;
			var tip_width = Maths.Clamp(width - 2 * (tip_length + CornerRadius), 5, TipBaseWidth);

			var path = new GraphicsPath();

			// Find the corner to start from
			switch (m_corner)
			{
			default:
				Debug.Assert(false, "Unknown corner");
				break;
			case 0: // top left
				path.AddLine(0, 0, tip_length + CornerRadius + tip_width, tip_length);
				path.AddArc(width - tip_length - CornerRadius, tip_length, CornerRadius, CornerRadius, 270f, 90f);
				path.AddArc(width - tip_length - CornerRadius, height - tip_length - CornerRadius, CornerRadius, CornerRadius, 0f, 90f);
				path.AddArc(tip_length, height - tip_length - CornerRadius, CornerRadius, CornerRadius, 90f, 90f);
				path.AddArc(tip_length, tip_length, CornerRadius, CornerRadius, 180f, 90f);
				path.AddLine(tip_length + CornerRadius, tip_length, 0, 0);
				break;
			case 1: // top right
				path.AddLine(width, 0, width - tip_length - CornerRadius, tip_length);
				path.AddArc(width - tip_length - CornerRadius, tip_length, CornerRadius, CornerRadius, 270f, 90f);
				path.AddArc(width - tip_length - CornerRadius, height - tip_length - CornerRadius, CornerRadius, CornerRadius, 0f, 90f);
				path.AddArc(tip_length, height - tip_length - CornerRadius, CornerRadius, CornerRadius, 90f, 90f);
				path.AddArc(tip_length, tip_length, CornerRadius, CornerRadius, 180f, 90f);
				path.AddLine(tip_length + CornerRadius, tip_length, width - tip_length - CornerRadius - tip_width, tip_length);
				path.AddLine(width - tip_length - CornerRadius - tip_width, tip_length, width, 0);
				break;
			case 2: // bottom left
				path.AddLine(0, height, tip_length + CornerRadius, height - tip_length);
				path.AddArc(tip_length, height - tip_length - CornerRadius, CornerRadius, CornerRadius, 90f, 90f);
				path.AddArc(tip_length, tip_length, CornerRadius, CornerRadius, 180f, 90f);
				path.AddArc(width - tip_length - CornerRadius, tip_length, CornerRadius, CornerRadius, 270f, 90f);
				path.AddArc(width - tip_length - CornerRadius, height - tip_length - CornerRadius, CornerRadius, CornerRadius, 0f, 90f);
				path.AddLine(width - tip_length - CornerRadius, height - tip_length, tip_length + CornerRadius + tip_width, height - tip_length);
				path.AddLine(tip_length + CornerRadius + tip_width, height - tip_length, 0, height);
				break;
			case 3: // bottom right
				path.AddLine(width, height, width - tip_length - CornerRadius - tip_width, height - tip_length);
				path.AddArc(tip_length, height - tip_length - CornerRadius, CornerRadius, CornerRadius, 90f, 90f);
				path.AddArc(tip_length, tip_length, CornerRadius, CornerRadius, 180f, 90f);
				path.AddArc(width - tip_length - CornerRadius, tip_length, CornerRadius, CornerRadius, 270f, 90f);
				path.AddArc(width - tip_length - CornerRadius, height - tip_length - CornerRadius, CornerRadius, CornerRadius, 0f, 90f);
				path.AddLine(width - tip_length - CornerRadius, height - tip_length, width, height);
				break;
			}
			return path;
		}

		/// <summary>When the parent control moves, follow it</summary>
		private void UpdateBalloonLocation(object sender = null, EventArgs e = null)
		{
			var corner = SetLocation();
			if (corner != m_corner)
			{
				m_corner = corner;
				m_path = GeneratePath(false);
				Region = new Region(GeneratePath(true));
				Invalidate();
			}
		}

		/// <summary>Called when the owner is closed</summary>
		private void DetachFromOwner(object sender = null, EventArgs e = null)
		{
			// Immediately hide the balloon. This method should only be called
			// when it is still attached to the owner or from within a locked
			// section (the owner is only changed in a locked section). Therefore
			// we don't need to check the issue number.
			lock (m_lock)
			{
				Owner = null;
				Visible = false;
			}
		}

		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				cp.ClassStyle = Win32.CS_DROPSHADOW;
				return cp;
			}
		}
		protected override bool ShowWithoutActivation
		{
			get { return true; }
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the balloon and outline
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.FillRectangle(SystemBrushes.Info, ClientRectangle);
			e.Graphics.DrawPath(SystemPens.ActiveBorder, m_path);
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			UpdateBalloonLocation();
		}
		#region Windows Form Designer generated code
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.m_msg = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			//
			// m_msg
			//
			this.m_msg.BackColor = System.Drawing.SystemColors.Info;
			this.m_msg.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_msg.Location = new System.Drawing.Point(23, 28);
			this.m_msg.Margin = new System.Windows.Forms.Padding(0);
			this.m_msg.Name = "m_msg";
			this.m_msg.ReadOnly = true;
			this.m_msg.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
			this.m_msg.Size = new System.Drawing.Size(154, 59);
			this.m_msg.TabIndex = 0;
			this.m_msg.TabStop = false;
			this.m_msg.Text = "hint text goes here";
			//
			// HintBalloon
			//
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.BackColor = System.Drawing.SystemColors.ActiveBorder;
			this.ClientSize = new System.Drawing.Size(200, 120);
			this.ControlBox = false;
			this.Controls.Add(this.m_msg);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "HintBalloon";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.TransparencyKey = System.Drawing.Color.Fuchsia;
			this.ResumeLayout(false);
}
		#endregion
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using gui;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestTooltip
		{
			[Test] public static void Test()
			{
				const string text =
					"Short Msg\r\n"+
					"Really long message goes here with lots of words\r\n"+
					"Including some new lines\r\n"+
					"..and really really really really really really really really really really really really really really really really really really really really really really really long sentences"+
					"";

				//var btns = MessageBoxButtons.OKCancel;
				//var icon = MessageBoxIcon.Question;
				//var line = string.Join(" "  , Enumerable.Range(0, 30).Select(x => "123456789"));
				//var msg = string.Join("\r\n", Enumerable.Range(0, 30).Select(x => line));
				new HintBalloon().Show(null, new Point(500,500), text);
			}
		}
	}
}

#endif