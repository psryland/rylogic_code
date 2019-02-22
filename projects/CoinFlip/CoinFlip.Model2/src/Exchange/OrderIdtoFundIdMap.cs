using System.Collections.Generic;

namespace CoinFlip
{
	public class OrderIdtoFundIdMap
	{
		private readonly Dictionary<long, string> m_map;
		public OrderIdtoFundIdMap()
		{
			m_map = new Dictionary<long, string>();
		}
		public string this[long order_id]
		{
			get { return m_map.TryGetValue(order_id, out var ctx_id) ? ctx_id : Fund.Main; }
			set { m_map[order_id] = value; }
		}
	}
}



