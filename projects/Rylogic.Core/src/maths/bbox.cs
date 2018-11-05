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
	public struct BBox
	{
		private v4 m_centre;
		private v4 m_radius;

		// Constructors
		public BBox(v4 centre, v4 radius)
		{
			m_centre = centre;
			m_radius = radius;
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
			m_centre = centre;
			m_radius = radius;
		}
		public override string ToString()
		{
			return $"Centre={m_centre.ToString3()} Radius={m_radius.ToString3()}";
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
		public static BBox operator +(BBox lhs, v4 offset) { lhs.m_centre += offset; return lhs; }
		public static BBox operator -(BBox lhs, v4 offset) { lhs.m_centre -= offset; return lhs; }
		public static BBox operator *(BBox lhs, float s) { lhs.m_radius *= s; return lhs; }
		public static BBox operator /(BBox lhs, float s) { lhs.m_radius /= s; return lhs; }
		public static BBox operator *(m4x4 m, BBox rhs)
		{
			Debug.Assert(rhs.IsValid, "Transforming an invalid bounding box");
			var bb = new BBox(m.pos, v4.Zero);
			var mat = Math_.Transpose3x3(m);
			for (int i = 0; i != 3; ++i)
			{
				bb.m_centre[i] += Math_.Dot(mat[i], rhs.m_centre);
				bb.m_radius[i] += Math_.Dot(Math_.Abs(mat[i]), rhs.m_radius);
			}
			return bb;
		}

		// Equality operators
		public static bool operator ==(BBox lhs, BBox rhs) { return lhs.m_centre == rhs.m_centre; }
		public static bool operator !=(BBox lhs, BBox rhs) { return !(lhs == rhs); }
		public override bool Equals(object o) { return o is BBox && (BBox)o == this; }
		public override int GetHashCode() { unchecked { return m_centre.GetHashCode() ^ m_radius.GetHashCode(); } }

		/// <summary>Returns true if the bounding box represents a point or volume</summary>
		public bool IsValid
		{
			get { return m_radius.x >= 0f && m_radius.y >= 0f && m_radius.z >= 0f; }
		}

		/// <summary>Returns true if this bbox encompasses a single point</summary>
		public bool IsPoint
		{
			get { return m_radius == v4.Zero;  }
		}

		/// <summary>Minimum X bound</summary>
		public float MinX
		{
			get { return m_centre.x - m_radius.x; }
		}

		/// <summary>Maximum X bound</summary>
		public float MaxX
		{
			get { return m_centre.x + m_radius.x; }
		}

		/// <summary>Minimum Y bound</summary>
		public float MinY
		{
			get { return m_centre.y - m_radius.y; }
		}

		/// <summary>Maximum Y bound</summary>
		public float MaxY
		{
			get { return m_centre.y + m_radius.y; }
		}

		/// <summary>Minimum Z bound</summary>
		public float MinZ
		{
			get { return m_centre.z - m_radius.z; }
		}

		/// <summary>Maximum Z bound</summary>
		public float MaxZ
		{
			get { return m_centre.z + m_radius.z; }
		}

		/// <summary>Returns the lower corner of the bounding box</summary>
		public v4 Lower()
		{
			return m_centre - m_radius;
		}

		/// <summary>Returns the upper corner of the bounding box</summary>
		public v4 Upper()
		{
			return m_centre + m_radius;
		}

		/// <summary>Returns the lower dimension for an axis of the bounding box</summary>
		public float Lower(int axis)
		{
			return m_centre[axis] - m_radius[axis];
		}

		/// <summary>Returns the upper dimension of an axis of the bounding box</summary>
		public float Upper(int axis)
		{
			return m_centre[axis] + m_radius[axis];
		}

		/// <summary>Gets the x dimension of the bounding box</summary>
		public float SizeX
		{
			get { return 2.0f * m_radius.x; }
		}

		/// <summary>Gets the y dimension of the bounding box</summary>
		public float SizeY
		{
			get { return 2.0f * m_radius.y; }
		}

		/// <summary>Gets the z dimension of the bounding box</summary>
		public float SizeZ
		{
			get { return 2.0f * m_radius.z; }
		}

		/// <summary>Get/Sets the centre point of the bounding box</summary>
		public v4 Centre
		{
			get { return m_centre; }
			set { m_centre = value; }
		}

		/// <summary>Get/Sets the dimensions of the bounding box</summary>
		public v4 Radius
		{
			get { return m_radius; }
			set { m_radius = value; }
		}

		/// <summary>Gets the squared length of the diagonal of the bounding box</summary>
		public float DiametreSq
		{
			get { return 4.0f * m_radius.LengthSq; }
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
			return new v4(m_centre.x + x*m_radius.x, m_centre.y + y*m_radius.y, m_centre.z + z*m_radius.z, 1.0f);
		}

		/// <summary>Returns true if 'point' is within this bounding box (within 'tol'erance)</summary>
		public bool IsWithin(v4 point, float tol)
		{
			return  Math.Abs(point.x - m_centre.x) <= m_radius.x + tol &&
					Math.Abs(point.y - m_centre.y) <= m_radius.y + tol &&
					Math.Abs(point.z - m_centre.z) <= m_radius.z + tol;
		}

		/// <summary>Returns true if 'bbox' is within this bounding box (within 'tol'erance)</summary>
		public bool IsWithin(BBox bbox, float tol)
		{
			return  Math.Abs(bbox.m_centre.x - m_centre.x) <= (m_radius.x - bbox.m_radius.x + tol) &&
					Math.Abs(bbox.m_centre.y - m_centre.y) <= (m_radius.y - bbox.m_radius.y + tol) &&
					Math.Abs(bbox.m_centre.z - m_centre.z) <= (m_radius.z - bbox.m_radius.z + tol);
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
				if (bb.m_radius[i] < 0.0f)
				{
					bb.m_centre[i] = point[i];
					bb.m_radius[i] = 0.0f;
				}
				else
				{
					float signed_dist = point[i] - bb.m_centre[i];
					float length      = Math.Abs(signed_dist);
					if (length > bb.m_radius[i])
					{
						float new_radius = (length + bb.m_radius[i]) / 2.0f;
						bb.m_centre[i] += signed_dist * (new_radius - bb.m_radius[i]) / length;
						bb.m_radius[i] = new_radius;
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
			bb = Encompass(bb, rhs.m_centre + rhs.m_radius);
			bb = Encompass(bb, rhs.m_centre - rhs.m_radius);
			return bb;
		}

		/// <summary>Returns true if 'rhs' intersects with this bounding box</summary>
		public static bool IsIntersection(BBox lhs, BBox rhs)
		{
			return  Math.Abs(lhs.m_centre.x - rhs.m_centre.x) <= (lhs.m_radius.x + rhs.m_radius.x) &&
					Math.Abs(lhs.m_centre.y - rhs.m_centre.y) <= (lhs.m_radius.y + rhs.m_radius.y) &&
					Math.Abs(lhs.m_centre.z - rhs.m_centre.z) <= (lhs.m_radius.z + rhs.m_radius.z);
		}

		/// <summary>Create a RectangleF from the X,Y axes of this bounding box</summary>
		public RectangleF ToRectXY()
		{
			return new RectangleF(MinX, MinY, SizeX, SizeY);
		}
	}
}