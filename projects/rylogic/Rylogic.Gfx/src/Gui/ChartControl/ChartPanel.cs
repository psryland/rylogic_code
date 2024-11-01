using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF.ChartDetail
{
	public class ChartPanel : View3dControl, IChartCMenuContext
	{
		public ChartPanel()
		{
			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			try
			{
				MouseNavigation = false;
				DefaultKeyboardShortcuts = false;
				Window.FocusPointVisible = false;
				Window.OriginPointVisible = false;
				Window.LightProperties = View3d.LightInfo.Directional(-v4.ZAxis, camera_relative: true);
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
				// If the aspect is locked, resizing the render target zooms in/out.
				// Otherwise, change the camera position so that the range is preserved.
				if (chart.LockAspect)
					chart.SetRangeFromCamera();
				else
					chart.SetCameraFromRange();
				Invalidate();
			}
		}

		/// <inheritdoc/>
		public new IView3dCMenu View3dCMenuContext => Chart;

		/// <inheritdoc/>
		public IChartCMenu ChartCMenuContext => Chart;

		/// <summary>The containing chart control</summary>
		internal ChartControl Chart
		{
			get => m_chart;
			set
			{
				if (m_chart == value) return;
				m_chart = value;
				DataContext = m_chart;
			}
		}
		private ChartControl m_chart = null!;

		/// <summary>Add an object to the scene</summary>
		public void AddObject(View3d.Object obj)
		{
			Window.AddObject(obj);
		}
		public void AddObjects(IEnumerable<View3d.Object> objects)
		{
			Window.AddObjects(objects);
		}
		public void AddObjects(Guid context_id)
		{
			Window.AddObjects(context_id);
		}
		public void AddObjects(Guid[] context_ids, int include_count, int exclude_count)
		{
			Window.AddObjects(context_ids, include_count, exclude_count);
		}

		/// <summary>Remove an object from the scene</summary>
		public void RemoveObject(View3d.Object obj)
		{
			Window.RemoveObject(obj);
		}
		public void RemoveObjects(IEnumerable<View3d.Object> objects)
		{
			Window.RemoveObjects(objects);
		}
		public void RemoveObjects(Guid[] context_ids, int include_count, int exclude_count)
		{
			Window.RemoveObjects(context_ids, include_count, exclude_count);
		}
		public void RemoveAllObjects()
		{
			Window.RemoveAllObjects();
		}

		/// <summary>Prepare the scene for rendering</summary>
		protected override void OnBuildScene()
		{
			if (Chart == null)
				return;

			// Update axis graphics
			Chart.Range.UpdateScene(Window);

			// Raise the BuildScene event
			// Do this before calling UpdateScene on each element so that last minute
			// changes to the elements can be made. E.g. setting visibility, invalidating gfx, etc
			base.OnBuildScene();

			// Add/Remove all chart elements
			foreach (var elem in Chart.Elements)
				elem.UpdateScene();
		}
	}
}
