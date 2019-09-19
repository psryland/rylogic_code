using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

		private Dictionary<long, OrderGfx> m_gfx;

		public Orders(Func<IOrder, double> xvalue, Func<IOrder, double> yvalue)
		{
			XValue = xvalue;
			YValue = yvalue;
			m_gfx = new Dictionary<long, OrderGfx>();
		}
		public void Dispose()
		{
			ClearScene();
		}

		/// <summary>Callback functions used to position each order</summary>
		public Func<IOrder, double> XValue { get; set; }
		public Func<IOrder, double> YValue { get; set; }

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
				var gfx = m_gfx.GetOrAdd(order.OrderId, k => new OrderGfx(order, this));

				// Update the position
				var x = XValue(order);
				var y = YValue(order);
				var s = chart.ChartToClient(new Point(x, y));
				var hl = highlight.Contains(order.OrderId);
				gfx.Update(chart.Overlay, s, hl);
			}
		}

		/// <summary>Interaction events on the orders</summary>
		public event EventHandler<OrderEventArgs> OrderSelected;
		private void NotifyOrderSelected(IOrder order)
		{
			OrderSelected?.Invoke(this, new OrderEventArgs(order));
		}

		/// <summary>Interaction events on the orders</summary>
		public event EventHandler<OrderEventArgs> EditOrder;
		private void NotifyEditOrder(IOrder order)
		{
			EditOrder?.Invoke(this, new OrderEventArgs(order));
		}

		/// <summary>Graphics for a single collected order</summary>
		private class OrderGfx :IDisposable
		{
			private readonly Orders m_orders;
			private readonly IOrder m_order;
			public OrderGfx(IOrder order, Orders owner)
			{
				m_orders = owner;
				m_order = order;

				Mark = new Polygon
				{
					Stroke = Brushes.Black,
					Points = TradeType == ETradeType.Q2B
						? new PointCollection(new[] { new Point(0, 0), new Point(-5, +5), new Point(+5, +5) })
						: new PointCollection(new[] { new Point(0, 0), new Point(-5, -5), new Point(+5, -5) }),
				};
				Label = new TextBlock
				{
					Text = order.Description,
					FontSize = 8.0,
				};

				Mark.MouseLeftButtonDown += HandleMouseLeftButtonDown;
			}
			public void Dispose()
			{
				Mark.Detach();
				Label.Detach();
			}

			/// <summary>The ID of the order these graphics objects are for</summary>
			public long OrderId => m_order.OrderId;

			/// <summary></summary>
			public ETradeType TradeType => m_order.TradeType;

			/// <summary>The triangle marker</summary>
			private Polygon Mark { get; }

			/// <summary>A text label</summary>
			private TextBlock Label { get; }

			/// <summary>Resize and reposition the graphics</summary>
			public void Update(Canvas overlay, Point s, bool highlight)
			{
				var col = TradeType == ETradeType.Q2B
					? new SolidColorBrush(SettingsData.Settings.Chart.Q2BColour.Darken(0.5f).ToMediaColor())
					: new SolidColorBrush(SettingsData.Settings.Chart.B2QColour.Darken(0.5f).ToMediaColor());

				Mark.Fill = col;
				Mark.RenderTransform = new MatrixTransform(1, 0, 0, 1, s.X, s.Y);
				overlay.Adopt(Mark);

				Label.Foreground = col;
				Label.Visibility = SettingsData.Settings.Chart.ConfettiDescriptionsVisible ? Visibility.Visible : Visibility.Collapsed;
				Label.FontSize = SettingsData.Settings.Chart.ConfettiLabelSize;
				Label.FontWeight = highlight ? FontWeights.Bold : FontWeights.Normal;
				Label.Measure(Rylogic.Extn.Windows.Size_.Infinity);
				Label.Background = new SolidColorBrush(Colour32.White.Alpha(1.0f - (float)SettingsData.Settings.Chart.ConfettiLabelTransparency).ToMediaColor());
				Label.RenderTransform = SettingsData.Settings.Chart.ConfettiLabelsToTheLeft
					? new MatrixTransform(1, 0, 0, 1, s.X - Label.DesiredSize.Width - 7.0, s.Y - Label.DesiredSize.Height/2)
					: new MatrixTransform(1, 0, 0, 1, s.X + 7.0, s.Y - Label.DesiredSize.Height / 2);
				overlay.Adopt(Label);
			}

			/// <summary>Handle selection and double clicks</summary>
			private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
			{
				if (e.ClickCount == 2)
					m_orders.NotifyEditOrder(m_order);
				else if (e.ClickCount == 1)
					m_orders.NotifyOrderSelected(m_order);
			}
		}

		/// <summary>Event args for 'OrderGfx' interactions</summary>
		public class OrderEventArgs :EventArgs
		{
			public OrderEventArgs(IOrder order)
			{
				Order = order;
			}

			/// <summary>The selected order</summary>
			public IOrder Order { get; }
		}
	}
}
