//***************************************************
// Bounding Rectangle
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Drawing;

namespace pr.maths
{
	/// <summary>Bounding box</summary>
	[Serializable]
	public struct BRect
	{
		private v2 m_centre;
		private v2 m_radius;

		// Constructors
		public BRect(v2 centre, v2 radius)       { m_centre = centre; m_radius = radius; }
		public void  reset()                     { this = Reset; }
		public void  unit()                      { this = Unit; }
		public void  set(v2 centre, v2 radius)   { m_centre = centre; m_radius = radius; }
		public override string ToString()        { return "Centre=" + m_centre.ToString() + " Radius=" + m_radius.ToString(); }

		// Static types
		private readonly static BRect m_unit  = new BRect(v2.Zero, new v2(0.5f, 0.5f));
		private readonly static BRect m_reset = new BRect(v2.Zero, new v2(-1, -1));
		public static BRect Unit  { get { return m_unit; } }
		public static BRect Reset { get { return m_reset; } }

		// Binary operators
		public static BRect operator + (BRect lhs, v2 offset) { lhs.m_centre += offset; return lhs; }
		public static BRect operator - (BRect lhs, v2 offset) { lhs.m_centre -= offset; return lhs; }
		public static BRect operator * (BRect lhs, float s)   { lhs.m_radius *= s; return lhs; }
		public static BRect operator / (BRect lhs, float s)   { lhs.m_radius /= s; return lhs; }
		//public static BRect operator * (m3x4 m, BRect rhs)
		//{
		//	Debug.Assert(rhs.IsValid, "Transforming an invalid bounding box");
		//	BRect bb = new BRect(m.p, v2.Zero);
		//	m2x2 mat = m2x2.Transpose2x2(m);
		//	for (int i = 0; i != 2; ++i)
		//	{
		//		bb.m_centre[i] += v2.Dot2(       mat[i] , rhs.m_centre);
		//		bb.m_radius[i] += v2.Dot2(v2.Abs(mat[i]), rhs.m_radius);
		//	}
		//	return bb;
		//}

		// Equality operators
		public static bool operator == (BRect lhs, BRect rhs) { return lhs.m_centre == rhs.m_centre; }
		public static bool operator != (BRect lhs, BRect rhs) { return !(lhs == rhs); }
		public override bool Equals(object o)                 { return o is BRect && (BRect)o == this; }
		public override int GetHashCode()                     { unchecked { return m_centre.GetHashCode() ^ m_radius.GetHashCode(); } }

		// Conversion
		public static implicit operator BRect(RectangleF r) { return new BRect(new v2(r.X+r.Width/2, r.Y+r.Height/2), new v2(r.Width/2, r.Height/2)); }
		public static implicit operator RectangleF(BRect r) { return new RectangleF(r.m_centre.x - r.m_radius.x, r.m_centre.y - r.m_radius.y, 2*r.m_radius.x, 2*r.m_radius.y); }
		public Rectangle ToRectangle()
		{
			return new Rectangle(
				(int)Math.Round(m_centre.x - m_radius.x),
				(int)Math.Round(m_centre.y - m_radius.y),
				(int)Math.Round(2*m_radius.x),
				(int)Math.Round(2*m_radius.y));
		}

		/// <summary>Returns true if the bounding box represents a point or volume</summary>
		public bool IsValid
		{
			get { return Volume >= 0.0f; }
		}

		/// <summary>Returns the lower corner of the bounding box</summary>
		public v2 Lower()
		{
			return m_centre - m_radius;
		}

		/// <summary>Returns the upper corner of the bounding box</summary>
		public v2 Upper()
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

		/// <summary>Get/Set the x dimension of the bounding box</summary>
		public float SizeX
		{
			get { return 2.0f * m_radius.x; }
			set { m_radius.x = value * 0.5f; }
		}

		/// <summary>Gets the y dimension of the bounding box</summary>
		public float SizeY
		{
			get { return 2.0f * m_radius.y; }
			set { m_radius.y = value * 0.5f; }
		}

		/// <summary>Get/Sets the centre point of the bounding box</summary>
		public v2 Centre
		{
			get { return m_centre; }
			set { m_centre = value; }
		}

		/// <summary>Get/Sets the dimensions of the bounding box</summary>
		public v2 Radius
		{
			get { return m_radius; }
			set { m_radius = value; }
		}

		/// <summary>Gets the squared length of the diagonal of the bounding box</summary>
		public float DiametreSq
		{
			get { return 4.0f * m_radius.Length2Sq; }
		}

		/// <summary>Gets the length of the diagonal of the bounding box</summary>
		public float Diametre
		{
			get { return Maths.Sqrt(DiametreSq); }
		}

		/// <summary>Gets the volume of the bounding box</summary>
		public float Volume
		{
			get { return SizeX * SizeY; }
		}

		/// <summary>Returns a corner of the bounding box. 'corner'</summary>
		public v2 GetCorner(uint corner)
		{
			Debug.Assert(corner < 4, "Invalid corner index");
			uint x = ((corner >> 0) & 0x1) * 2 - 1;
			uint y = ((corner >> 1) & 0x1) * 2 - 1;
			return new v2(m_centre.x + x*m_radius.x, m_centre.y + y*m_radius.y);
		}

		/// <summary>Expands the bounding box to include 'point'</summary>
		public void Encompass(v2 point)
		{
			for (int i = 0; i != 2; ++i)
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
		public void Encompass(BRect rhs)
		{
			Debug.Assert(rhs.IsValid, "Encompasing an invalid bounding box");
			Encompass(rhs.m_centre + rhs.m_radius);
			Encompass(rhs.m_centre - rhs.m_radius);
		}

		/// <summary>Returns true if 'point' is within this bounding box (within 'tol'erance)</summary>
		public bool IsWithin(v2 point, float tol)
		{
			return
				Math.Abs(point.x - m_centre.x) <= m_radius.x + tol &&
				Math.Abs(point.y - m_centre.y) <= m_radius.y + tol;
		}

		/// <summary>Returns true if 'brect' is within this bounding box (within 'tol'erance)</summary>
		public bool IsWithin(BRect bbox, float tol)
		{
			return
				Math.Abs(bbox.m_centre.x - m_centre.x) <= (m_radius.x - bbox.m_radius.x + tol) &&
				Math.Abs(bbox.m_centre.y - m_centre.y) <= (m_radius.y - bbox.m_radius.y + tol);
		}

		/// <summary>Returns true if 'rhs' intersects with this bounding box</summary>
		public static bool IsIntersection(BRect lhs, BRect rhs)
		{
			return
				Math.Abs(lhs.m_centre.x - rhs.m_centre.x) <= (lhs.m_radius.x + rhs.m_radius.x) &&
				Math.Abs(lhs.m_centre.y - rhs.m_centre.y) <= (lhs.m_radius.y + rhs.m_radius.y);
		}

		/// <summary>Returns the bounding rectangle shifted by [dx,dy]</summary>
		public BRect Shifted(v2 offset)
		{
			return new BRect(m_centre + offset, m_radius);
		}
		public BRect Shifted(float dx, float dy)
		{
			return Shifted(new v2(dx, dy));
		}
	}
}