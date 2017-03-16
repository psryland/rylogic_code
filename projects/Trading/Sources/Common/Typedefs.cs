using System;
using System.Diagnostics;
using pr.maths;
using pr.extn;

namespace Rylobot
{
	/// <summary>"typedef" for negative (fractional) instrument indices. Typically in the range [-Count,+1]</summary>
	[DebuggerDisplay("{value}")]
	public struct Idx :IComparable<Idx>
	{
		// Note: differences return 'Idx' rather than double because casting an 
		// 'Idx' to (int) correctly returns Math.Floor(idx)
		private double value;
		[DebuggerStepThrough] private Idx(double v)                               { value = v; }
		[DebuggerStepThrough] public static implicit operator double(Idx neg_idx) { return neg_idx.value; }
		[DebuggerStepThrough] public static implicit operator int(Idx neg_idx)    { return (int)Math.Floor(neg_idx.value); }
		[DebuggerStepThrough] public static implicit operator Idx(int neg_idx)    { return new Idx(neg_idx); }
		[DebuggerStepThrough] public static implicit operator Idx(double neg_idx) { return new Idx(neg_idx); }
		[DebuggerStepThrough] public static bool operator == (Idx lhs, Idx rhs)   { return (int)lhs == (int)rhs; }
		[DebuggerStepThrough] public static bool operator != (Idx lhs, Idx rhs)   { return (int)lhs != (int)rhs; }
		[DebuggerStepThrough] public static bool operator <  (Idx lhs, Idx rhs)   { return lhs.value <  rhs.value; }
		[DebuggerStepThrough] public static bool operator <= (Idx lhs, Idx rhs)   { return lhs.value <= rhs.value; }
		[DebuggerStepThrough] public static bool operator >  (Idx lhs, Idx rhs)   { return lhs.value >  rhs.value; }
		[DebuggerStepThrough] public static bool operator >= (Idx lhs, Idx rhs)   { return lhs.value >= rhs.value; }
		[DebuggerStepThrough] public static Idx operator ++ (Idx lhs)             { return new Idx(++lhs.value); }
		[DebuggerStepThrough] public static Idx operator -- (Idx lhs)             { return new Idx(--lhs.value); }
		[DebuggerStepThrough] public static Idx operator + (Idx lhs, Idx rhs)     { return lhs.value + rhs.value; }
		[DebuggerStepThrough] public static Idx operator - (Idx lhs, Idx rhs)     { return lhs.value - rhs.value; }
		[DebuggerStepThrough] public override string ToString()                   { return value.ToString(); }
		[DebuggerStepThrough] public override bool Equals(object obj)             { return value.Equals(obj); }
		[DebuggerStepThrough] public override int GetHashCode()                   { return value.GetHashCode(); }
		[DebuggerStepThrough] public int CompareTo(Idx other)                     { return value.CompareTo(other.value); }
		[DebuggerStepThrough] public bool Within(Idx lhs, Idx rhs)                { return value.Within(lhs.value, rhs.value); }
	}

	/// <summary>An index for a point in the future</summary>
	[DebuggerDisplay("{Index}")]
	public struct FutureIdx
	{
		public FutureIdx(Idx idx, double confidence) :this()
		{
			Index = idx;
			Confidence = confidence;
		}

		/// <summary>The future index</summary>
		public Idx Index { [DebuggerStepThrough] get; private set; }

		/// <summary>A confidence level associated with the index</summary>
		public double Confidence { [DebuggerStepThrough] get; private set; }

		// Implicit conversion
		[DebuggerStepThrough] public static implicit operator Idx(FutureIdx fi)    { return fi.Index; }
		[DebuggerStepThrough] public static implicit operator double(FutureIdx fi) { return fi.Index; }
		[DebuggerStepThrough] public static implicit operator int(FutureIdx fi)    { return fi.Index; }
		[DebuggerStepThrough] public override string ToString()                    { return Index.ToString(); }
		[DebuggerStepThrough] public bool Within(Idx min, Idx max)                 { return Index.Within(min, max); }
	}

