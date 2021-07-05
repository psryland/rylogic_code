using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	/// <summary>Chart graphics representing the trade</summary>
	public class TradeWidget : ChartControl.Element
	{
		public TradeWidget(Trade trade, CandleChart candle_chart)
			: base(Guid.NewGuid(), "Trade")
		{
			Trade = trade;
			CandleChart = candle_chart;

			LabelEP = new TextBlock
			{
			};
			LineEP = new Line
			{
				StrokeThickness = 1,
			};
			GlowEP = new Line
			{
				StrokeThickness = 5,
			};
			LabelEP.Typeface(candle_chart.Chart.XAxisPanel.Typeface, candle_chart.Chart.XAxisPanel.FontSize);
		}
		protected override void Dispose(bool disposing)
		{
			LabelEP.Detach();
			LineEP.Detach();
			GlowEP.Detach();
			Trade = null;
			base.Dispose(disposing);
		}

		/// <summary>The Trade being 'indicated'</summary>
		public Trade Trade
		{
			get => m_trade;
			private set
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

		/// <summary>The candle chart that this widget is on</summary>
		private CandleChart CandleChart { get; }

		/// <summary>The trade type being represented</summary>
		public ETradeType TradeType => Trade.TradeType;

		/// <summary>The price level that the indicator is at</summary>
		public Unit<decimal> PriceQ2B
		{
			get => Trade.PriceQ2B;
			set => Trade.PriceQ2B = value;
		}

		/// <summary>Graphics</summary>
		private TextBlock LabelEP { get; }
		private Line LineEP { get; }
		private Line GlowEP { get; }

		/// <summary>Update the scene</summary>
		protected override void UpdateSceneCore(View3d.Window window, View3d.Camera camera)
		{
			if (Visible)
			{
				var pt0 = Chart.ChartToScene(new v4((float)Chart.XAxis.Min, (float)PriceQ2B.ToDouble(), 0, 1f));
				var pt1 = Chart.ChartToScene(new v4((float)Chart.XAxis.Max, (float)PriceQ2B.ToDouble(), 0, 1f));

				// Colour based on trade direction
				var col =
					TradeType == ETradeType.Q2B ? SettingsData.Settings.Chart.Q2BColour :
					TradeType == ETradeType.B2Q ? SettingsData.Settings.Chart.B2QColour :
					throw new Exception("Unknown trade type");

				// Entry price label
				LabelEP.Text = PriceQ2B.ToString(6, false);
				LabelEP.Background = col.ToMediaBrush();
				LabelEP.Foreground = col.InvertBW().ToMediaBrush();
				LabelEP.Measure(Rylogic.Extn.Windows.Size_.Infinity);
				Canvas.SetLeft(LabelEP, pt1.x - LabelEP.DesiredSize.Width);
				Canvas.SetTop(LabelEP, pt1.y - LabelEP.DesiredSize.Height / 2);
				Chart.Overlay.Adopt(LabelEP);

				// Entry price line
				LineEP.X1 = pt0.x;
				LineEP.Y1 = pt0.y;
				LineEP.X2 = pt1.x - LabelEP.DesiredSize.Width;
				LineEP.Y2 = pt1.y;
				LineEP.Stroke = col.ToMediaBrush();
				Chart.Overlay.Adopt(LineEP);

				// Add the glow if hovered or selected
				if (Hovered || Selected)
				{
					GlowEP.X1 = pt0.x;
					GlowEP.Y1 = pt0.y;
					GlowEP.X2 = pt1.x - LabelEP.DesiredSize.Width;
					GlowEP.Y2 = pt1.y;
					GlowEP.Stroke = col.Alpha(Selected ? 0.25 : 0.15).ToMediaBrush();
					Chart.Overlay.Adopt(GlowEP);
				}
				else
				{
					GlowEP.Detach();
				}
			}
			else
			{
				LabelEP.Detach();
				LineEP.Detach();
				GlowEP.Detach();
			}
		}

		/// <inheritdoc/>
		public override ChartControl.HitTestResult.Hit HitTest(v4 chart_point, v2 scene_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
		{
			if (Chart.IsAncestorOf(LabelEP))
			{
				//todo: This was correct, just needs updating to 3D
				//// Hit if over a price label
				//var pt0 = Chart.TransformToDescendant(LabelEP).Transform(scene_point.ToPointD());
				//if (LabelEP.RenderArea().Contains(pt0))
				//	return new ChartControl.HitTestResult.Hit(this, pt0, null);
				//
				//// Get the price in client space
				//var client_price = Chart.ChartToScene(new v4(chart_point.x, (float)PriceQ2B.ToDouble(), 0, 1));
				//if (Math.Abs(client_price.Y - client_point.Y) < Chart.Options.MinSelectionDistance)
				//	return new ChartControl.HitTestResult.Hit(this, client_price, null);
			}

			return null;
		}

		/// <summary>Support dragging</summary>
		protected override void HandleDragged(ChartControl.ChartDraggedEventArgs args)
		{
			switch (args.State)
			{
				default: throw new Exception($"Unknown drag state: {args.State}");
				case ChartControl.EDragState.Start:
				{
					m_drag_start = PriceQ2B;
					break;
				}
				case ChartControl.EDragState.Dragging:
				{
					PriceQ2B = (m_drag_start + (decimal)args.Delta.y)._(PriceQ2B);
					break;
				}
				case ChartControl.EDragState.Commit:
				{
					break;
				}
				case ChartControl.EDragState.Cancel:
				{
					PriceQ2B = m_drag_start._(PriceQ2B);
					break;
				}
			}
			args.Handled = true;
		}
		private decimal m_drag_start;
	}
}

