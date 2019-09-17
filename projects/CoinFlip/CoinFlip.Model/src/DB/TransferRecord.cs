namespace CoinFlip.DB
{
	internal class TransferRecord
	{
		//  - This is a domain object used to stored a history of fund transfers in a DB Table
		public TransferRecord()
		{ }
		public TransferRecord(Transfer transfer)
		{
			Id = 0;
			TransactionId = transfer.TransactionId;
			Type = transfer.Type;
			Symbol = transfer.Coin.Symbol;
			Amount = transfer.Amount.ToDouble();
			Created = transfer.Created.Ticks;
		}

		/// <summary>A unique Id for the transfer (CoinFlip only, used as a primary key)</summary>
		public long Id { get; set; }

		/// <summary>Unique Id for the transaction</summary>
		public string TransactionId { get; set; }

		/// <summary>Transfer direction</summary>
		public ETransfer Type { get; set; }

		/// <summary>The currency moved</summary>
		public string Symbol { get; set; }

		/// <summary>The amount moved</summary>
		public double Amount { get; set; }

		/// <summary>The timestamp of the transfer (in ticks)</summary>
		public long Created { get; set; }
	}
}
