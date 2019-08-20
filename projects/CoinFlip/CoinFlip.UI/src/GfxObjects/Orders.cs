using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.GfxObjects
{
	public class Orders : IDisposable
	{
		// Notes:
		//  - A manager class for adding graphics to a chart for completed open orders

		private Func<IOrder, double> m_xvalue;
		private Func<IOrder, double> m_yvalue;
		private Dictionary<long, OrderGfx> m_gfx;

		public Orders(Func<IOrder, double> xvalue, Func<IOrder, double> yvalue)
		{
			m_xvalue = xvalue;
			m_yvalue = yvalue;
			m_gfx = new Dictionary<long, OrderGfx>();
		}
		public void Dispose()
		{
			ClearScene();
		}

		/// <summary>Remove all order graphics from the scene</summary>
		public void ClearScene()
		{
			foreach (var gfx in m_gfx)
				gfx.Value.Dispose();

			m_gfx.Clear();
		}

		/// <summary>Update the scene for the given orders</summary>
		public void BuildScene(IEnumerable<IOrder> orders, IEnumerable<IOrder> highlighted, ChartControl chart)
		{
			var in_scene = orders.ToHashSet(x => x.OrderId);
			var highlight = highlighted?.ToHashSet(x => x.OrderId) ?? new HashSet<long>();

			// Remove all gfx no longer in the scene
			var remove = m_gfx.Where(kv => !in_scene.Contains(kv.Key)).ToList();
			foreach (var gfx in remove)
			{
				m_gfx.Remove(gfx.Key);
				gfx.Value.Dispose();
			}

			// Add graphics objects for each order in the scene
			foreach (var order in orders)
			{
				// Get the associated graphics objects
				var gfx = m_gfx.GetOrAdd(order.OrderId, k => new OrderGfx(order));

				// Update the position
				var x = m_xvalue(order);
				var y = m_yvalue(order);
				var s = chart.ChartToClient(new Point(x, y));
				var hl = highlight.Contains(order.OrderId);
				gfx.Update(chart.Overlay, s, hl);
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
			public void Update(Canvas overlay, Point s, bool highlight)
			{
				overlay.Adopt(Mark);
				Mark.RenderTransform = new MatrixTransform(1, 0, 0, 1, s.X, s.Y);

				overlay.Adopt(Label);
				Label.Visibility = SettingsData.Settings.Chart.ShowTradeDescriptions ? Visibility.Visible : Visibility.Collapsed;
				Label.FontSize = SettingsData.Settings.Chart.TradeLabelSize;
				Label.FontWeight = highlight ? FontWeights.Bold : FontWeights.Normal;
				Label.Measure(Rylogic.Extn.Windows.Size_.Infinity);
				Label.Background = new SolidColorBrush(Colour32.White.Alpha(1.0f - (float)SettingsData.Settings.Chart.TradeLabelTransparency).ToMediaColor());
				Label.RenderTransform = SettingsData.Settings.Chart.LabelsToTheLeft
					? new MatrixTransform(1, 0, 0, 1, s.X - Label.DesiredSize.Width - 7.0, s.Y - Label.DesiredSize.Height/2)
					: new MatrixTransform(1, 0, 0, 1, s.X + 7.0, s.Y - Label.DesiredSize.Height / 2);
			}
		}
	}
}
