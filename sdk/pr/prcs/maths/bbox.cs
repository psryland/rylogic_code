//***************************************************
// Bounding Box
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;

namespace pr.maths
{
	/// <summary>Bounding box</summary>
	[Serializable]
	public struct BBox
	{
		private v4 m_centre;
		private v4 m_radius;

		// Constructors
		public BBox(v4 centre, v4 radius)        { m_centre = centre; m_radius = radius; }
		public void  reset()                     { this = Reset; }
		public void  unit()                      { this = Unit; }
		public void  set(v4 centre, v4 radius)   { m_centre = centre; m_radius = radius; }
		public override string ToString()        { return "Centre=" + m_centre.ToString3() + " Radius=" + m_radius.ToString3(); }

		// Static v4 types
		private readonly static BBox m_unit = new BBox(v4.Origin, new v4(0.5f, 0.5f, 0.5f, 0));
		private readonly static BBox m_reset = new BBox(v4.Origin, new v4(-1, -1, -1, 0));
		public static BBox Unit  { get { return m_unit; } }
		public static BBox Reset { get { return m_reset; } }

		// Binary operators
		public static BBox operator + (BBox lhs, v4 offset)    { lhs.m_centre += offset; return lhs; }
		public static BBox operator - (BBox lhs, v4 offset)    { lhs.m_centre -= offset; return lhs; }
		public static BBox operator * (BBox lhs, float s)      { lhs.m_radius *= s; return lhs; }
		public static BBox operator / (BBox lhs, float s)      { lhs.m_radius /= s; return lhs; }
		public static BBox operator * (m4x4 m, BBox rhs)
		{
			Debug.Assert(rhs.IsValid, "Transforming an invalid bounding box");
			BBox bb = new BBox(m.p, v4.Zero);
			m4x4 mat = m4x4.Transpose3x3(m);
			for (int i = 0; i != 3; ++i)
			{
				bb.m_centre[i] += v4.Dot4(       mat[i] , rhs.m_centre);
				bb.m_radius[i] += v4.Dot4(v4.Abs(mat[i]), rhs.m_radius);
			}
			return bb;
		}

		// Equality operators
		public static bool operator == (BBox lhs, BBox rhs) { return lhs.m_centre == rhs.m_centre; }
		public static bool operator != (BBox lhs, BBox rhs) { return !(lhs == rhs); }
		public override bool Equals(object o)               { return o is BBox && (BBox)o == this; }
		public override int GetHashCode()                   { unchecked { return m_centre.GetHashCode() ^ m_radius.GetHashCode(); } }

		/// <summary>Returns true if the bounding box represents a point or volume</summary>
		public bool IsValid
		{
			get { return m_radius.x >= 0f && m_radius.y >= 0f && m_radius.z >= 0f; }
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
			get { return 4.0f * m_radius.Length3Sq; }
		}

		/// <summary>Gets the length of the diagonal of the bounding box</summary>
		public float Diametre
		{
			get { return Maths.Sqrt(DiametreSq); }
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

		/// <summary>Expands the bounding box to include 'point'</summary>
		public void Encompass(v4 point)
		{
			for (int i = 0; i != 3; ++i)
			{
				if (m_radius[i] < 0.0f)
				{
					m_centre[i] = point[i];
					m_radius[i] = 0.0f;
				}
				else
				{
					float signed_dist = point[i] - m_centre[i];
					float length      = Math.Abs(signed_dist);
					if (length > m_radius[i])
					{
						float new_radius = (length + m_radius[i]) / 2.0f;
						m_centre[i] += signed_dist * (new_radius - m_radius[i]) / length;
						m_radius[i] = new_radius;
					}
				}
			}
		}

		/// <summary>Expands the bounding box to include 'rhs'</summary>
		public void Encompass(BBox rhs)
		{
			Debug.Assert(rhs.IsValid, "Encompasing an invalid bounding box");
			Encompass(rhs.m_centre + rhs.m_radius);
			Encompass(rhs.m_centre - rhs.m_radius);
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

		/// <summary>Returns true if 'rhs' intersects with this bounding box</summary>
		public static bool IsIntersection(BBox lhs, BBox rhs)
		{
			return  Math.Abs(lhs.m_centre.x - rhs.m_centre.x) <= (lhs.m_radius.x + rhs.m_radius.x) &&
					Math.Abs(lhs.m_centre.y - rhs.m_centre.y) <= (lhs.m_radius.y + rhs.m_radius.y) &&
					Math.Abs(lhs.m_centre.z - rhs.m_centre.z) <= (lhs.m_radius.z + rhs.m_radius.z);
		}
	}
}