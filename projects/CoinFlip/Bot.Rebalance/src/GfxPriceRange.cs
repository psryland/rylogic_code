using System;
using System.Windows.Input;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Bot.Rebalance
{
	public class GfxPriceRange :ChartControl.Element
	{
		// Notes:
		// - Chart graphics to represent the price range
		public GfxPriceRange(Guid id)
			:base(id, m4x4.Identity, "PriceRange")
		{
		}
		public override void Dispose()
		{
			Gfx = null;
			base.Dispose();
		}
		protected override void SetChartCore(ChartControl chart)
		{
			if (Chart != null)
			{
				Chart.PreviewMouseDown -= HandleMouseDown;
			}
			base.SetChartCore(chart);
			if (Chart != null)
			{
				Chart.PreviewMouseDown += HandleMouseDown;
			}

			// Handlers
			void HandleMouseDown(object sender, MouseButtonEventArgs args)
			{
			//	if (Hovered && args.ChangedButton == MouseButton.Left && args.LeftButton == MouseButtonState.Pressed)
			//		Chart.MouseOperations.SetPending(MouseButton.Left, new DragPrice(this));
			}
		}

		/// <summary>The graphics object</summary>
		public View3d.Object Gfx
		{
			get { return m_gfx; }
			private set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx;

		protected override void UpdateGfxCore()
		{
			base.UpdateGfxCore();

			var ldr =
				$"Group* price_range\n" +
				$"{{\n" +

				$"}}\n";

			Gfx = new View3d.Object(ldr, false, Id, null);
		}
	}
}
