using System.Diagnostics;

namespace CoinFlip
{
	public class TransfersCollection : CollectionBase<string, Transfer>
	{
		public TransfersCollection(Exchange exch)
			: base(exch)
		{
			KeyFrom = x => x.TransactionId;
		}
		public TransfersCollection(TransfersCollection rhs)
			: base(rhs)
		{ }

		/// <summary>Get/Set a history entry by order id. Returns null if 'key' is not in the collection</summary>
		public override Transfer this[string key]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return TryGetValue(key, out var txfr) ? txfr : null;
			}
			set
			{
				Debug.Assert(Misc.AssertMainThread());
				base[key] = value;
			}
		}
	}
}



