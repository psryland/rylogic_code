using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;

namespace pr.gui
{
	/// <summary>A base class for (typically non-modal) tool-windows that move with their parent</summary>
	public class ToolForm :Form
	{
		public enum EPin { TopLeft, TopRight, BottomLeft, BottomRight, Centre };

		/// <summary>How this tool window is pinned to the owner</summary>
		public EPin Pin
		{
			get { return m_pin; }
			set { m_pin = value; RecordOffset(); }
		}
		private EPin m_pin;

		/// <summary>The offset of this form from the pin location</summary>
		public Point PinOffset
		{
			get { return m_ofs; }
			set { m_ofs = value; UpdateLocation(); }
		}
		private Point m_ofs;

		/// <summary>Default constructor for the designer</summary>
		public ToolForm()
		:this(null, EPin.TopLeft, Point.Empty, Size.Empty, false)
		{}
		public ToolForm(Form owner)
		:this(owner, EPin.TopLeft, Point.Empty, Size.Empty, false)
		{}
		public ToolForm(Form owner, EPin pin)
		:this(owner, pin, Point.Empty, Size.Empty, false)
		{}
		public ToolForm(Form owner, EPin pin, Point ofs)
		:this(owner, pin, ofs, Size.Empty, false)
		{}
		public ToolForm(Form owner, Point ofs, Size size)
		:this(owner, EPin.TopLeft, ofs, size, false)
		{}

		/// <summary>
		/// Create and position the tool form.
		/// Remember, for positioning to work you need to ensure it's not overwritten
		/// in the call to InitiailizeComponent(). Leaving the StartPosition property
		/// as WindowDefaultPosition should prevent the designer adding an explicit set
		/// of the StartPosition property.</summary>
		public ToolForm(Form owner, EPin pin, Point ofs, Size size, bool modal)
		{
			m_ofs = Point.Empty;
			m_pin = pin;
			Owner = owner;
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.SizableToolWindow;
			StartPosition = FormStartPosition.CenterParent;
			HideOnClose = !modal;
			if (ofs  != Point.Empty) { m_ofs = ofs; StartPosition = FormStartPosition.Manual; }
			if (size != Size.Empty) Size = size;

			// Whenever this window moves, save it's offset from the owner
			Move += RecordOffset;
			LocationChanged += RecordOffset;

			// On closing, if non-modal just hide, otherwise remove the Move handler from the owner
			FormClosing += (s,a)=>
				{
					if (HideOnClose && a.CloseReason == CloseReason.UserClosing)
					{
						Hide();
						a.Cancel = true;
						if (Owner != null)
							Owner.Focus();
					}
				};
			FormClosed += (s,a) =>
				{
					Owner = null;
				};

			UpdateLocation();
		}

		/// <summary>Controls whether the form closes or just hides</summary>
		protected bool HideOnClose { get; set; }

		/// <summary>Hide </summary>
		public new Form Owner
		{
			get { return base.Owner; }
			set
			{
				if (value != Owner)
				{
					if (base.Owner != null)
					{
						base.Owner.Move   -= UpdateLocation;
						base.Owner.Resize -= UpdateLocation;
						base.Owner         = null;
					}
					if (value != null)
					{
						// Whenever the owner moves, move this form as well
						base.Owner         = value;
						base.Owner.Move   += UpdateLocation;
						base.Owner.Resize += UpdateLocation;
					}
				}
				UpdateLocation();
			}
		}

		/// <summary>Display the UI</summary>
		public void Display()
		{
			UpdateLocation();
			if (!Visible) Show(Owner);
			else          Focus();
		}

		/// <summary>Handle key presses</summary>
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

		/// <summary>Records the current offset of this form from the owner form</summary>
		protected void RecordOffset() { RecordOffset(null, null); }
		protected void RecordOffset(object sender, EventArgs args)
		{
			if (Owner == null) return;
			switch (Pin)
			{
			default: Debug.Assert(false, "Unknown pin type"); break;
			case EPin.TopLeft:     m_ofs = new Point(Location.X - Owner.Left , Location.Y - Owner.Top   ); break;
			case EPin.TopRight:    m_ofs = new Point(Location.X - Owner.Right, Location.Y - Owner.Top   ); break;
			case EPin.BottomLeft:  m_ofs = new Point(Location.X - Owner.Left , Location.Y - Owner.Bottom); break;
			case EPin.BottomRight: m_ofs = new Point(Location.X - Owner.Right, Location.Y - Owner.Bottom); break;
			case EPin.Centre:      m_ofs = new Point(Location.X - (Owner.Left+Owner.Right)/2, Location.Y - (Owner.Top+Owner.Bottom)/2); break;
			}
		}

		/// <summary>Called to update the position relative to the owner form</summary>
		protected void UpdateLocation() { UpdateLocation(null, null); }
		protected void UpdateLocation(object sender, EventArgs args)
		{
			if (Owner == null) return;
			switch (Pin)
			{
			default: Debug.Assert(false, "Unknown pin type"); break;
			case EPin.TopLeft:     Location = new Point(Owner.Left , Owner.Top   ) + m_ofs.ToSize(); break;
			case EPin.TopRight:    Location = new Point(Owner.Right, Owner.Top   ) + m_ofs.ToSize(); break;
			case EPin.BottomLeft:  Location = new Point(Owner.Left , Owner.Bottom) + m_ofs.ToSize(); break;
			case EPin.BottomRight: Location = new Point(Owner.Right, Owner.Bottom) + m_ofs.ToSize(); break;
			case EPin.Centre:      Location = new Point((Owner.Left+Owner.Right)/2, (Owner.Top+Owner.Bottom)/2) + m_ofs.ToSize(); break;
			}
		}
	}
}
