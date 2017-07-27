using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptopia.API.Implementation;

namespace Cryptopia.API.DataObjects
{
	public class MarketOrderGroupsRequest : IRequest
	{
		public MarketOrderGroupsRequest(int[] tradePairs, int? orderCount = null)
		{
			TradePairIds = tradePairs;
			OrderCount = orderCount;
		}
		public int[] TradePairIds { get; set; }
		public int? OrderCount { get; set; }
	}
}

