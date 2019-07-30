using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	public class Orders : IDisposable
	{
		// Notes:
		//  - A manager class for adding graphics to a chart for completed open orders

		private Func<IOrder, double> m_xvalue;
		private Func<IOrder, double> m_yvalue;
		private Cache<long, OrderGfx> m_cache;

		public Orders(Func<IOrder, double> xvalue, Func<IOrder, double> yvalue)
		{
			m_xvalue = xvalue;
			m_yvalue = yvalue;
			m_cache = new Cache<long, OrderGfx>(int.MaxValue);
		}
		public void Dispose()
		{
			Util.Dispose(ref m_cache);
		}

		/// <summary>Update the scene for the given orders</summary>
		public void BuildScene(IEnumerable<IOrder> orders, ChartControl chart, Canvas overlay)
		{
			// Remove all gfx no longer in the scene
			var in_scene = orders.ToHashSet(x => x.OrderId);
			foreach (var gfx in m_cache.CachedItems.Where(x => !in_scene.Contains(x.OrderId)).ToList())
				m_cache.Invalidate(gfx.OrderId);

			// Add graphics objects for each order
			foreach (var order in orders)
			{
				// Get the associated graphics objects
				var gfx = m_cache.Get(order.OrderId, k => new OrderGfx(order));

				// Update the position
				var x = m_xvalue(order);
				var y = m_yvalue(order);
				var s = chart.ChartToClient(new Point(x, y));
				gfx.Update(overlay, s);
			}
		}

		/// <summary>Graphics for a single collected order</summary>
		private class OrderGfx :IDisposable
		{
			public OrderGfx(IOrder order)
			{
				OrderId = order.OrderId;
				TradeType = order.TradeType;

				var col = TradeType == ETradeType.Q2B
					? new SolidColorBrush(SettingsData.Settings.Chart.Q2BColour.Darken(0.5f).ToMediaColor())
					: new SolidColorBrush(SettingsData.Settings.Chart.B2QColour.Darken(0.5f).ToMediaColor());
				Mark = new Polygon
				{
					Fill = col,
					Stroke = Brushes.Black,
					Points = TradeType == ETradeType.Q2B
						? new PointCollection(new[] { new Point(0, 0), new Point(-5, +5), new Point(+5, +5) })
						: new PointCollection(new[] { new Point(0, 0), new Point(-5, -5), new Point(+5, -5) }),
				};
				Label = new TextBlock
				{
					Text = order.Description,
					Foreground = col,
					FontSize = 8.0,
				};
			}
			public void Dispose()
			{
				Mark = null;
				Label = null;
			}

			/// <summary>The ID of the order these graphics objects are for</summary>
			public long OrderId { get; }

			/// <summary></summary>
			public ETradeType TradeType { get; }

			/// <summary>The triangle marker</summary>
			public Polygon Mark
			{
				get => m_mark;
				private set
				{
					if (m_mark == value) return;
					if (m_mark?.Parent is Canvas parent)
						parent.Children.Remove(m_mark);
					m_mark = value;
				}
			}
			private Polygon m_mark;

			/// <summary>A text label</summary>
			public TextBlock Label
			{
				get => m_label;
				private set
				{
					if (m_label == value) return;
					if (m_label?.Parent is Canvas parent)
						parent.Children.Remove(m_label);
					m_label = value;
				}
			}
			private TextBlock m_label;

			/// <summary>Resize and reposition the graphics</summary>
			public void Update(Canvas overlay, Point s)
			{
				Misc.AddToOverlay(Mark, overlay);
				Mark.RenderTransform = new MatrixTransform(1.0, 0.0, 0.0, 1.0, s.X, s.Y);

				Misc.AddToOverlay(Label, overlay);
				Label.Visibility = SettingsData.Settings.Chart.ShowTradeDescriptions ? Visibility.Visible : Visibility.Collapsed;
				Label.FontSize = SettingsData.Settings.Chart.TradeLabelSize;
				Label.Measure(Rylogic.Extn.Windows.Size_.Infinity);
				Label.Background = new SolidColorBrush(Colour32.White.Alpha(1.0f - (float)SettingsData.Settings.Chart.TradeLabelTransparency).ToMediaColor());
				Label.RenderTransform = new MatrixTransform(1.0, 0.0, 0.0, 1.0, s.X - (7.0 + Label.DesiredSize.Width), s.Y + -2.5);
					//s.X + (TradeType == ETradeType.Q2B ? -(7.0+Label.ActualWidth) : +7.0),
					//s.Y + (TradeType == ETradeType.Q2B ? -2.5 : -(Label.ActualHeight-2.5)));
			}
		}
	}
}
