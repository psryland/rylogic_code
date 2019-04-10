//***************************************************
// Bounding Box
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Drawing;

namespace Rylogic.Maths
{
	/// <summary>Bounding box</summary>
	[Serializable]
	[DebuggerDisplay("{Description,nq}")]
	public struct BBox
	{
		// Constructors
		public BBox(v4 centre, v4 radius)
		{
			Centre = centre;
			Radius = radius;
		}
		public void reset()
		{
			this = Reset;
		}
		public void unit()
		{
			this = Unit;
		}
		public void set(v4 centre, v4 radius)
		{
			Centre = centre;
			Radius = radius;
		}
		public override string ToString()
		{
			return $"Centre={Centre.ToString3()} Radius={Radius.ToString3()}";
		}

		// Construct from
		public static BBox From(v4 min, v4 max)
		{
			return new BBox((max + min) * 0.5f, Math_.Abs(max - min) * 0.5f);
		}

		// Static v4 types
		private readonly static BBox m_unit = new BBox(v4.Origin, new v4(0.5f, 0.5f, 0.5f, 0));
		private readonly static BBox m_reset = new BBox(v4.Origin, new v4(-1, -1, -1, 0));
		public static BBox Unit
		{
			get { return m_unit; }
		}
		public static BBox Reset
		{
			get { return m_reset; }
		}

		// Binary operators
		public static BBox operator +(BBox lhs, v4 offset) { lhs.Centre += offset; return lhs; }
		public static BBox operator -(BBox lhs, v4 offset) { lhs.Centre -= offset; return lhs; }
		public static BBox operator *(BBox lhs, float s) { lhs.Radius *= s; return lhs; }
		public static BBox operator /(BBox lhs, float s) { lhs.Radius /= s; return lhs; }
		public static BBox operator *(m4x4 m, BBox rhs)
		{
			Debug.Assert(rhs.IsValid, "Transforming an invalid bounding box");
			var bb = new BBox(m.pos, v4.Zero);
			var mat = Math_.Transpose3x3(m);
			bb.Centre.x += Math_.Dot(mat.x, rhs.Centre);
			bb.Radius.x += Math_.Dot(Math_.Abs(mat.x), rhs.Radius);
			bb.Centre.y += Math_.Dot(mat.y, rhs.Centre);
			bb.Radius.y += Math_.Dot(Math_.Abs(mat.y), rhs.Radius);
			bb.Centre.z += Math_.Dot(mat.z, rhs.Centre);
			bb.Radius.z += Math_.Dot(Math_.Abs(mat.z), rhs.Radius);
			return bb;
		}

		// Equality operators
		public static bool operator ==(BBox lhs, BBox rhs) { return lhs.Centre == rhs.Centre; }
		public static bool operator !=(BBox lhs, BBox rhs) { return !(lhs == rhs); }
		public override bool Equals(object o) { return o is BBox && (BBox)o == this; }
		public override int GetHashCode() { unchecked { return Centre.GetHashCode() ^ Radius.GetHashCode(); } }

		/// <summary>Returns true if the bounding box represents a point or volume</summary>
		public bool IsValid
		{
			get
			{
				return Radius.x >= 0f && Radius.y >= 0f && Radius.z >= 0f &&
					Math_.IsFinite(Radius.LengthSq) && Math_.IsFinite(Centre.LengthSq);
			}
		}

		/// <summary>Returns true if this bbox encompasses a single point</summary>
		public bool IsPoint
		{
			get { return Radius == v4.Zero;  }
		}

		/// <summary>Minimum X bound</summary>
		public float MinX
		{
			get { return Centre.x - Radius.x; }
		}

		/// <summary>Maximum X bound</summary>
		public float MaxX
		{
			get { return Centre.x + Radius.x; }
		}

		/// <summary>Minimum Y bound</summary>
		public float MinY
		{
			get { return Centre.y - Radius.y; }
		}

		/// <summary>Maximum Y bound</summary>
		public float MaxY
		{
			get { return Centre.y + Radius.y; }
		}

		/// <summary>Minimum Z bound</summary>
		public float MinZ
		{
			get { return Centre.z - Radius.z; }
		}

		/// <summary>Maximum Z bound</summary>
		public float MaxZ
		{
			get { return Centre.z + Radius.z; }
		}

		/// <summary>Returns the lower corner of the bounding box</summary>
		public v4 Lower()
		{
			return Centre - Radius;
		}

		/// <summary>Returns the upper corner of the bounding box</summary>
		public v4 Upper()
		{
			return Centre + Radius;
		}

		/// <summary>Returns the lower dimension for an axis of the bounding box</summary>
		public float Lower(int axis)
		{
			return Centre[axis] - Radius[axis];
		}

		/// <summary>Returns the upper dimension of an axis of the bounding box</summary>
		public float Upper(int axis)
		{
			return Centre[axis] + Radius[axis];
		}

