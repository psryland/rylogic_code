//***********************************************
// Tuple
//  Copyright © Rylogic Ltd 2011
//***********************************************

using System.Collections;

namespace pr.common
{
	/* Use System.Tuple
	public struct Tuple<P0> :IEnumerable
	{
		public P0 p0;
		
		public Tuple(P0 p0) { this.p0 = p0; }
		public override bool Equals(object obj) { return obj is Tuple<P0> && p0.Equals(((Tuple<P0>)obj).p0); }
		public override int GetHashCode()       { return p0.GetHashCode(); }
		public IEnumerator GetEnumerator()      { yield return p0; }
	}
	public struct Tuple<P0,P1> :IEnumerable
	{
		public P0 p0;
		public P1 p1;

		public Tuple(P0 p0, P1 p1) { this.p0 = p0; this.p1 = p1; }
		public override bool Equals(object obj) { return obj is Tuple<P0,P1> && p0.Equals(((Tuple<P0,P1>)obj).p0) && p1.Equals(((Tuple<P0,P1>)obj).p1); }
		public override int GetHashCode()       { return p0.GetHashCode() ^ p1.GetHashCode(); }
		public IEnumerator GetEnumerator()      { yield return p0; yield return p1; }
	}
	public struct Tuple<P0,P1,P2> :IEnumerable
	{
		public P0 p0;
		public P1 p1;
		public P2 p2;

		public Tuple(P0 p0, P1 p1, P2 p2) { this.p0 = p0; this.p1 = p1; this.p2 = p2; }
		public override bool Equals(object obj) { return obj is Tuple<P0,P1,P2> && p0.Equals(((Tuple<P0,P1,P2>)obj).p0) && p1.Equals(((Tuple<P0,P1,P2>)obj).p1) && p2.Equals(((Tuple<P0,P1,P2>)obj).p2); }
		public override int GetHashCode()       { return p0.GetHashCode() ^ p1.GetHashCode() ^ p2.GetHashCode(); }
		public IEnumerator GetEnumerator()      { yield return p0; yield return p1; yield return p2; }
	}
	public struct Tuple<P0,P1,P2,P3> :IEnumerable
	{
		public P0 p0;
		public P1 p1;
		public P2 p2;
		public P3 p3;

		public Tuple(P0 p0, P1 p1, P2 p2, P3 p3) { this.p0 = p0; this.p1 = p1; this.p2 = p2; this.p3 = p3; }
		public override bool Equals(object obj) { return obj is Tuple<P0,P1,P2,P3> && p0.Equals(((Tuple<P0,P1,P2,P3>)obj).p0) && p1.Equals(((Tuple<P0,P1,P2,P3>)obj).p1) && p2.Equals(((Tuple<P0,P1,P2,P3>)obj).p2) && p3.Equals(((Tuple<P0,P1,P2,P3>)obj).p3); }
		public override int GetHashCode()       { return p0.GetHashCode() ^ p1.GetHashCode() ^ p2.GetHashCode() ^ p3.GetHashCode(); }
		public IEnumerator GetEnumerator()      { yield return p0; yield return p1; yield return p2; yield return p3; }
	}
	public struct Tuple<P0,P1,P2,P3,P4> :IEnumerable
	{
		public P0 p0;
		public P1 p1;
		public P2 p2;
		public P3 p3;
		public P4 p4;

		public Tuple(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4) { this.p0 = p0; this.p1 = p1; this.p2 = p2; this.p3 = p3; this.p4 = p4; }
		public override bool Equals(object obj) { return obj is Tuple<P0,P1,P2,P3,P4> && p0.Equals(((Tuple<P0,P1,P2,P3,P4>)obj).p0) && p1.Equals(((Tuple<P0,P1,P2,P3,P4>)obj).p1) && p2.Equals(((Tuple<P0,P1,P2,P3,P4>)obj).p2) && p3.Equals(((Tuple<P0,P1,P2,P3,P4>)obj).p3) && p4.Equals(((Tuple<P0,P1,P2,P3,P4>)obj).p4); }
		public override int GetHashCode()       { return p0.GetHashCode() ^ p1.GetHashCode() ^ p2.GetHashCode() ^ p3.GetHashCode() ^ p4.GetHashCode(); }
		public IEnumerator GetEnumerator()      { yield return p0; yield return p1; yield return p2; yield return p3; yield return p4; }
	}
	 */
}
