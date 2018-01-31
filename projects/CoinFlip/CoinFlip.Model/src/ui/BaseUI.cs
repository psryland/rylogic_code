using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>Base class for UI components</summary>
	public class BaseUI :UserControl ,IDockable
	{
		private BaseUI() {}
		public BaseUI(Model model, string name)
		{
			m_model = model;
			DockControl = new DockControl(this, name) { TabText = name };

			// Even though the model has been set, set it again to give
			// sub classes a chance to hook up event handlers
			CreateHandle();
			this.BeginInvoke(() => SetModelCore(model));
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			DockControl = null;
			base.Dispose(disposing);
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		 {
		 	get { return m_impl_dock_control; }
		 	private set
		 	{
		 		if (m_impl_dock_control == value) return;
		 		Util.Dispose(ref m_impl_dock_control);
		 		m_impl_dock_control = value;
		 	}
		 }
		private DockControl m_impl_dock_control;

		/// <summary>Raised when the dockable is removed from all panes</summary>
		public event EventHandler Closed
		{
			add { DockControl.Closed += value; }
			remove { DockControl.Closed -= value; }
		}

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			get { return Model.Settings; }
		}

		/// <summary>The parent UI</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			set
			{
				if (m_model == value) return;
				SetModelCore(value);
			}
		}
		private Model m_model;
		protected virtual void SetModelCore(Model model)
		{
			m_model = model;
		}

		/// <summary>Invalidate this control and all children</summary>
		protected void Invalidate(object sender, EventArgs e)
		{
			Invalidate(true);
		}
	}
}
