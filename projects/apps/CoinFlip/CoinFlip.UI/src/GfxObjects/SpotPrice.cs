using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using CoinFlip.Settings;
using Rylogic.Extn.Windows;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace CoinFlip.UI.GfxObjects
{
	public class SpotPrices :IDisposable
	{
		public SpotPrices(ChartControl chart)
		{
			B2QLine = new Line
			{
				Stroke = SettingsData.Settings.Chart.B2QColour.ToMediaBrush(),
				IsHitTestVisible = false,
			};
			Q2BLine = new Line
			{
				Stroke = SettingsData.Settings.Chart.Q2BColour.ToMediaBrush(),
				IsHitTestVisible = false,
			};
			B2QPrice = new TextBlock
			{
				Background = SettingsData.Settings.Chart.B2QColour.ToMediaBrush(),
				Foreground = SettingsData.Settings.Chart.B2QColour.InvertBW().ToMediaBrush(),
				IsHitTestVisible = false,
			};
			Q2BPrice = new TextBlock
			{
				Background = SettingsData.Settings.Chart.Q2BColour.ToMediaBrush(),
				Foreground = SettingsData.Settings.Chart.Q2BColour.InvertBW().ToMediaBrush(),
				IsHitTestVisible = false,
			};
			B2QPrice.Typeface(chart.YAxisPanel.Typeface, chart.YAxisPanel.FontSize);
			Q2BPrice.Typeface(chart.YAxisPanel.Typeface, chart.YAxisPanel.FontSize);
		}
		public void Dispose()
		{
		}

		/// <summary>Graphics for the base->quote price line</summary>
		private Line B2QLine { get; }

		/// <summary>Graphics for the quote->base price line</summary>
		private Line Q2BLine { get; }

		/// <summary>A text label for the current spot price</summary>
		public TextBlock B2QPrice { get; }

		/// <summary>A text label for the current spot price</summary>
		public TextBlock Q2BPrice { get; }

		/// <summary>Add the spot price to the chart</summary>
		public void BuildScene(TradePair pair, ChartControl chart)
		{
			// Add b2q first, so green is above red
			var spot_b2q = pair.SpotPrice[ETradeType.B2Q];
			var spot_q2b = pair.SpotPrice[ETradeType.Q2B];

			//if (spot_b2q != null)
			{
				var pt0 = chart.ChartToScene(new v4((float)chart.XAxis.Min, (float)spot_b2q.ToDouble(), 0, 1));
				var pt1 = chart.ChartToScene(new v4((float)chart.XAxis.Max, (float)spot_b2q.ToDouble(), 0, 1));

				// Add the line
				B2QLine.X1 = pt0.x;
				B2QLine.Y1 = pt0.y;
				B2QLine.X2 = pt1.x;
				B2QLine.Y2 = pt1.y;
				chart.Overlay.Adopt(B2QLine);

				// Add the price label
				var pt = chart.TransformToDescendant(chart.YAxisPanel).Transform(pt1.ToPointD());
				Canvas.SetLeft(B2QPrice, 0);
				Canvas.SetTop(B2QPrice, pt.Y - B2QPrice.RenderSize.Height / 2);
				B2QPrice.Text = spot_b2q.ToString(8);
				chart.YAxisPanel.Adopt(B2QPrice);
			}
			//else
			//{
			//	B2QLine.Detach();
			//	B2QPrice.Detach();
			//}

			//if (spot_q2b != null)
			{
				var pt0 = chart.ChartToScene(new v4((float)chart.XAxis.Min, (float)spot_q2b.ToDouble(), 0, 1));
				var pt1 = chart.ChartToScene(new v4((float)chart.XAxis.Max, (float)spot_q2b.ToDouble(), 0, 1));

				// Add the line
				Q2BLine.X1 = pt0.x;
				Q2BLine.Y1 = pt0.y;
				Q2BLine.X2 = pt1.x;
				Q2BLine.Y2 = pt1.y;
				chart.Overlay.Adopt(Q2BLine);

				// Add the price label
				var pt = chart.TransformToDescendant(chart.YAxisPanel).Transform(pt1.ToPointD());
				Canvas.SetLeft(Q2BPrice, 0);
				Canvas.SetTop(Q2BPrice, pt.Y - Q2BPrice.RenderSize.Height / 2);
				Q2BPrice.Text = spot_q2b.ToString(8);
				chart.YAxisPanel.Adopt(Q2BPrice);
			}
			//else
			//{
			//	Q2BLine.Detach();
			//	Q2BPrice.Detach();
			//}
		}
	}
}
