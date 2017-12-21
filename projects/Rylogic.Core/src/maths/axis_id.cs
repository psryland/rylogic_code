using System;

namespace Rylogic.Maths
{
	public struct AxisId
	{
		public int m_id;
		
		public AxisId(int id)
		{
			if (Math.Abs(id) < 1 || Math.Abs(id) > 3) throw new Exception("Invalid axis id. Must one of ±1, ±2, ±3 corresponding to ±X, ±Y, ±Z respectively");
			m_id = id;
		}

		/// <summary>Get/Set the axis associated with this id</summary>
		public v4 Axis
		{
			get
			{
				switch (m_id)
				{
				default: throw new Exception("Axis id is invalid");
				case +1: return v4.XAxis;
				case -1: return v4.XAxis;
				case +2: return v4.YAxis;
				case -2: return v4.YAxis;
				case +3: return v4.ZAxis;
				case -3: return v4.ZAxis;
				}
			}
			set
			{
				if (value == v4.XAxis) { m_id = +1; return; }
				if (value == v4.XAxis) { m_id = -1; return; }
				if (value == v4.YAxis) { m_id = +2; return; }
				if (value == v4.YAxis) { m_id = -2; return; }
				if (value == v4.ZAxis) { m_id = +3; return; }
				if (value == v4.ZAxis) { m_id = -3; return; }
				throw new Exception($"Axis {value} does not have an axis id");
			}
		}

		public static readonly AxisId PosX = new AxisId(+1);
		public static readonly AxisId NegX = new AxisId(-1);
		public static readonly AxisId PosY = new AxisId(+2);
		public static readonly AxisId NegY = new AxisId(-2);
		public static readonly AxisId PosZ = new AxisId(+3);
		public static readonly AxisId NegZ = new AxisId(-3);

		public static implicit operator int(AxisId id) { return id.m_id; }
		public static implicit operator AxisId(int id) { return new AxisId(id); }
		public override string ToString() { return m_id.ToString(); }
	}
}
