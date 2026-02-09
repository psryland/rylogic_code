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
			:base(id, "PriceRange")
		{
		}
		protected override void Dispose(bool _)
		{
			Gfx = null;
			base.Dispose(_);
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
			get;
			private set
			{
				if (field == value) return;
				Util.Dispose(ref field);
				field = value;
			}
		}

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
