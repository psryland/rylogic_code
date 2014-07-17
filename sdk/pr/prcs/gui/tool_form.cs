using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace pr.gui
{
	/// <summary>A base class for (typically non-modal) tool-windows that move with their parent</summary>
	public class ToolForm :Form
	{
		public enum EPin
		{
			TopLeft,
			TopCentre,
			TopRight,
			BottomLeft,
			BottomCentre,
			BottomRight,
			CentreLeft,
			Centre,
			CentreRight,
		}

		/// <summary>Handle for the pin popup menu</summary>
		private readonly IntPtr m_sys_menu_handle;
		private const int m_menucmd_pin_window = 1000;

		/// <summary>Default constructor for the designer</summary>
		public ToolForm()
			:this(null, EPin.TopLeft, Point.Empty, Size.Empty, false)
		{}
		public ToolForm(Control target)
			:this(target, EPin.TopLeft, Point.Empty, Size.Empty, false)
		{}
		public ToolForm(Control target, EPin pin)
			:this(target, pin, Point.Empty, Size.Empty, false)
		{}
		public ToolForm(Control target, EPin pin, Point ofs)
			:this(target, pin, ofs, Size.Empty, false)
		{}
		public ToolForm(Control target, Point ofs, Size size)
			:this(target, EPin.TopLeft, ofs, size, false)
		{}

		/// <summary>
		/// Create and position the tool form.
		/// Remember, for positioning to work you need to ensure it's not overwritten
		/// in the call to InitiailizeComponent(). Leaving the StartPosition property
		/// as WindowDefaultPosition should prevent the designer adding an explicit set
		/// of the StartPosition property.</summary>
		public ToolForm(Control target, EPin pin, Point ofs, Size size, bool modal)
		{
			m_ofs = ofs;
			m_pin = pin;
			PinTarget = target;
			StartPosition = FormStartPosition.Manual;
			HideOnClose = !modal;
			AutoFade = 1.0f;
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			if (size != Size.Empty)
				Size = size;

			// Setup the pin menu
			m_sys_menu_handle = Win32.GetSystemMenu(Handle, false);
			Win32.InsertMenu(m_sys_menu_handle, 5, Win32.MF_BYPOSITION|Win32.MF_SEPARATOR, 0, string.Empty);
			Win32.InsertMenu(m_sys_menu_handle, 6, Win32.MF_BYPOSITION|Win32.MF_STRING, m_menucmd_pin_window, "&Pin Window");
			Win32.CheckMenuItem(m_sys_menu_handle, m_menucmd_pin_window, Win32.MF_BYCOMMAND|Win32.MF_CHECKED);
			m_pin_window = true;
		}
		protected override void Dispose(bool disposing)
		{
			PinTarget = null;
			base.Dispose(disposing);
		}

		/// <summary>How this tool window is pinned to the owner</summary>
		public EPin Pin
		{
			get { return m_pin; }
			set { m_pin = value; RecordOffset(); }
		}
		private EPin m_pin;

		/// <summary>True if the 'Pin' state should be used</summary>
		public bool PinWindow
		{
			get { return m_pin_window; }
			set
			{
				m_pin_window = value;
				RecordOffset();
				Win32.CheckMenuItem(m_sys_menu_handle, m_menucmd_pin_window, Win32.MF_BYCOMMAND|(m_pin_window ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
			}
		}
		private bool m_pin_window;

		/// <summary>The offset of top-left corner of this form to the pin location on the parent form</summary>
		public Point PinOffset
		{
			get { return m_ofs; }
			set { m_ofs = value; UpdateLocation(); }
		}
		private Point m_ofs;

		/// <summary>Automatically drop the opacity to this value when the mouse leaves the window bounds. Set to 1.0f to disable</summary>
		public float AutoFade { get; set; }

		/// <summary>Controls whether the form closes or just hides</summary>
		protected bool HideOnClose { get; set; }

		/// <summary>Get/Set the child control on the owner form that this tool window is pinned to. If null, assumes the form itself</summary>
		public Control PinTarget
		{
			get { return m_pin_target; }
			set
			{
				if (value == m_pin_target) return;
				if (m_pin_target != null)
				{
					m_pin_target.Move   -= UpdateLocation;
					m_pin_target.Resize -= UpdateLocation;
				}

				m_pin_target = value;
				WatchTopLevelControl();

				if (m_pin_target != null)
				{
					m_pin_target.Move   += UpdateLocation;
					m_pin_target.Resize += UpdateLocation;
				}
				UpdateLocation();
			}
		}
		private Control m_pin_target;

		/// <summary>Display the UI</summary>
		public void Display()
		{
			UpdateLocation();
			if (!Visible) Show(Owner);
			else          Focus();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (m_ofs == Point.Empty)
			{
				switch (m_pin)
				{
				default: throw new Exception("Unknown pin location '%d'".Fmt(m_pin));
				case EPin.TopLeft:      m_ofs = new Point(0             , 0); break;
				case EPin.TopCentre:    m_ofs = new Point(-Size.Width/2 , 0); break;
				case EPin.TopRight:     m_ofs = new Point(-Size.Width   , 0); break;
				case EPin.BottomLeft:   m_ofs = new Point(0             , -Size.Height); break;
				case EPin.BottomCentre: m_ofs = new Point(-Size.Width/2 , -Size.Height); break;
				case EPin.BottomRight:  m_ofs = new Point(-Size.Width   , -Size.Height); break;
				case EPin.CentreLeft:   m_ofs = new Point(0             , -Size.Height/2); break;
				case EPin.Centre:       m_ofs = new Point(-Size.Width/2 , -Size.Height/2); break;
				case EPin.CentreRight:  m_ofs = new Point(-Size.Width   , -Size.Height/2); break;
				}
			}
			UpdateLocation();
		}
		protected override void OnShown(EventArgs e)
		{
			WatchTopLevelControl();
			base.OnShown(e);
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			if (e.KeyCode == Keys.Escape)
			{
				Close();
				return;
			}
			if (e.KeyCode == Keys.Tab && e.Control && Owner != null)
			{
				Owner.Focus();
				return;
			}
			e.Handled = false;
			base.OnKeyDown(e);
		}
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			HandleAutoFade();
		}
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			HandleAutoFade();
		}
		protected override void OnMove(EventArgs e)
		{
			// Whenever this window moves, save it's offset from the owner
			base.OnMove(e);
			RecordOffset();
		}
		protected override void OnLocationChanged(EventArgs e)
		{
			// Whenever this window moves, save it's offset from the owner
			base.OnLocationChanged(e);
			RecordOffset();
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			// On closing, if non-modal just hide, otherwise remove the Move handler from the owner
 			base.OnFormClosing(e);
			if (HideOnClose && e.CloseReason == CloseReason.UserClosing)
			{
				Hide();
				e.Cancel = true;
				if (Owner != null)
					Owner.Focus();
			}
		}
		protected override void OnFormClosed(FormClosedEventArgs e)
		{
 			base.OnFormClosed(e);
			Owner = null;
		}
		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
			e.Control.MouseEnter += HandleAutoFade;
			e.Control.MouseLeave += HandleAutoFade;
		}
		protected override void OnControlRemoved(ControlEventArgs e)
		{
			e.Control.MouseEnter -= HandleAutoFade;
			e.Control.MouseLeave -= HandleAutoFade;
			base.OnControlRemoved(e);
		}

		/// <summary>Set the form opacity based on where the mouse is</summary>
		private void HandleAutoFade(object sender = null, EventArgs args = null)
		{
			Opacity = ClientRectangle.Contains(PointToClient(MousePosition)) ? 1.0f : AutoFade;
		}

		/// <summary>Get/Set the owning form</summary>
		private void WatchTopLevelControl()
		{
			var top = m_pin_target != null ? m_pin_target.TopLevelControl : null;
			if (top == m_top_level_control) return;

			// Watch the owning form for position/size changes
			// Whenever the owner moves, move this form as well
			if (m_top_level_control != null)
			{
				m_top_level_control.Move   -= UpdateLocation;
				m_top_level_control.Resize -= UpdateLocation;
			}

			m_top_level_control = top;
			Owner = m_top_level_control as Form;

			if (m_top_level_control != null)
			{
				m_top_level_control.Move   += UpdateLocation;
				m_top_level_control.Resize += UpdateLocation;
			}
			UpdateLocation();
		}
		private Control m_top_level_control;

		/// <summary>Returns the bounds of the form/control that the tool is pinned to</summary>
		protected Rectangle TargetFrame
		{
			get
			{
				Debug.Assert(PinTarget != null);
				return PinTarget.RectangleToScreen(PinTarget.Bounds);
			}
		}

		/// <summary>Records the current offset of this form from the owner form</summary>
		protected void RecordOffset() { RecordOffset(null, null); }
		protected void RecordOffset(object sender, EventArgs args)
		{
			if (Owner == null || !PinWindow) return;
			var frame = TargetFrame;
			switch (Pin)
			{
			default: throw new Exception("Unknown pin location '%d'".Fmt(Pin));
			case EPin.TopLeft:      m_ofs = new Point(Location.X - frame.Left                 , Location.Y - frame.Top   ); break;
			case EPin.TopCentre:    m_ofs = new Point(Location.X - (frame.Left+frame.Right)/2 , Location.Y - frame.Top   ); break;
			case EPin.TopRight:     m_ofs = new Point(Location.X - frame.Right                , Location.Y - frame.Top   ); break;
			case EPin.BottomLeft:   m_ofs = new Point(Location.X - frame.Left                 , Location.Y - frame.Bottom); break;
			case EPin.BottomCentre: m_ofs = new Point(Location.X - (frame.Left+frame.Right)/2 , Location.Y - frame.Bottom); break;
			case EPin.BottomRight:  m_ofs = new Point(Location.X - frame.Right                , Location.Y - frame.Bottom); break;
			case EPin.CentreLeft:   m_ofs = new Point(Location.X - frame.Left                 , Location.Y - (frame.Top+frame.Bottom)/2); break;
			case EPin.Centre:       m_ofs = new Point(Location.X - (frame.Left+frame.Right)/2 , Location.Y - (frame.Top+frame.Bottom)/2); break;
			case EPin.CentreRight:  m_ofs = new Point(Location.X - frame.Right                , Location.Y - (frame.Top+frame.Bottom)/2); break;
			}
		}

		/// <summary>Called to update the position relative to the owner form</summary>
		protected void UpdateLocation() { UpdateLocation(null, null); }
		protected void UpdateLocation(object sender, EventArgs args)
		{
			if (Owner == null || !PinWindow) return;
			var frame = TargetFrame;
			switch (Pin)
			{
			default: throw new Exception("Unknown pin location '%d'".Fmt(Pin));
			case EPin.TopLeft:      Location = new Point(frame.Left                 , frame.Top   ) + m_ofs.ToSize(); break;
			case EPin.TopCentre:    Location = new Point((frame.Left+frame.Right)/2 , frame.Top   ) + m_ofs.ToSize(); break;
			case EPin.TopRight:     Location = new Point(frame.Right                , frame.Top   ) + m_ofs.ToSize(); break;
			case EPin.BottomLeft:   Location = new Point(frame.Left                 , frame.Bottom) + m_ofs.ToSize(); break;
			case EPin.BottomCentre: Location = new Point((frame.Left+frame.Right)/2 , frame.Bottom) + m_ofs.ToSize(); break;
			case EPin.BottomRight:  Location = new Point(frame.Right                , frame.Bottom) + m_ofs.ToSize(); break;
			case EPin.CentreLeft:   Location = new Point(frame.Left                 , (frame.Top+frame.Bottom)/2) + m_ofs.ToSize(); break;
			case EPin.Centre:       Location = new Point((frame.Left+frame.Right)/2 , (frame.Top+frame.Bottom)/2) + m_ofs.ToSize(); break;
			case EPin.CentreRight:  Location = new Point(frame.Right                , (frame.Top+frame.Bottom)/2) + m_ofs.ToSize(); break;
			}
		}

		/// <summary>Handle the system menu options</summary>
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == Win32.WM_SYSCOMMAND)
			{
				switch (m.WParam.ToInt32())
				{
				case m_menucmd_pin_window: PinWindow = !PinWindow; return;
				}
			}
			base.WndProc(ref m);
		}
	}
}
