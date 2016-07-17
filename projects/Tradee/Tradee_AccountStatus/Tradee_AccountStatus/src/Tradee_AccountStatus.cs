using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using pr.util;
using Tradee;

namespace cAlgo
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
	public class Tradee_AccountStatus :Robot
	{
		[Parameter(DefaultValue = 0.0)]
		public double Parameter { get; set; }

		#region Robot Implementation
		protected override void OnStart()
		{
			// Connect to 'Tradee'
			Tradee = new TradeeProxy();
			UpdateAccountStatus();
		}
		protected override void OnTick()
		{ }
		protected override void OnStop()
		{
			Tradee = null;
		}
		#endregion

		/// <summary>The connection to the Tradee application</summary>
		private TradeeProxy Tradee
		{
			get { return m_tradee; }
			set
			{
				if (m_tradee == value) return;
				if (m_tradee != null) Util.Dispose(ref m_tradee);
				m_tradee = value;
			}
		}
		private TradeeProxy m_tradee;

		/// <summary>Update total risk</summary>
		private void UpdateAccountStatus()
		{
			var acct = new AccountStatus();
			acct.Balance               = Account.Balance               ;
			acct.BrokerName            = Account.BrokerName            ;
			acct.Currency              = Account.Currency              ;
			acct.Equity                = Account.Equity                ;
			acct.FreeMargin            = Account.FreeMargin            ;
			acct.IsLive                = Account.IsLive                ;
			acct.Leverage              = Account.Leverage              ;
			acct.Margin                = Account.Margin                ;
			acct.MarginLevel           = Account.MarginLevel           ;
			acct.Number                = Account.Number                ;
			acct.UnrealizedGrossProfit = Account.UnrealizedGrossProfit ;
			acct.UnrealizedNetProfit   = Account.UnrealizedNetProfit   ;

			// Add up all the potential losses from current positions
			foreach (var position in Positions)
			{
				var symbol = MarketData.GetSymbol(position.SymbolCode);
				if (position.StopLoss == null)
					continue;

				var pips = (position.EntryPrice - position.StopLoss.Value) * position.Volume;
				acct.PositionRisk += pips * symbol.PipValue / symbol.PipSize;
			}

			// Add up all potential losses from orders
			foreach (var order in PendingOrders)
			{
				var symbol = MarketData.GetSymbol(order.SymbolCode);
				if (order.StopLoss == null)
					continue;

				var pips = order.TradeType == TradeType.Buy ? (order.TargetPrice - order.StopLoss.Value) * order.Volume : (order.StopLoss.Value - order.TargetPrice) * order.Volume;
				acct.OrderRisk += pips * symbol.PipValue / symbol.PipSize;
			}

			Tradee.Post(acct);
		}
	}
}
