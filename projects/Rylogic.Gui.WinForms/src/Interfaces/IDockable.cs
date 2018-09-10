//**********************************************************************
// DockContainer
//  Copyright (c) Rylogic 2015
//**********************************************************************

namespace Rylogic.Gui.WinForms
{
	/// <summary>A control that can be docked within a DockContainer</summary>
	public interface IDockable
	{
		// Typical implementation:
		//  /// <summary>Provides support for the DockContainer</summary>
		//  [Browsable(false)]
		//  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		//  public DockControl DockControl
		//  {
		//  	get { return m_impl_dock_control; }
		//  	private set
		//  	{
		//  		if (m_impl_dock_control == value) return;
		//  		Util.Dispose(ref m_impl_dock_control);
		//  		m_impl_dock_control = value;
		//  	}
		//  }
		//  private DockControl m_impl_dock_control;

		/// <summary>The docking implementation object that provides the docking functionality</summary>
		DockControl DockControl { get; }
	}
}
