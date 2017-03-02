using pr.maths;

namespace Rylobot
{
	public class FervourData
	{
		public FervourData()
		{
			BuyPeriod = new Avr();
			SellPeriod = new Avr();
		}

		/// <summary>The average/total length of time until a buy occurred</summary>
		public Avr BuyPeriod { get; private set; }

		/// <summary>The average/total length of time until a sell occurred</summary>
		public Avr SellPeriod { get; private set; }

		/// <summary>The normalised ratio of buys to sells. Values in the range [-1,+1]</summary>
		public double TotalRatio
		{
			get
			{
				var buy_sum = BuyPeriod.Sum;
				var sel_sum = SellPeriod.Sum;
				var total = buy_sum + sel_sum;
				return !Maths.FEql(total, 0) ? (buy_sum - sel_sum) / total : 0;
			}
		}

		/// <summary>The normalised ratio of average buy and sell periods. Values in the range [-1,+1]</summary>
		public double AvrRatio
		{
			get
			{
				var buy_avr = BuyPeriod.Mean;
				var sel_avr = SellPeriod.Mean;
				var total = buy_avr + sel_avr;
				return !Maths.FEql(total, 0) ? (buy_avr - sel_avr) / total : 0;
			}
		}
	}
}
