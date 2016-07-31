using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using pr.gui;
using pr.util;

namespace Tradee
{
	/// <summary>Base class for UI components</summary>
	public class BaseUI :UserControl ,IDockable
	{
		private BaseUI() {}
		public BaseUI(MainModel model, string name)
		{
			DockControl = new DockControl(this, name) { TabText = name };
			Model = model;
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			DockControl = null;
			base.Dispose(disposing);
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)] public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				if (m_impl_dock_control != null) Util.Dispose(ref m_impl_dock_control);
				m_impl_dock_control = value;
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>The parent UI</summary>
		protected MainModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				}
				SetModelCore(value);
				if (m_model != null)
				{
				}
			}
		}
		protected virtual void SetModelCore(MainModel model)
		{
			m_model = model;
		}
		private MainModel m_model;

		/// <summary>Application settings</summary>
		protected Settings Settings
		{
			get { return Model.Settings; }
		}
	}
}
