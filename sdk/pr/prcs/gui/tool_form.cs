using System;
using System.Drawing;
using System.Windows.Forms;

namespace pr.gui
{
	/// <summary>A base class for (typically non-modal) tool-windows that move with their parent</summary>
	public class ToolForm :Form
	{
		public enum EPin { TopLeft, TopRight, BottomLeft, BottomRight };
		private Size m_ofs;
		private EPin m_pin;
		
		/// <summary>How this tool window is pinned to the owner</summary>
		public EPin Pin { get { return m_pin; } set { m_pin = value; RecordOffset(); } }
		
		/// <summary>Default constructor for the designer</summary>
		public ToolForm()
		:this(null, Size.Empty, Size.Empty, EPin.TopLeft, false)
		{}
		public ToolForm(Form owner)
		:this(owner, Size.Empty, Size.Empty, EPin.TopLeft, false)
		{}
		public ToolForm(Form owner, Size ofs, Size size)
		:this(owner, ofs, size, EPin.TopLeft, false)
		{}
		
		/// <summary>Create an position the tool form.
		/// Remember, for positioning to work you need to ensure it's not overwritten
		/// in the call to InitiailizeComponent(). Leaving the StartPosition property
		/// as WindowDefaultPosition should prevent the designer adding an explicit set
		/// of the StartPosition property.</summary>
		public ToolForm(Form owner, Size ofs, Size size, EPin pin, bool modal)
		{
			m_ofs = Size.Empty;
			m_pin = pin;
			Owner = owner;
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.SizableToolWindow;
			StartPosition = FormStartPosition.CenterParent;
			if (ofs  != Size.Empty) { m_ofs = ofs; StartPosition = FormStartPosition.Manual; }
			if (size != Size.Empty) Size = size;
			
			// Whenever this window moves, save it's offset from the owner
			Move += RecordOffset;
			
			// Whenever the owner moves, move this form as well
			if (Owner != null)
				Owner.Move += UpdateLocation;
			
			// On closing, if non-modal just hide, otherwise remove the Move handler from the owner
			FormClosing += (s,a)=>
				{
					if (!modal && a.CloseReason == CloseReason.UserClosing)
					{
						Hide();
						a.Cancel = true;
						if (Owner != null)
							Owner.Focus();
					}
					else
					{
						if (Owner != null)
							Owner.Move -= UpdateLocation;
					}
				};
			
			UpdateLocation();
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
			case EPin.TopLeft:     m_ofs = new Size(Location.X - Owner.Left , Location.Y - Owner.Top   ); break;
			case EPin.TopRight:    m_ofs = new Size(Location.X - Owner.Right, Location.Y - Owner.Top   ); break;
			case EPin.BottomLeft:  m_ofs = new Size(Location.X - Owner.Left , Location.Y - Owner.Bottom); break;
			case EPin.BottomRight: m_ofs = new Size(Location.X - Owner.Right, Location.Y - Owner.Bottom); break;
			}
		}

		/// <summary>Called to update the position relative to the owner form</summary>
		protected void UpdateLocation() { UpdateLocation(null, null); }
		protected void UpdateLocation(object sender, EventArgs args)
		{
			if (Owner == null) return;
			switch (Pin)
			{
			case EPin.TopLeft:     Location = new Point(Owner.Left , Owner.Top   ) + m_ofs; break;
			case EPin.TopRight:    Location = new Point(Owner.Right, Owner.Top   ) + m_ofs; break;
			case EPin.BottomLeft:  Location = new Point(Owner.Left , Owner.Bottom) + m_ofs; break;
			case EPin.BottomRight: Location = new Point(Owner.Right, Owner.Bottom) + m_ofs; break;
			}
		}
	}
}
