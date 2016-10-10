using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using pr.extn;

namespace Rylobot
{
	public class StrategyPotLuck :Strategy
	{
		private const string Label = "StrategyPotLuck";

		private Random m_rng;
		private Position m_position;
		
		public StrategyPotLuck(RylobotModel model)
			:base(model)
		{
			m_rng = new Random();

			// Look for any existing trades created by this strategy
			m_position = Model.Robot.Positions.FirstOrDefault(x => x.Label == Label);
		}

		private void CreateOrder()
		{
			if (m_position != null)
				throw new Exception("Order already exists");

			var tt = m_rng.Float() > 0.5f ? TradeType.Buy : TradeType.Sell;
			var instr = Model.Instrument;
			var r = Model.Robot.ExecuteMarketOrder(tt, instr.Symbol, 1000, Label, 100.0, 50.0);
			if (r.IsSuccessful)
			{
				m_position = r.Position;
			}
			else
			{
				Model.Robot.Print("Create order failed: {0}".Fmt(r.Error));
			}
		}

		public override void Step()
		{
			// No current position? create one
			if (m_position == null)
				CreateOrder();
		}

		/// <summary>Called when a position closes</summary>
		protected override void HandlePositionClosed(object sender, PositionEventArgs e)
		{
			base.HandlePositionClosed(sender, e);
			if (m_position != null && m_position.Id == e.Position.Id)
			{
				m_position = null;
				CreateOrder();
			}
		}
	}
}
