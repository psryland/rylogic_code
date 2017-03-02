using System;
using System.Diagnostics;
using pr.maths;

namespace Rylobot
{
	// "typedef" for negative instrument indices
	[DebuggerDisplay("{value}")]
	public struct NegIdx :IComparable<NegIdx>
	{
		private int value;
		[DebuggerStepThrough] private NegIdx(int v) { value = v; }
		[DebuggerStepThrough] public static explicit operator int(NegIdx neg_idx) { return neg_idx.value; }
		[DebuggerStepThrough] public static implicit operator NegIdx(int neg_idx) { return new NegIdx(neg_idx); }
		[DebuggerStepThrough] public static bool operator == (NegIdx lhs, NegIdx rhs) { return lhs.value == rhs.value; }
		[DebuggerStepThrough] public static bool operator != (NegIdx lhs, NegIdx rhs) { return lhs.value != rhs.value; }
		[DebuggerStepThrough] public static bool operator <  (NegIdx lhs, NegIdx rhs) { return lhs.value <  rhs.value; }
		[DebuggerStepThrough] public static bool operator <= (NegIdx lhs, NegIdx rhs) { return lhs.value <= rhs.value; }
		[DebuggerStepThrough] public static bool operator >  (NegIdx lhs, NegIdx rhs) { return lhs.value >  rhs.value; }
		[DebuggerStepThrough] public static bool operator >= (NegIdx lhs, NegIdx rhs) { return lhs.value >= rhs.value; }
		[DebuggerStepThrough] public static NegIdx operator ++ (NegIdx lhs) { return new NegIdx(++lhs.value); }
		[DebuggerStepThrough] public static NegIdx operator -- (NegIdx lhs) { return new NegIdx(--lhs.value); }
		[DebuggerStepThrough] public static NegIdx operator + (int lhs, NegIdx rhs) { return new NegIdx(lhs + rhs.value); }
		[DebuggerStepThrough] public static NegIdx operator + (NegIdx lhs, int rhs) { return new NegIdx(lhs.value + rhs); }
		[DebuggerStepThrough] public static NegIdx operator - (NegIdx lhs, int rhs) { return new NegIdx(lhs.value - rhs); }
		[DebuggerStepThrough] public static int operator - (NegIdx lhs, NegIdx rhs) { return lhs.value - rhs.value; }
		[DebuggerStepThrough] public override string ToString() { return value.ToString(); }
		[DebuggerStepThrough] public override bool Equals(object obj) { return value.Equals(obj); }
		[DebuggerStepThrough] public override int GetHashCode() { return value.GetHashCode(); }
		[DebuggerStepThrough] public int CompareTo(NegIdx other) { return value.CompareTo(other.value); }
	}

	// "typedef" for 'pips'
	[DebuggerDisplay("{value}")]
	public struct Pips :IComparable<Pips>
	{
		private double value;
		[DebuggerStepThrough] private Pips(double v) { value = v; }
		[DebuggerStepThrough] public static explicit operator double(Pips pip) { return pip.value; }
		[DebuggerStepThrough] public static implicit operator Pips(double pip) { return new Pips(pip); }
		[DebuggerStepThrough] public static bool operator == (Pips lhs, Pips rhs) { return lhs.value == rhs.value; }
		[DebuggerStepThrough] public static bool operator != (Pips lhs, Pips rhs) { return lhs.value != rhs.value; }
		[DebuggerStepThrough] public static bool operator <  (Pips lhs, Pips rhs) { return lhs.value <  rhs.value; }
		[DebuggerStepThrough] public static bool operator <= (Pips lhs, Pips rhs) { return lhs.value <= rhs.value; }
		[DebuggerStepThrough] public static bool operator >  (Pips lhs, Pips rhs) { return lhs.value >  rhs.value; }
		[DebuggerStepThrough] public static bool operator >= (Pips lhs, Pips rhs) { return lhs.value >= rhs.value; }
		[DebuggerStepThrough] public static Pips operator + (Pips lhs) { return new Pips(+lhs.value); }
		[DebuggerStepThrough] public static Pips operator - (Pips lhs) { return new Pips(-lhs.value); }
		[DebuggerStepThrough] public static Pips operator + (Pips lhs, Pips rhs) { return new Pips(lhs.value + rhs.value); }
		[DebuggerStepThrough] public static Pips operator - (Pips lhs, Pips rhs) { return new Pips(lhs.value - rhs.value); }
		[DebuggerStepThrough] public static Pips operator * (double lhs, Pips rhs) { return new Pips(lhs * rhs.value); }
		[DebuggerStepThrough] public static Pips operator * (Pips lhs, double rhs) { return new Pips(lhs.value * rhs); }
		[DebuggerStepThrough] public static Pips operator / (Pips lhs, double rhs) { return new Pips(lhs.value / rhs); }
		[DebuggerStepThrough] public static double operator / (Pips lhs, Pips rhs) { return lhs.value / rhs.value; }
		[DebuggerStepThrough] public override string ToString() { return value.ToString(); }
		[DebuggerStepThrough] public override bool Equals(object obj) { return value.Equals(obj); }
		[DebuggerStepThrough] public override int GetHashCode() { return value.GetHashCode(); }
		[DebuggerStepThrough] public int CompareTo(Pips other) { return value.CompareTo(other.value); }
	}