		/// <summary>Gets the x dimension of the bounding box</summary>
		public float SizeX
		{
			get { return 2.0f * Radius.x; }
		}

		/// <summary>Gets the y dimension of the bounding box</summary>
		public float SizeY
		{
			get { return 2.0f * Radius.y; }
		}

		/// <summary>Gets the z dimension of the bounding box</summary>
		public float SizeZ
		{
			get { return 2.0f * Radius.z; }
		}

		/// <summary>Get/Sets the centre point of the bounding box</summary>
		public v4 Centre;

		/// <summary>Get/Sets the dimensions of the bounding box</summary>
		public v4 Radius;

		/// <summary>Gets the squared length of the diagonal of the bounding box</summary>
		public float DiametreSq
		{
			get { return 4.0f * Radius.LengthSq; }
		}

		/// <summary>Gets the length of the diagonal of the bounding box</summary>
		public float Diametre
		{
			get { return Math_.Sqrt(DiametreSq); }
		}

		/// <summary>Gets the volume of the bounding box</summary>
		public float Volume
		{
			get { return SizeX * SizeY * SizeZ; }
		}

		/// <summary>Returns a corner of the bounding box. 'corner'</summary>
		public v4 GetCorner(uint corner)
		{
			Debug.Assert(corner < 8, "Invalid corner index");
			uint x = ((corner >> 0) & 0x1) * 2 - 1;
			uint y = ((corner >> 1) & 0x1) * 2 - 1;
			uint z = ((corner >> 2) & 0x1) * 2 - 1;
			return new v4(Centre.x + x*Radius.x, Centre.y + y*Radius.y, Centre.z + z*Radius.z, 1.0f);
		}

		/// <summary>Returns true if 'point' is within this bounding box (within 'tol'erance)</summary>
		public bool IsWithin(v4 point, float tol)
		{
			return  Math.Abs(point.x - Centre.x) <= Radius.x + tol &&
					Math.Abs(point.y - Centre.y) <= Radius.y + tol &&
					Math.Abs(point.z - Centre.z) <= Radius.z + tol;
		}

		/// <summary>Returns true if 'bbox' is within this bounding box (within 'tol'erance)</summary>
		public bool IsWithin(BBox bbox, float tol)
		{
			return  Math.Abs(bbox.Centre.x - Centre.x) <= (Radius.x - bbox.Radius.x + tol) &&
					Math.Abs(bbox.Centre.y - Centre.y) <= (Radius.y - bbox.Radius.y + tol) &&
					Math.Abs(bbox.Centre.z - Centre.z) <= (Radius.z - bbox.Radius.z + tol);
		}

		/// <summary>Expands the bounding box to include 'point'</summary>
		public static BBox Encompass(BBox bbox, v4 point)
		{
			// Note:
			// This is not a member function because it's dangerous.
			// Calling: 'thing.BBox.Encompass(v4)' does not change 'BBox' because it's a value type
			// Use: 'thing.BBox = BBox.Encompass(thing.BBox, v4)' instead

			var bb = bbox;
			for (int i = 0; i != 3; ++i)
			{
				if (bb.Radius[i] < 0.0f)
				{
					bb.Centre[i] = point[i];
					bb.Radius[i] = 0.0f;
				}
				else
				{
					float signed_dist = point[i] - bb.Centre[i];
					float length      = Math.Abs(signed_dist);
					if (length > bb.Radius[i])
					{
						float new_radius = (length + bb.Radius[i]) / 2.0f;
						bb.Centre[i] += signed_dist * (new_radius - bb.Radius[i]) / length;
						bb.Radius[i] = new_radius;
					}
				}
			}
			return bb;
		}

		/// <summary>Expands the bounding box to include 'rhs'</summary>
		public static BBox Encompass(BBox lhs, BBox rhs)
		{
			Debug.Assert(rhs.IsValid, "Encompassing an invalid bounding box");
			var bb = lhs;
			bb = Encompass(bb, rhs.Centre + rhs.Radius);
			bb = Encompass(bb, rhs.Centre - rhs.Radius);
			return bb;
		}

		/// <summary>Returns true if 'rhs' intersects with this bounding box</summary>
		public static bool IsIntersection(BBox lhs, BBox rhs)
		{
			return  Math.Abs(lhs.Centre.x - rhs.Centre.x) <= (lhs.Radius.x + rhs.Radius.x) &&
					Math.Abs(lhs.Centre.y - rhs.Centre.y) <= (lhs.Radius.y + rhs.Radius.y) &&
					Math.Abs(lhs.Centre.z - rhs.Centre.z) <= (lhs.Radius.z + rhs.Radius.z);
		}

		/// <summary>Create a RectangleF from the X,Y axes of this bounding box</summary>
		public RectangleF ToRectXY()
		{
			return new RectangleF(MinX, MinY, SizeX, SizeY);
		}

		/// <summary></summary>
		public string Description => $"C={Centre} R={Radius}";
	}
}