	// "typedef" for 'pips'
	[DebuggerDisplay("{value}")]
	public struct Pips :IComparable<Pips>
	{
		private double value;
		[DebuggerStepThrough] private Pips(double v)                               { value = v; }
		[DebuggerStepThrough] public static implicit operator double(Pips pip)     { return pip.value; }
		[DebuggerStepThrough] public static implicit operator Pips(double pip)     { return new Pips(pip); }
		[DebuggerStepThrough] public static bool operator == (Pips lhs, Pips rhs)  { return lhs.value == rhs.value; }
		[DebuggerStepThrough] public static bool operator != (Pips lhs, Pips rhs)  { return lhs.value != rhs.value; }
		[DebuggerStepThrough] public static bool operator <  (Pips lhs, Pips rhs)  { return lhs.value <  rhs.value; }
		[DebuggerStepThrough] public static bool operator <= (Pips lhs, Pips rhs)  { return lhs.value <= rhs.value; }
		[DebuggerStepThrough] public static bool operator >  (Pips lhs, Pips rhs)  { return lhs.value >  rhs.value; }
		[DebuggerStepThrough] public static bool operator >= (Pips lhs, Pips rhs)  { return lhs.value >= rhs.value; }
		[DebuggerStepThrough] public static Pips operator + (Pips lhs)             { return new Pips(+lhs.value); }
		[DebuggerStepThrough] public static Pips operator - (Pips lhs)             { return new Pips(-lhs.value); }
		[DebuggerStepThrough] public static Pips operator + (Pips lhs, Pips rhs)   { return new Pips(lhs.value + rhs.value); }
		[DebuggerStepThrough] public static Pips operator - (Pips lhs, Pips rhs)   { return new Pips(lhs.value - rhs.value); }
		[DebuggerStepThrough] public static Pips operator * (double lhs, Pips rhs) { return new Pips(lhs * rhs.value); }
		[DebuggerStepThrough] public static Pips operator * (Pips lhs, double rhs) { return new Pips(lhs.value * rhs); }
		[DebuggerStepThrough] public static Pips operator / (Pips lhs, double rhs) { return new Pips(lhs.value / rhs); }
		[DebuggerStepThrough] public static double operator / (Pips lhs, Pips rhs) { return lhs.value / rhs.value; }
		[DebuggerStepThrough] public override string ToString()                    { return value.ToString(); }
		[DebuggerStepThrough] public override bool Equals(object obj)              { return value.Equals(obj); }
		[DebuggerStepThrough] public override int GetHashCode()                    { return value.GetHashCode(); }
		[DebuggerStepThrough] public int CompareTo(Pips other)                     { return value.CompareTo(other.value); }
		[DebuggerStepThrough] public bool Within(Pips min, Pips max)               { return value.Within(min, max); }
	}

