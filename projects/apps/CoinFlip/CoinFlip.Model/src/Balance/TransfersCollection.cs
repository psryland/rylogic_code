using System.Diagnostics;
using Rylogic.Container;

namespace CoinFlip
{
	public class TransfersCollection : CollectionBase<string, Transfer>
	{
		public TransfersCollection(Exchange exch)
			:base(exch, x => x.TransactionId)
		{}

		/// <summary></summary>
		private BindingDict<string, Transfer> Transfers => m_data;

		/// <summary>Reset the collection</summary>
		public void Clear()
		{
			Transfers.Clear();
		}

		/// <summary>Add a transfer to this collection</summary>
		public Transfer AddOrUpdate(Transfer transfer)
		{
			Debug.Assert(Misc.AssertMainThread());
			if (Transfers.TryGetValue(transfer.TransactionId, out var existing))
			{
				existing.Update(transfer);
				Transfers.ResetItem(transfer.TransactionId);
			}
			else
			{
				Transfers.Add2(transfer);
			}
			return transfer;
		}

		/// <summary>Get/Set a history entry by order id. Returns null if 'key' is not in the collection</summary>
		public Transfer? this[string key]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return Transfers.TryGetValue(key, out var txfr) ? txfr : null;
			}
		}
	}
}



