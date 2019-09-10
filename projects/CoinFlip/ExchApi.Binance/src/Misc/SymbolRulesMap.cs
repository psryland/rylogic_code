using System.Collections.Generic;
using Binance.API.DomainObjects;

namespace Binance.API
{
	public class SymbolRulesMap
	{
		private readonly Dictionary<CurrencyPair, ServerRulesData.SymbolData> m_map;
		public SymbolRulesMap()
		{
			m_map = new Dictionary<CurrencyPair, ServerRulesData.SymbolData>();
		}
		public ServerRulesData.SymbolData this[CurrencyPair pair]
		{
			get => m_map.TryGetValue(pair, out var rules) ? rules : null;
			set => m_map[pair] = value;
		}
	}
}
