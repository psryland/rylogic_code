using System.ComponentModel;
using System.Windows.Input;

namespace Rylogic.Gui.WPF
{
	public interface IChartCMenu :INotifyPropertyChanged
	{
		/// <summary>Allow property changed to be triggered externally</summary>
		void NotifyPropertyChanged(string prop_name);

		/// <summary>Show/hide grid lines</summary>
		bool ShowGridLines { get; set; }
		ICommand ToggleGridLines { get; }

		/// <summary>Show/hide axes</summary>
		bool ShowAxes { get; set; }
		ICommand ToggleAxes { get; }

		/// <summary>Show/hide the popup containing the pointer value</summary>
		bool ShowValueAtPointer { get; set; }
		ICommand ToggleShowValueAtPointer { get; }

		/// <summary>Show/hide the cross-hair</summary>
		bool ShowCrossHair { get; set; }
		ICommand ToggleShowCrossHair { get; }

		/// <summary>Navigation control mode</summary>
		ChartControl.ENavMode NavigationMode { get; set; }

		/// <summary>Auto Range</summary>
		ICommand DoAutoRange { get; }

		/// <summary>Reset the aspect ratio to 1:1</summary>
		ICommand DoAspect11 { get; }

		/// <summary>Get/Set whether the aspect ratio can change</summary>
		bool LockAspect { get; set; }
		ICommand ToggleLockAspect { get; }

		/// <summary>Get/Set whether zoom zooms in toward the mouse</summary>
		bool MouseCentredZoom { get; set; }
		ICommand ToggleMouseCentredZoom { get; }
	}
	public interface IChartAxisCMenu :INotifyPropertyChanged
	{
		/// <summary>Allow property changed to be triggered externally</summary>
		void NotifyPropertyChanged(string prop_name);

		/// <summary>Allow scrolling</summary>
		bool AllowScroll { get; set; }
		ICommand ToggleScrollLock { get; }

		/// <summary>Allow zooming</summary>
		bool AllowZoom { get; set; }
		ICommand ToggleZoomLock { get; }

		/// <summary></summary>
		ICollectionView LinkableCharts { get; }
		IChartProxy? LinkToChart { get; set; }
	}
}
