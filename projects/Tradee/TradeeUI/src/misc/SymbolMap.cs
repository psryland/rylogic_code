using System.Collections.Generic;

namespace Tradee
{
	/// <summary>A data type associated with a single symbol</summary>
	public interface ISymbolData
	{
		string Symbol { get; }
	}

	/// <summary>A map that lazily creates entries on request</summary>
	public abstract class SymbolMap<ISymbolData> :Dictionary<string, ISymbolData>
	{
		/// <summary>Get the 'Value' associated with 'sym'</summary>
		public new ISymbolData this[string sym]
		{
			get
			{
				ISymbolData value;
				if (!TryGetValue(sym, out value))
				{
					value = FactoryNew(sym);
					Add(sym, value);
				}
				return value;
			}
		}
		protected abstract ISymbolData FactoryNew(string sym);
	}
}
