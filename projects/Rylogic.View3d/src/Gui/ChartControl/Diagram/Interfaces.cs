using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	/// <summary>Marker interface for element styles</summary>
	public interface IStyle :INotifyPropertyChanged
	{
	}

	/// <summary>For elements that have an associated IStyle object</summary>
	public interface IHasStyle
	{
		IStyle Style { get; }
	}

	/// <summary>Says "I have a GUID"</summary>
	internal interface IHasId
	{
		Guid Id { get; }
	}

	/// <summary>Diagram menu item</summary>
	public interface IDiagramCMenu :INotifyPropertyChanged
	{
		/// <summary>Enable/Disable continuous scattering of nodes</summary>
		bool Scattering { get; }
		ICommand ToggleScattering { get; }
	}
	public interface IDiagramCMenuContext
	{
		/// <summary>The data context for diagram context menu items</summary>
		IDiagramCMenu DiagramCMenuContext { get; }
	}
}