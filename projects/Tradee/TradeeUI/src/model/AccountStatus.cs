using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using pr.container;
using pr.extn;

namespace Tradee
{
	public class AccountStatus :INotifyPropertyChanged
	{
		public AccountStatus(MainModel model)
		{
			Model = model;
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Raised on property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void SetProp<T>(ref T prop, T value, params string[] prop_names)
		{
			if (Equals(prop, value)) return;
			prop = value;

			// Raise notification for each affected property
			foreach (var name in prop_names)
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>Account Id</summary>
		public string AccountId
		{
			get { return m_account_id ?? string.Empty; }
			set { SetProp(ref m_account_id, value, nameof(Account.AccountId)); }
		}
		private string m_account_id;

		/// <summary>True if this account uses real money</summary>
		public bool IsLive
		{
			get { return m_is_live; }
			set { SetProp(ref m_is_live, value, nameof(IsLive)); }
		}
		private bool m_is_live;

		/// <summary>Currency of the account</summary>
		public string Currency
		{
			get { return m_currency ?? string.Empty; }
			set { SetProp(ref m_currency, value, nameof(Currency)); }
		}
		private string m_currency;

		/// <summary>Account balance (in units of Currency)</summary>
		public double Balance
		{
			get { return m_balance; }
			set { SetProp(ref m_balance, value, nameof(Balance)); }
		}
		private double m_balance;

		/// <summary>Unrealised account balance</summary>
		public double Equity
		{
			get { return m_equity; }
			set { SetProp(ref m_equity, value, nameof(Equity)); }
		}
		private double m_equity;


	
			///// <summary>The free margin of the current account.</summary>
			//public double FreeMargin { get; set; }

			///// <summary>The account leverage</summary>
			//public int Leverage { get; set; }

			///// <summary>Represents the margin of the current account.</summary>
			//public double Margin { get; set; }

			///// <summary>
			///// Represents the margin level of the current account.
			///// Margin level (in %) is calculated using this formula: Equity / Margin * 100.</summary>
			//public double? MarginLevel { get; set; }

			///// <summary>Unrealised gross profit</summary>
			//public double UnrealizedGrossProfit { get; set; }

			///// <summary>Unrealised net profit</summary>
			//public double UnrealizedNetProfit { get; set; }

		/// <summary>The total risk due to active and pending orders</summary>
		public double TotalRisk
		{
			get { return CurrentRisk + PendingRisk; }
		}

		/// <summary>The risk due to open positions</summary>
		public double CurrentRisk
		{
			get { return m_position_risk; }
			set { SetProp(ref m_position_risk, value, nameof(CurrentRisk), nameof(TotalRisk)); }
		}
		private double m_position_risk;

		/// <summary>The risk due to pending orders</summary>
		public double PendingRisk
		{
			get { return m_pending_risk; }
			set { SetProp(ref m_pending_risk, value, nameof(PendingRisk), nameof(TotalRisk)); }
		}
		private double m_pending_risk;

		/// <summary>The total risk due to active and pending orders (as a percent of balance)</summary>
		public double TotalRiskPC
		{
			get { return 100.0 * TotalRisk / Balance; }
		}

		/// <summary>The risk due to open positions (as a percent of balance)</summary>
		public double CurrentRiskPC
		{
			get { return 100.0 * CurrentRisk / Balance; }
		}

		/// <summary>The risk due to pending orders (as a percent of balance)</summary>
		public double PendingRiskPC
		{
			get { return 100.0 * PendingRisk / Balance; }
		}


		/// <summary>Update the state of the account from received account info</summary>
		public void Update(Account acct)
		{
			AccountId = acct.AccountId;
			IsLive    = acct.IsLive;
			Currency  = acct.Currency;
			Balance   = acct.Balance;
			Equity    = acct.Equity;
		}

		/// <summary>Update the risk level from currently active positions</summary>
		public void Update(Position[] positions)
		{
			// Add up all the potential losses from current positions
			var risk = 0.0;
			foreach (var position in positions)
			{
				// The stop loss will be negative if it is on the 'win' side
				// of the entry price, regardless of trade type.
				var pd = Model.MarketData[position.SymbolCode].PriceData;
				var sl = position.StopLossRel != 0 ? position.StopLossRel : position.EntryPrice; // assume SL at $0 if none set
				risk += (position.StopLossRel / pd.PipSize) * pd.PipValue * position.Volume;
			}

			CurrentRisk = risk;
		}

		/// <summary>Update the risk level from pending orders positions</summary>
		public void Update(PendingOrder[] orders)
		{
			// Add up all potential losses from pending orders
			var risk = 0.0;
			foreach (var order in orders)
			{
				// The stop loss will be negative if it is on the 'win' side
				// of the entry price, regardless of trade type.
				var pd = Model.MarketData[order.SymbolCode].PriceData;
				var sl = order.StopLossRel != 0 ? order.StopLossRel : order.EntryPrice; // assume SL at $0 if none set
				risk += (order.StopLossRel / pd.PipSize) * pd.PipValue * order.Volume;
			}

			PendingRisk = risk;
		}
	}
}