	// "typedef" for 'quote currency'
	[DebuggerDisplay("{value}")]
	public struct QuoteCurrency :IComparable<QuoteCurrency>
	{
		private double value;
		[DebuggerStepThrough] private QuoteCurrency(double v) { value = v; }
		[DebuggerStepThrough] public static explicit operator double(QuoteCurrency pip) { return pip.value; }
		[DebuggerStepThrough] public static implicit operator QuoteCurrency(double pip) { return new QuoteCurrency(pip); }
		[DebuggerStepThrough] public static bool operator == (QuoteCurrency lhs, QuoteCurrency rhs) { return lhs.value == rhs.value; }
		[DebuggerStepThrough] public static bool operator != (QuoteCurrency lhs, QuoteCurrency rhs) { return lhs.value != rhs.value; }
		[DebuggerStepThrough] public static bool operator <  (QuoteCurrency lhs, QuoteCurrency rhs) { return lhs.value <  rhs.value; }
		[DebuggerStepThrough] public static bool operator <= (QuoteCurrency lhs, QuoteCurrency rhs) { return lhs.value <= rhs.value; }
		[DebuggerStepThrough] public static bool operator >  (QuoteCurrency lhs, QuoteCurrency rhs) { return lhs.value >  rhs.value; }
		[DebuggerStepThrough] public static bool operator >= (QuoteCurrency lhs, QuoteCurrency rhs) { return lhs.value >= rhs.value; }
		[DebuggerStepThrough] public static QuoteCurrency operator + (QuoteCurrency lhs) { return new QuoteCurrency(+lhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator - (QuoteCurrency lhs) { return new QuoteCurrency(-lhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator + (QuoteCurrency lhs, QuoteCurrency rhs) { return new QuoteCurrency(lhs.value + rhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator - (QuoteCurrency lhs, QuoteCurrency rhs) { return new QuoteCurrency(lhs.value - rhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator * (double lhs, QuoteCurrency rhs) { return new QuoteCurrency(lhs * rhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator * (QuoteCurrency lhs, double rhs) { return new QuoteCurrency(lhs.value * rhs); }
		[DebuggerStepThrough] public static QuoteCurrency operator / (QuoteCurrency lhs, double rhs) { return new QuoteCurrency(lhs.value / rhs); }
		[DebuggerStepThrough] public static double operator / (QuoteCurrency lhs, QuoteCurrency rhs) { return lhs.value / rhs.value; }
		[DebuggerStepThrough] public override string ToString() { return value.ToString(); }
		[DebuggerStepThrough] public override bool Equals(object obj) { return value.Equals(obj); }
		[DebuggerStepThrough] public override int GetHashCode() { return value.GetHashCode(); }
		[DebuggerStepThrough] public int CompareTo(QuoteCurrency other) { return value.CompareTo(other.value); }
	}

	// "typedef" for 'account currency'
	[DebuggerDisplay("{value}")]
	public struct AcctCurrency :IComparable<AcctCurrency>
	{
		private double value;
		[DebuggerStepThrough] private AcctCurrency(double v) { value = v; }
		[DebuggerStepThrough] public static explicit operator double(AcctCurrency pip) { return pip.value; }
		[DebuggerStepThrough] public static implicit operator AcctCurrency(double pip) { return new AcctCurrency(pip); }
		[DebuggerStepThrough] public static bool operator == (AcctCurrency lhs, AcctCurrency rhs) { return lhs.value == rhs.value; }
		[DebuggerStepThrough] public static bool operator != (AcctCurrency lhs, AcctCurrency rhs) { return lhs.value != rhs.value; }
		[DebuggerStepThrough] public static bool operator <  (AcctCurrency lhs, AcctCurrency rhs) { return lhs.value <  rhs.value; }
		[DebuggerStepThrough] public static bool operator <= (AcctCurrency lhs, AcctCurrency rhs) { return lhs.value <= rhs.value; }
		[DebuggerStepThrough] public static bool operator >  (AcctCurrency lhs, AcctCurrency rhs) { return lhs.value >  rhs.value; }
		[DebuggerStepThrough] public static bool operator >= (AcctCurrency lhs, AcctCurrency rhs) { return lhs.value >= rhs.value; }
		[DebuggerStepThrough] public static AcctCurrency operator + (AcctCurrency lhs) { return new AcctCurrency(+lhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator - (AcctCurrency lhs) { return new AcctCurrency(-lhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator + (AcctCurrency lhs, AcctCurrency rhs) { return new AcctCurrency(lhs.value + rhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator - (AcctCurrency lhs, AcctCurrency rhs) { return new AcctCurrency(lhs.value - rhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator * (double lhs, AcctCurrency rhs) { return new AcctCurrency(lhs * rhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator * (AcctCurrency lhs, double rhs) { return new AcctCurrency(lhs.value * rhs); }
		[DebuggerStepThrough] public static AcctCurrency operator / (AcctCurrency lhs, double rhs) { return new AcctCurrency(lhs.value / rhs); }
		[DebuggerStepThrough] public static double operator / (AcctCurrency lhs, AcctCurrency rhs) { return lhs.value / rhs.value; }
		[DebuggerStepThrough] public override string ToString() { return value.ToString(); }
		[DebuggerStepThrough] public override bool Equals(object obj) { return value.Equals(obj); }
		[DebuggerStepThrough] public override int GetHashCode() { return value.GetHashCode(); }
		[DebuggerStepThrough] public int CompareTo(AcctCurrency other) { return value.CompareTo(other.value); }
	}

	public static class Misc
	{
		public static int Sign(QuoteCurrency lhs)
		{
			return Math.Sign((double)lhs);
		}
		public static QuoteCurrency Abs(QuoteCurrency lhs)
		{
			return Math.Abs((double)lhs);
		}
		public static QuoteCurrency Min(QuoteCurrency lhs, QuoteCurrency rhs)
		{
			return Math.Min((double)lhs, (double)rhs);
		}
		public static QuoteCurrency Max(QuoteCurrency lhs, QuoteCurrency rhs)
		{
			return Math.Max((double)lhs, (double)rhs);
		}
		public static double Div(QuoteCurrency lhs, QuoteCurrency rhs, double def = 0)
		{
			return Maths.Div((double)lhs, (double)rhs, def);
		}

		public static int Sign(AcctCurrency lhs)
		{
			return Math.Sign((double)lhs);
		}
		public static AcctCurrency Abs(AcctCurrency lhs)
		{
			return Math.Abs((double)lhs);
		}
		public static AcctCurrency Min(AcctCurrency lhs, AcctCurrency rhs)
		{
			return Math.Min((double)lhs, (double)rhs);
		}
		public static AcctCurrency Max(AcctCurrency lhs, AcctCurrency rhs)
		{
			return Math.Max((double)lhs, (double)rhs);
		}
		public static double Div(AcctCurrency lhs, AcctCurrency rhs, double def = 0)
		{
			return Maths.Div((double)lhs, (double)rhs, def);
		}
	}
}
