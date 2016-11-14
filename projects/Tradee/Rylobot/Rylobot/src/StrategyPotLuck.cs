using System;
using cAlgo.API;
using pr.extn;

namespace Rylobot
{
	/// <summary>Random chance</summary>
	public class StrategyPotLuck :Strategy
	{
		private Random m_rng;
		
		public StrategyPotLuck(Rylobot bot)
			:base(bot, "StrategyPotLuck")
		{
			m_rng = new Random(1);
		}

		/// <summary>The position managed by this strategy</summary>
		public Position Position
		{
			get { return m_position; }
			protected set
			{
				if (m_position == value) return;
				m_position = value;
			}
		}
		private Position m_position;

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			// No current position? create one
			Position = FindLivePosition(Position);
			if (Position == null)
				CreateOrder();
		}

		/// <summary>Called then the current position closes</summary>
		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);

			// Update the account
			Bot.Broker.Update();

			// If the position was a win, create another one the same, if not, try opposite
			var tt = position.GrossProfit > 0 ? position.TradeType : position.TradeType.Opposite();
			CreateOrder(tt);
		}

		/// <summary>Create a random trade</summary>
		private void CreateOrder(TradeType? preferred = null)
		{
			if (Position != null)
				throw new Exception("Position already exists");

			// Choose a trade type
			var tt = preferred ?? (m_rng.Float() > 0.5f ? TradeType.Buy : TradeType.Sell);
			var trade = new Trade(Bot, Instrument, tt, Label);
			Position = Bot.Broker.CreateOrder(trade);
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public static double FitnessScore(Rylobot bot)
		{
			return 0.01; // never a good idea...
		}
	}
}
