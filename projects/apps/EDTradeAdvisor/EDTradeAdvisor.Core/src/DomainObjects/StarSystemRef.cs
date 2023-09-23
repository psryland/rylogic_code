using System;
using System.Diagnostics;
using Rylogic.Container;
using Rylogic.Maths;

namespace EDTradeAdvisor.DomainObjects
{
	[DebuggerDisplay("{ID}")]
	public struct StarSystemRef :KDTree_.IAccessors<StarSystemRef>
	{
		// Notes:
		//  - This type is used in the KD Tree star system map
		//  - Floats are used instead of doubles so that the struct
		//    is as small as possible and 64bit aligned.

		public StarSystemRef(StarSystem star)
		{
			X = (float)star.X;
			Y = (float)star.Y;
			Z = (float)star.Z;
			Axis = 0;
			ID = star.ID;
		}

		/// <summary>The axis </summary>
		public int Axis;

		/// <summary>The position of the system in space</summary>
		public float X, Y, Z;

		/// <summary>The star system database ID</summary>
		public long ID;

		/// <summary></summary>
		public v4 Position => new(X, Y, Z, 1f);

		/// <summary>KDTree accessors</summary>
		StarSystemRef KDTree_.IAccessors<StarSystemRef>.SortAxisSet(StarSystemRef elem, int axis) { elem.Axis = axis; return elem; }
		int KDTree_.IAccessors<StarSystemRef>.SortAxisGet(StarSystemRef elem) => elem.Axis;
		double KDTree_.IAccessors<StarSystemRef>.AxisValueGet(StarSystemRef elem, int axis) =>
			axis == 0 ? elem.X :
			axis == 1 ? elem.Y :
			axis == 2 ? elem.Z :
			throw new Exception("Invalid axis value");
	}
}