	// "typedef" for 'quote currency'
	[DebuggerDisplay("{value}")]
	public struct QuoteCurrency :IComparable<QuoteCurrency>
	{
		private double value;
		[DebuggerStepThrough] private QuoteCurrency(double v)                                               { value = v; }
		[DebuggerStepThrough] public static implicit operator double(QuoteCurrency price)                   { return price.value; }
		[DebuggerStepThrough] public static implicit operator QuoteCurrency(double price)                   { return new QuoteCurrency(price); }
		[DebuggerStepThrough] public static bool operator == (QuoteCurrency lhs, QuoteCurrency rhs)         { return lhs.value == rhs.value; }
		[DebuggerStepThrough] public static bool operator != (QuoteCurrency lhs, QuoteCurrency rhs)         { return lhs.value != rhs.value; }
		[DebuggerStepThrough] public static bool operator <  (QuoteCurrency lhs, QuoteCurrency rhs)         { return lhs.value <  rhs.value; }
		[DebuggerStepThrough] public static bool operator <= (QuoteCurrency lhs, QuoteCurrency rhs)         { return lhs.value <= rhs.value; }
		[DebuggerStepThrough] public static bool operator >  (QuoteCurrency lhs, QuoteCurrency rhs)         { return lhs.value >  rhs.value; }
		[DebuggerStepThrough] public static bool operator >= (QuoteCurrency lhs, QuoteCurrency rhs)         { return lhs.value >= rhs.value; }
		[DebuggerStepThrough] public static QuoteCurrency operator + (QuoteCurrency lhs)                    { return new QuoteCurrency(+lhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator - (QuoteCurrency lhs)                    { return new QuoteCurrency(-lhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator + (QuoteCurrency lhs, QuoteCurrency rhs) { return new QuoteCurrency(lhs.value + rhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator - (QuoteCurrency lhs, QuoteCurrency rhs) { return new QuoteCurrency(lhs.value - rhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator * (double lhs, QuoteCurrency rhs)        { return new QuoteCurrency(lhs * rhs.value); }
		[DebuggerStepThrough] public static QuoteCurrency operator * (QuoteCurrency lhs, double rhs)        { return new QuoteCurrency(lhs.value * rhs); }
		[DebuggerStepThrough] public static QuoteCurrency operator / (QuoteCurrency lhs, double rhs)        { return new QuoteCurrency(lhs.value / rhs); }
		[DebuggerStepThrough] public static double operator / (QuoteCurrency lhs, QuoteCurrency rhs)        { return lhs.value / rhs.value; }
		[DebuggerStepThrough] public override string ToString()                                             { return value.ToString(); }
		[DebuggerStepThrough] public override bool Equals(object obj)                                       { return value.Equals(obj); }
		[DebuggerStepThrough] public override int GetHashCode()                                             { return value.GetHashCode(); }
		[DebuggerStepThrough] public int CompareTo(QuoteCurrency other)                                     { return value.CompareTo(other.value); }
		[DebuggerStepThrough] public bool Within(QuoteCurrency lhs, QuoteCurrency rhs)                      { return value.Within(lhs.value, rhs.value); }
	}

	// "typedef" for 'account currency'
	[DebuggerDisplay("{value}")]
	public struct AcctCurrency :IComparable<AcctCurrency>
	{
		private double value;
		[DebuggerStepThrough] private AcctCurrency(double v)                                             { value = v; }
		[DebuggerStepThrough] public static explicit operator double(AcctCurrency price)                 { return price.value; }
		[DebuggerStepThrough] public static implicit operator AcctCurrency(double price)                 { return new AcctCurrency(price); }
		[DebuggerStepThrough] public static bool operator == (AcctCurrency lhs, AcctCurrency rhs)        { return lhs.value == rhs.value; }
		[DebuggerStepThrough] public static bool operator != (AcctCurrency lhs, AcctCurrency rhs)        { return lhs.value != rhs.value; }
		[DebuggerStepThrough] public static bool operator <  (AcctCurrency lhs, AcctCurrency rhs)        { return lhs.value <  rhs.value; }
		[DebuggerStepThrough] public static bool operator <= (AcctCurrency lhs, AcctCurrency rhs)        { return lhs.value <= rhs.value; }
		[DebuggerStepThrough] public static bool operator >  (AcctCurrency lhs, AcctCurrency rhs)        { return lhs.value >  rhs.value; }
		[DebuggerStepThrough] public static bool operator >= (AcctCurrency lhs, AcctCurrency rhs)        { return lhs.value >= rhs.value; }
		[DebuggerStepThrough] public static AcctCurrency operator + (AcctCurrency lhs)                   { return new AcctCurrency(+lhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator - (AcctCurrency lhs)                   { return new AcctCurrency(-lhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator + (AcctCurrency lhs, AcctCurrency rhs) { return new AcctCurrency(lhs.value + rhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator - (AcctCurrency lhs, AcctCurrency rhs) { return new AcctCurrency(lhs.value - rhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator * (double lhs, AcctCurrency rhs)       { return new AcctCurrency(lhs * rhs.value); }
		[DebuggerStepThrough] public static AcctCurrency operator * (AcctCurrency lhs, double rhs)       { return new AcctCurrency(lhs.value * rhs); }
		[DebuggerStepThrough] public static AcctCurrency operator / (AcctCurrency lhs, double rhs)       { return new AcctCurrency(lhs.value / rhs); }
		[DebuggerStepThrough] public static double operator / (AcctCurrency lhs, AcctCurrency rhs)       { return lhs.value / rhs.value; }
		[DebuggerStepThrough] public override string ToString()                                          { return value.ToString(); }
		[DebuggerStepThrough] public override bool Equals(object obj)                                    { return value.Equals(obj); }
		[DebuggerStepThrough] public override int GetHashCode()                                          { return value.GetHashCode(); }
		[DebuggerStepThrough] public int CompareTo(AcctCurrency other)                                   { return value.CompareTo(other.value); }
		[DebuggerStepThrough] public bool Within(AcctCurrency lhs, AcctCurrency rhs)                     { return value.Within(lhs.value, rhs.value); }
	}
}
