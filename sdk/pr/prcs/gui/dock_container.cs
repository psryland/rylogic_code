using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using pr.maths;
using pr.util;

namespace pr.gui
{
	public class DockForm :Form
	{
		/// <summary>Ways in which a form can be docked</summary>
		public enum EDockState
		{
			Floating,
			Centre,
			Left,
			Top,
			Right,
			Bottom,
		}
		
		// members
		internal DockContainer m_container;      // The container that is managing this form
		private  EDockState    m_dock_state;     // The current dock state of the form
		private  bool          m_auto_hide;      // True if forms should auto hide once they have lost focus
		private  bool          m_moving;         // True while the form is moving
		
		/// <summary>Get/Set whether the form should hide itself automatically after losing focus</summary>
		public bool AutoHide
		{
			get { return m_auto_hide; }
			set
			{
				if (value == m_auto_hide) return;
				m_auto_hide = value;
			}
		}
		
		/// <summary>How the form is currently docked</summary>
		public EDockState DockState
		{
			get { return m_dock_state; }
			set
			{
				if (value == m_dock_state) return;
				DockStateChangedEventArgs evt_args = new DockStateChangedEventArgs(m_dock_state, value);
				m_dock_state = value;
				DoDock();
				if (DockStateChanged != null) DockStateChanged(this, evt_args);
			}
		}

		/// <summary>Occurs when the dock state of the form is changed</summary>
		public Action<object, DockStateChangedEventArgs> DockStateChanged;
		public class DockStateChangedEventArgs :EventArgs
		{
			public EDockState OldDockState {get;private set;}
			public EDockState NewDockState {get;private set;}
			internal DockStateChangedEventArgs(EDockState old_ds, EDockState new_ds) { OldDockState = old_ds; NewDockState = new_ds; }
		}
		
		/// <summary>Constructor</summary>
		public DockForm()
		{
			//TopLevel = false;
			ShowInTaskbar = true;//false;
			FormBorderStyle = FormBorderStyle.SizableToolWindow;
			WindowState = FormWindowState.Normal;
			StartPosition = FormStartPosition.Manual;
			m_dock_state = EDockState.Floating;
		}
		
		/// <summary>WinProc handler for doing things to the actual window, not just the client area</summary>
		protected override void WndProc(ref Message m)
		{
			switch ((uint)m.Msg)
			{
			default: break;
			case Win32.WM_SYSCOMMAND:
				m_moving = ((uint)m.WParam & Win32.SC_WPARAM_MASK) == Win32.SC_MOVE;
				break;
			}
			base.WndProc(ref m);
		}
		
		/// <summary>Position this form within the container appropriately for its dock stack</summary>
		internal void DoDock()
		{
			DockContainer.FormLayout layout = new DockContainer.FormLayout(m_container);
			switch (m_dock_state)
			{
			default: throw new ArgumentOutOfRangeException();
			case EDockState.Floating:
				
				m_container.Controls.Remove(this);
				TopLevel = true;
				if (Visible) { Hide(); Show(m_container); }
				//if (ParentForm == null) throw new InvalidOperationException("The dock container must have a parent before forms are added");
				//form.Location = PointToScreen(Location);
				//form.BringToFront();
				//form.Show(this);
				
				break; // floating windows aren't affected
			case EDockState.Centre:
				break;
			case EDockState.Left:

				break;
			case EDockState.Top:
				break;
			case EDockState.Right:
				break;
			case EDockState.Bottom:
				break;
			}
		}
		protected override void OnResizeBegin(EventArgs e)
		{
			if (m_moving) Opacity = 0.5f;
			base.OnResizeBegin(e);
		}
		protected override void OnResizeEnd(EventArgs e)
		{
			if (m_moving) { Opacity = 1f; m_moving = false; }
			base.OnResizeEnd(e);
		}
		protected override void OnMove(EventArgs e)
		{
			base.OnMove(e);
			if (m_moving) m_container.PreviewDock(PointToClient(MousePosition));
		}
		
		// Hide these set methods from the client
		public   new FormBorderStyle FormBorderStyle { get {return base.FormBorderStyle;} internal set {base.FormBorderStyle = value;} }
		public   new bool            ShowInTaskbar   { get {return base.ShowInTaskbar;}   internal set {base.ShowInTaskbar = value;}   }
		public   new Point           Location        { get {return base.Location;}        internal set {base.Location = value;}        }
		public   new Size            Size            { get {return base.Size;}            internal set {base.Size = value;}            }
		public   new Rectangle       Bounds          { get {return base.Bounds;}          internal set {base.Bounds = value;}          }
		public   new Size            ClientSize      { get {return base.ClientSize;}      internal set {base.ClientSize = value;}      }
		public   new int             Width           { get {return base.Width;}           internal set {base.Width = value;}           }
		public   new int             Height          { get {return base.Height;}          internal set {base.Height = value;}          }
	}
	
