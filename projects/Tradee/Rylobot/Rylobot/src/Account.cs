using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.extn;
using pr.util;

namespace Rylobot
{
	public class Account :INotifyPropertyChanged ,IDisposable
	{
		public Account(RylobotModel model, IAccount acct)
		{
			Model = model;
			Update(acct);
		}
		public void Dispose()
		{
			Model = null;
		}

		/// <summary>The App logic</summary>
		public RylobotModel Model
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Raised when the AccountId is about to change</summary>
		public event EventHandler AccountChanging;

		/// <summary>Raised after the AccountId has changed</summary>
		public event EventHandler AccountChanged;

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
		public int AccountId
		{
			get { return m_account_id; }
			set { SetProp(ref m_account_id, value, R<Account>.Name(x => x.AccountId)); }
		}
		private int m_account_id;

		/// <summary>True if this account uses real money</summary>
		public bool IsLive
		{
			get { return m_is_live; }
			set { SetProp(ref m_is_live, value, R<Account>.Name(x => x.IsLive)); }
		}
		private bool m_is_live;

		/// <summary>Currency of the account</summary>
		public string Currency
		{
			get { return m_currency ?? string.Empty; }
			set { SetProp(ref m_currency, value, R<Account>.Name(x => x.Currency)); }
		}
		private string m_currency;

		/// <summary>Account balance (in units of Currency)</summary>
		public double Balance
		{
			get { return m_balance; }
			set { SetProp(ref m_balance, value, R<Account>.Name(x => x.Balance)); }
		}
		private double m_balance;

		/// <summary>Unrealised account balance</summary>
		public double Equity
		{
			get { return m_equity; }
			set { SetProp(ref m_equity, value, R<Account>.Name(x => x.Equity)); }
		}
		private double m_equity;

		/// <summary>The account leverage</summary>
		public int Leverage
		{
			get { return m_leverage; }
			set { SetProp(ref m_leverage, value, R<Account>.Name(x => x.Leverage)); }
		}
		private int m_leverage;

	
			///// <summary>The free margin of the current account.</summary>
			//public double FreeMargin { get; set; }


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
			set { SetProp(ref m_position_risk, value, R<Account>.Name(x => x.CurrentRisk), R<Account>.Name(x => x.TotalRisk)); }
		}
		private double m_position_risk;

		/// <summary>The risk due to pending orders</summary>
		public double PendingRisk
		{
			get { return m_pending_risk; }
			set { SetProp(ref m_pending_risk, value, R<Account>.Name(x => x.PendingRisk), R<Account>.Name(x => x.TotalRisk)); }
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

		/// <summary>Update the state of the account</summary>
		public void Update()
		{
			Update(Model.Robot.Account);
			Update(Model.Robot.Positions);
			Update(Model.Robot.PendingOrders);
		}

		/// <summary>Update the state of the account from received account info</summary>
		private void Update(IAccount acct)
		{
			var acct_change = AccountId != acct.Number;
			if (acct_change)
				AccountChanging.Raise(this);

			AccountId = acct.Number;
			IsLive    = acct.IsLive;
			Currency  = acct.Currency;
			Balance   = acct.Balance;
			Equity    = acct.Equity;
			Leverage  = acct.Leverage;

			if (acct_change)
				AccountChanged.Raise(this);
		}

		/// <summary>Update the risk level from currently active positions</summary>
		private void Update(Positions positions)
		{
			// Add up all the potential losses from current positions
			var risk = 0.0;
			foreach (var position in positions)
			{
				// The stop loss will be negative if it is on the 'win' side
				// of the entry price, regardless of trade type.
				var sym = Model.GetSymbol(position.SymbolCode);
				var sl = position.StopLossRel() != 0 ? position.StopLossRel() : position.EntryPrice; // assume SL at $0 if none set
				risk += (position.StopLossRel() / sym.PipSize) * sym.PipValue * position.Volume;
			}

			// Update the current risk from active positions
			CurrentRisk = risk;
		}

		/// <summary>Update the risk level from pending orders positions</summary>
		private void Update(PendingOrders orders)
		{
			// Add up all potential losses from pending orders
			var risk = 0.0;
			foreach (var order in orders)
			{
				// The stop loss will be negative if it is on the 'win' side
				// of the entry price, regardless of trade type.
				var sym = Model.GetSymbol(order.SymbolCode);
				var sl = order.StopLossRel() != 0 ? order.StopLossRel() : order.TargetPrice; // assume SL at $0 if none set
				risk += (order.StopLossRel() / sym.PipSize) * sym.PipValue * order.Volume;
			}

			PendingRisk = risk;
		}
	}
}
