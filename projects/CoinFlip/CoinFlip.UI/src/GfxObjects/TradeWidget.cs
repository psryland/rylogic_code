using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using CoinFlip.Settings;
using Rylogic.Common;
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
			: base(Guid.NewGuid(), m4x4.Identity, "Trade")
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
		public override void Dispose()
		{
			LabelEP.Detach();
			LineEP.Detach();
			GlowEP.Detach();
			Trade = null;
			base.Dispose();
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
		public Unit<double> PriceQ2B
		{
			get => Trade.PriceQ2B;
			set => Trade.PriceQ2B = value;
		}

		/// <summary>Graphics</summary>
		private TextBlock LabelEP { get; }
		private Line LineEP { get; }
		private Line GlowEP { get; }

		/// <summary>Update the scene</summary>
		protected override void UpdateSceneCore()
		{
			if (Visible)
			{
				var pt0 = Chart.ChartToClient(new Point(Chart.XAxis.Min, PriceQ2B));
				var pt1 = Chart.ChartToClient(new Point(Chart.XAxis.Max, PriceQ2B));

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
				Canvas.SetLeft(LabelEP, pt1.X - LabelEP.DesiredSize.Width);
				Canvas.SetTop(LabelEP, pt1.Y - LabelEP.DesiredSize.Height / 2);
				Chart.Overlay.Adopt(LabelEP);

				// Entry price line
				LineEP.X1 = pt0.X;
				LineEP.Y1 = pt0.Y;
				LineEP.X2 = pt1.X - LabelEP.DesiredSize.Width;
				LineEP.Y2 = pt1.Y;
				LineEP.Stroke = col.ToMediaBrush();
				Chart.Overlay.Adopt(LineEP);

				// Add the glow if hovered or selected
				if (Hovered || Selected)
				{
					GlowEP.X1 = pt0.X;
					GlowEP.Y1 = pt0.Y;
					GlowEP.X2 = pt1.X - LabelEP.DesiredSize.Width;
					GlowEP.Y2 = pt1.Y;
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

		/// <summary>Hit test the trade price indicator</summary>
		public override ChartControl.HitTestResult.Hit HitTest(Point chart_point, Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
		{
			if (Chart.IsAncestorOf(LabelEP))
			{
				// Hit if over a price label
				var pt0 = Chart.TransformToDescendant(LabelEP).Transform(client_point);
				if (LabelEP.RenderArea().Contains(pt0))
					return new ChartControl.HitTestResult.Hit(this, pt0, null);

				// Get the price in client space
				var client_price = Chart.ChartToClient(new Point(chart_point.X, PriceQ2B));
				if (Math.Abs(client_price.Y - client_point.Y) < Chart.Options.MinSelectionDistance)
					return new ChartControl.HitTestResult.Hit(this, client_price, null);
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
					PriceQ2B = (m_drag_start + args.Delta.Y)._(PriceQ2B);
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
		private double m_drag_start;
	}
}