	/// <summary>A container for managing dockable forms</summary>
	public class DockContainer :UserControl
	{
		/// <summary>Style parameters for the dock container</summary>
		public class StyleData
		{
			/// <summary>The width in pixels of the button panel</summary>
			public int MarginSize {get;set;}
			
			/// <summary>Constructor</summary>
			public StyleData()
			{
				MarginSize = 80;
			}
		}
		
		/// <summary>Describes the layout of all forms at the time of construction</summary>
		internal class FormLayout
		{
			public List<DockForm> m_floating = new List<DockForm>();
			public List<DockForm> m_centre   = new List<DockForm>();
			public List<DockForm> m_left     = new List<DockForm>();
			public List<DockForm> m_top      = new List<DockForm>();
			public List<DockForm> m_right    = new List<DockForm>();
			public List<DockForm> m_bottom   = new List<DockForm>();
			public Rectangle m_rect_c;
			public Rectangle m_rect_l;
			public Rectangle m_rect_t;
			public Rectangle m_rect_r;
			public Rectangle m_rect_b;
			
			public FormLayout(DockContainer cont)
			{
				foreach (DockForm df in cont.m_forms)
				{
					switch (df.DockState)
					{
					default: throw new ArgumentOutOfRangeException();
					case DockForm.EDockState.Floating: m_floating.Add(df); break;
					case DockForm.EDockState.Centre:   m_centre  .Add(df); break;
					case DockForm.EDockState.Left:     m_left    .Add(df); break;
					case DockForm.EDockState.Top:      m_top     .Add(df); break;
					case DockForm.EDockState.Right:    m_right   .Add(df); break;
					case DockForm.EDockState.Bottom:   m_bottom  .Add(df); break;
					}
				}
				m_rect_c = cont.ClientRectangle;
				m_rect_l = Rectangle.Empty;
				m_rect_t = Rectangle.Empty;
				m_rect_r = Rectangle.Empty;
				m_rect_b = Rectangle.Empty;
			}
		}
		
		/// <summary>The forms managed by this container</summary>
		private readonly List<DockForm> m_forms = new List<DockForm>();
		
		/// <summary>The top level split container</summary>
		internal SplitContainer m_split = new SplitContainer{Panel2Collapsed = true, Dock = DockStyle.Fill, Visible = false};
		
		/// <summary>Style parameters for the dock container</summary>
		public StyleData Style {get;set;}
		
		/// <summary>Constructor</summary>
		public DockContainer()
		{
			InitializeComponent();
			Style = new StyleData();
			Controls.Add(m_split);
		}
		
		/// <summary>Add a form to be managed by this dock container</summary>
		public void Add(DockForm form)
		{
			form.m_container = this;
			m_forms.Add(form);
			form.DoDock();
		}
		
		/// <summary>Remove a form from this dock container</summary>
		public void Remove(DockForm form)
		{
			if (form.m_container == null) return;
			if (form.m_container != this) throw new ArgumentException("'form' is not managed by this DockContainer", "form");
			form.DockState = DockForm.EDockState.Floating;
			m_forms.Remove(form);
			form.m_container = null;
		}
		
		/// <summary>Resize docked windows when the container resizes</summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			foreach (DockForm df in m_forms) df.DoDock();
		}
		
		/// <summary>Returns the splitter panel and dock state within a splitter panel corresponding to mouse position 'loc'.
		/// 'loc' should be in dock container client space
		/// If 'panel' is null, then docking should be at the top level</summary>
		private void FindDockLocation(Point loc, out SplitterPanel panel, out DockForm.EDockState dock_state)
		{
			panel = null;
			dock_state = DockForm.EDockState.Floating;
			Rectangle area = ClientRectangle;
			
			// Find the panel in the deepest splitter control under the mouse
			for (SplitContainer sc = m_split; sc != null;)
			{
				if      (sc.Panel1.Bounds.Contains(loc)) panel = sc.Panel1;
				else if (sc.Panel2.Bounds.Contains(loc)) panel = sc.Panel2;
				else panel = null;
				if (panel != null && panel.Controls.Count != 0)
					sc = panel.Controls[0] as SplitContainer;
				else
					break;
			}
			
			// Test 'loc' against the edges of the dock container
			if (loc.X > loc.Y)
			{
				if (loc.X < area.Width * 0.2f) { panel = null; dock_state = DockForm.EDockState.Left; }
				if (loc.X > area.Width * 0.8f) { panel = null; dock_state = DockForm.EDockState.Right; }
			}
			else
			{
			}
		}
		
		/// <summary>Show a preview panel of where a form would dock to if dropped at 'loc'</summary>
		internal void PreviewDock(Point loc)
		{
			SplitterPanel panel;
			DockForm.EDockState dock_state;
			FindDockLocation(loc, out panel, out dock_state);
		}
		
		#region Component Designer generated code
		/// <summary> Required designer variable.</summary>
		private IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) components.Dispose();
			base.Dispose(disposing);
		}
		
		/// <summary> Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// DockContainer
			// 
			this.BackColor = System.Drawing.SystemColors.ControlDark;
			this.Name = "DockContainer";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
