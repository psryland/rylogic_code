using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	/// <summary>Chart graphics representing the trade</summary>
	public class TradeIndicator : ChartControl.Element
	{
		private const float GripperWidthFrac = 0.05f;
		private const float GripperHeight = 24f;

		public TradeIndicator(Trade trade)
			: base(Guid.NewGuid(), m4x4.Identity, "Trade")
		{
			Trade = trade;
		}
		public override void Dispose()
		{
			Gfx = null;
			Trade = null;
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
				if (Hovered && args.ChangedButton == MouseButton.Left && args.LeftButton == MouseButtonState.Pressed)
					Chart.MouseOperations.SetPending(MouseButton.Left, new DragPrice(this));
			}
		}

		/// <summary>The Trade being 'indicated'</summary>
		private Trade Trade
		{
			get { return m_trade; }
			set
			{
				if (m_trade == value) return;
				if (m_trade != null)
				{
					m_trade.PropertyChanged -= Invalidate;
				}
				m_trade = value;
				if (m_trade != null)
				{
					m_trade.PropertyChanged += Invalidate;
				}
			}
		}
		private Trade m_trade;

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

		/// <summary>The trade type being represented</summary>
		public ETradeType TradeType => Trade.TradeType;

		/// <summary>The price level that the indicator is at</summary>
		public Unit<decimal> PriceQ2B
		{
			get => Trade.PriceQ2B;
			set => Trade.PriceQ2B = value;
		}

		/// <summary>Position/Colour the graphics</summary>
		protected override void UpdateGfxCore()
		{
			base.UpdateGfxCore();

			// Price displayed with 8 significant figures
			var price = ((decimal)PriceQ2B).ToString(8);

			// Colour based on trade direction
			var col =
				TradeType == ETradeType.Q2B ? SettingsData.Settings.Chart.AskColour :
				TradeType == ETradeType.B2Q ? SettingsData.Settings.Chart.BidColour :
				throw new Exception("Unknown trade type");

			var ldr =
				$"*Group trade " +
				$"{{" +
				$"  *Line gripper {col} {{ {1f - GripperWidthFrac} 0 0  1 0 0 *Width {{{GripperHeight}}} }}" +
				$"  *Line level {col} {{0 0 0 1 0 0}}" +
				$"  *Line halo {col.Alpha(0.25f)} {{0 0 0 1 0 0 *Width {{{GripperHeight * 0.75f}}} *Hidden }}" +
				$"  *Font{{*Name{{\"tahoma\"}} *Size{{10}} *Weight{{500}} *Colour{{FFFFFFFF}}}}" +
				$"  *Text price {{ \"{price}\" *Billboard *Anchor {{+1 0}} *BackColour {{{col}}} *o2w{{*pos{{1 0 0}}}} *NoZTest }}" +
				$"}}";

			Gfx = new View3d.Object(ldr, false, Id, null);
		}

		/// <summary>Add the graphics to the chart</summary>
		protected override void UpdateSceneCore(View3d.Window window)
		{
			base.UpdateSceneCore(window);
			if (Gfx == null)
				return;

			if (Visible)
			{
				Gfx.Child("halo").Visible = Hovered;
				Gfx.O2P =
					m4x4.Translation((float)Chart.XAxis.Min, (float)(decimal)PriceQ2B, CandleChart.ZOrder.Indicators) *
					m4x4.Scale((float)Chart.XAxis.Span, 1f, 1f, v4.Origin);

				window.AddObject(Gfx);
			}
			else
			{
				window.RemoveObject(Gfx);
			}
		}

		/// <summary>Hit test the trade price indicator</summary>
		public override ChartControl.HitTestResult.Hit HitTest(Point chart_point, Point client_point, ModifierKeys modifier_keys, View3d.Camera cam)
		{
			// Find the nearest point to 'client_point' on the line
			var chart_pt = chart_point;
			var client_pt = Chart.ChartToClient(new Point(chart_pt.X, (float)(decimal)PriceQ2B));

			// If the point is within tolerance of the gripper
			if (Math_.Frac(Chart.XAxis.Min, chart_pt.X, Chart.XAxis.Max) > 1.0f - GripperWidthFrac &&
				Math.Abs(client_pt.Y - client_point.Y) < GripperHeight)
				return new ChartControl.HitTestResult.Hit(this, new Point(chart_pt.X, (float)(decimal)PriceQ2B), null);

			return null;
		}

		/// <summary>Drag the indicator to change the price</summary>
		private class DragPrice : ChartControl.MouseOp
		{
			private readonly TradeIndicator m_trade_indicator;
			public DragPrice(TradeIndicator trade_indicator)
				: base(trade_indicator.Chart)
			{
				m_trade_indicator = trade_indicator;
			}
			public override void MouseMove(MouseEventArgs e)
			{
				var chart_pt = Chart.ClientToChart(e.GetPosition(Chart));
				m_trade_indicator.PriceQ2B = ((decimal)chart_pt.Y)._(m_trade_indicator.PriceQ2B);
				base.MouseMove(e);
			}
		}
	}
}

