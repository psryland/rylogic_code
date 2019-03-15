using System.Windows;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF.ChartDetail
{
	public class ChartPanel : View3dControl
	{
		public ChartPanel()
		{
			try
			{
				MouseNavigation = false;
				DefaultKeyboardShortcuts = false;
				Window.FocusPointVisible = false;
				Window.OriginPointVisible = false;
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		protected override void OnRenderTargetChanged()
		{
			base.OnRenderTargetChanged();
			var chart = Chart;
			if (chart != null)
			{
				chart.SetRangeFromCamera();
				Invalidate();
			}
		}

		/// <summary>The containing chart control</summary>
		private ChartControl Chart => Gui_.FindVisualParent<ChartControl>(Parent);

		/// <summary>Add an object to the scene</summary>
		public void AddObject(View3d.Object obj)
		{
			Window.AddObject(obj);
		}

		/// <summary>Remove an object from the scene</summary>
		public void RemoveObject(View3d.Object obj)
		{
			Window.RemoveObject(obj);
		}

		/// <summary>Prepare the scene for rendering</summary>
		protected override void OnBuildScene()
		{
			// Update axis graphics
			Chart.Range.AddToScene(Window);

			// Add/Remove all chart elements
			foreach (var elem in Chart.Elements)
				elem.UpdateScene(Window);

			// Raise the BuildScene event
			base.OnBuildScene();
		}

		/// <summary>Returns a point in chart space from a point in ChartPanel-client space.</summary>
		private Point ClientToChart(Point point)
		{
			return Gui_.MapPoint(this, Chart, point);
		}

		/// <summary>Returns a point in ChartPanel-client space from a point in chart space. Inverse of ClientToChart</summary>
		private Point ChartToClient(Point point)
		{
			return Gui_.MapPoint(Chart, this, Chart.ChartToClient(point));
		}
	}
}
