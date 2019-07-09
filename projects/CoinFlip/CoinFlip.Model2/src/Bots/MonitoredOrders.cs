using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip.Bots
{
	public class MonitoredOrders :SettingsSet<MonitoredOrders>
	{
		// Notes:
		//  - This is a setting helper class for persisting information
		//    about created orders across restarts.
		//  - Intended usage is;
		//      - Bots include an instance of this type in their settings type.
		//      - On start up, Bots register the monitor with the model. Whenever
		//        a filled position is detected....

		public MonitoredOrders()
		{
			Orders = new PersistedOrder[0];
		}

		/// <summary>Details of orders that are persisted to settings</summary>
		public PersistedOrder[] Orders
		{
			get { return get<PersistedOrder[]>(nameof(Orders)); }
			set { set(nameof(Orders), value); }
		}

		/// <summary>Register this monitor with the model to monitor completed orders</summary>
		public IDisposable Register(Model model)
		{
			return Scope.Create(
				() =>
				{
					// Attach a handler to the collection changed event for each exchange's orders collection
					foreach (var exch in model.TradingExchanges)
					{
						if (exch.TradeHistoryUseful)
						{
							exch.History.ItemChanged += TriggerUpdate;
							exch.History.CollectionChanged += TriggerUpdate;
						}
						else
						{
							exch.Orders.ItemChanged += TriggerUpdate;
							exch.Orders.CollectionChanged += TriggerUpdate;
						}
					}
				},
				() =>
				{
					// Detach the handler on the collection changed event for each exchange's orders collection
					foreach (var exch in model.TradingExchanges)
					{
						if (exch.TradeHistoryUseful)
						{
							exch.History.ItemChanged -= TriggerUpdate;
							exch.History.CollectionChanged -= TriggerUpdate;
						}
						else
						{
							exch.Orders.ItemChanged -= TriggerUpdate;
							exch.Orders.CollectionChanged -= TriggerUpdate;
						}
					}
				});

			// Handle notification that the History or Orders collections on an exchange has changed
			void TriggerUpdate(object sender, EventArgs e)
			{
				if (sender is OrdersCompletedCollection h)
					CheckForCompletedOrders(h.Exchange);
				if (sender is OrdersCollection o)
					CheckForCompletedOrders(o.Exchange);
			}
		}

		/// <summary>The number of orders not known to be filled</summary>
		public int Count => Orders.Length;

		/// <summary>Add the result of a created trade</summary>
		public void Add(OrderResult order_result)
		{
			// If the order was immediately filled, there's no need to persist it. Signal OrderCompleted immediately.
			if (order_result.Filled)
				OrderCompleted?.Invoke(this, new MonitoredOrderEventArgs(order_result.Pair.Exchange, order_result.OrderId));

			// Otherwise, add the order to the persisted collection
			else
				Orders = Orders.Append(new PersistedOrder(order_result)).ToArray();
		}

		/// <summary>Look for pending orders that have completed</summary>
		private void CheckForCompletedOrders(Exchange exchange)
		{
			var remove = new List<PersistedOrder>();
			foreach (var order in Orders)
			{
				// 'order' is not on 'exchange', keep it.
				if (order.ExchangeName != exchange.Name)
					continue;

				// If 'order' still exists on the exchange, keep it.
				if (exchange.Orders[order.OrderId] != null)
					continue;

				// 'order' is no longer in the exchange's orders collection. It may have been filled or cancelled.
				// If the exchange data allows mapping from order id to completed orders, then we can use the history
				// to spot completed orders. If not, then we have to assume missing means "filled".
				if (exchange.TradeHistoryUseful)
				{
					// If there is a historic trade matching the order id then the order has definitely been filled.
					var his = exchange.History[order.OrderId];
					if (his != null)
						OrderCompleted?.Invoke(this, new MonitoredOrderEventArgs(exchange, order.OrderId));
					else
						OrderCancelled?.Invoke(this, new MonitoredOrderEventArgs(exchange, order.OrderId));
				}
				else
				{
					// Trade has gone, assume it was filled
					OrderCompleted?.Invoke(this, new MonitoredOrderEventArgs(exchange, order.OrderId));
				}

				// Stop monitoring it
				remove.Add(order);
			}

			// If some orders where completed or cancelled, update the 'Orders' array
			if (remove.Count != 0)
				Orders = Orders.Where(x => !remove.Contains(x)).ToArray();
		}

		/// <summary>Raised when an order is completed</summary>
		public event EventHandler<MonitoredOrderEventArgs> OrderCompleted;

		/// <summary>Raised when an order is cancelled</summary>
		public event EventHandler<MonitoredOrderEventArgs> OrderCancelled;

		/// <summary>A persistable record of an order result</summary>
		public class PersistedOrder :SettingsXml<PersistedOrder>
		{
			public PersistedOrder()
			{
				ExchangeName = string.Empty;
				OrderId = 0;
			}
			public PersistedOrder(OrderResult order_result)
			{
				ExchangeName = order_result.Pair.Exchange.Name;
				OrderId = order_result.OrderId;
			}
			public PersistedOrder(XElement node)
				: base(node)
			{ }

			/// <summary>The name of the exchange that the order is on</summary>
			public string ExchangeName
			{
				get { return get<string>(nameof(ExchangeName)); }
				set { set(nameof(ExchangeName), value); }
			}

			/// <summary>The exchange's assigned Id of the order</summary>
			public long OrderId
			{
				get { return get<long>(nameof(OrderId)); }
				set { set(nameof(OrderId), value); }
			}
		}
	}

	#region EventArgs
	public class MonitoredOrderEventArgs :EventArgs
	{
		public MonitoredOrderEventArgs(Exchange exchange, long order_id)
		{
			Exchange = exchange;
			OrderId = order_id;
		}

		/// <summary>The exchange that the order was on</summary>
		public Exchange Exchange { get; }

		/// <summary>The ID of the order that was completed/cancelled</summary>
		public long OrderId { get; }
	}
	#endregion
}
