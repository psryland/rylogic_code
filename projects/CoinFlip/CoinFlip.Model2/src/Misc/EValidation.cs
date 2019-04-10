using System;
using System.Text;

namespace CoinFlip
{
	/// <summary>Trade validation</summary>
	[Flags]
	public enum EValidation
	{
		Valid               = 0,
		AmountInOutOfRange  = 1 << 0,
		AmountOutOutOfRange = 1 << 1,
		PriceOutOfRange     = 1 << 2,
		InsufficientBalance = 1 << 3,
		PriceIsInvalid      = 1 << 4,
		AmountInIsInvalid   = 1 << 5,
		AmountOutIsInvalid  = 1 << 6,
	}

	public static class EValidationExtn
	{
		/// <summary>Return a string description of this validation result</summary>
		public static string ToErrorDescription(this EValidation val)
		{
			var sb = new StringBuilder();
			if (val.HasFlag(EValidation.AmountInOutOfRange))
			{
				sb.AppendLine("The amount of currency being sold is not within the valid range.");
				val ^= EValidation.AmountInOutOfRange;
			}
			if (val.HasFlag(EValidation.AmountOutOutOfRange))
			{
				sb.AppendLine("The amount of currency being bought is not within the valid range.");
				val ^= EValidation.AmountOutOutOfRange;
			}
			if (val.HasFlag(EValidation.PriceOutOfRange))
			{
				sb.AppendLine("The price level to trade at is not within the valid range.");
				val ^= EValidation.PriceOutOfRange;
			}
			if (val.HasFlag(EValidation.InsufficientBalance))
			{
				sb.AppendLine("There is insufficient balance.");
				val ^= EValidation.InsufficientBalance;
			}
			if (val.HasFlag(EValidation.PriceIsInvalid))
			{
				sb.AppendLine("The price level to trade at is invalid.");
				val ^= EValidation.PriceIsInvalid;
			}
			if (val.HasFlag(EValidation.AmountInIsInvalid))
			{
				sb.AppendLine("The amount of currency being sold is invalid.");
				val ^= EValidation.AmountInIsInvalid;
			}
			if (val.HasFlag(EValidation.AmountOutIsInvalid))
			{
				sb.AppendLine("The amount of currency being bought is invalid.");
				val ^= EValidation.AmountOutIsInvalid;
			}
			if (val != 0)
			{
				throw new Exception("Unknown validation flags");
			}
			return sb.ToString();
		}
	}
}
