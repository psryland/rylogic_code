using System;
using Rylogic.Attrib;

namespace Rylogic.Maths
{
	public enum EAxisId
	{
		[Desc("+X Axis")] PosX = +1,
		[Desc("-X Axis")] NegX = -1,
		[Desc("+Y Axis")] PosY = +2,
		[Desc("-Y Axis")] NegY = -2,
		[Desc("+Z Axis")] PosZ = +3,
		[Desc("-Z Axis")] NegZ = -3,
	}

	public struct AxisId
	{
		public AxisId(EAxisId id)
		{
			if (Math.Abs((int)id) < 1 || Math.Abs((int)id) > 3)
				throw new Exception("Invalid axis id. Must one of ±1, ±2, ±3 corresponding to ±X, ±Y, ±Z respectively");

			Id = id;
		}

		/// <summary></summary>
		public EAxisId Id { get; set; }

		/// <summary>Get/Set the axis associated with this id</summary>
		public v4 Axis
		{
			get
			{
				switch (Id)
				{
				default: throw new Exception("Axis id is invalid");
				case EAxisId.PosX: return v4.XAxis;
				case EAxisId.NegX: return v4.XAxis;
				case EAxisId.PosY: return v4.YAxis;
				case EAxisId.NegY: return v4.YAxis;
				case EAxisId.PosZ: return v4.ZAxis;
				case EAxisId.NegZ: return v4.ZAxis;
				}
			}
			set
			{
				if (value == +v4.XAxis) { Id = EAxisId.PosX; return; }
				if (value == -v4.XAxis) { Id = EAxisId.NegX; return; }
				if (value == +v4.YAxis) { Id = EAxisId.PosY; return; }
				if (value == -v4.YAxis) { Id = EAxisId.NegY; return; }
				if (value == +v4.ZAxis) { Id = EAxisId.PosZ; return; }
				if (value == -v4.ZAxis) { Id = EAxisId.NegZ; return; }
				throw new Exception($"Axis {value} does not have an axis id");
			}
		}

		public static implicit operator EAxisId(AxisId id) { return id.Id; }
		public static implicit operator AxisId(EAxisId id) { return new AxisId(id); }

		public static implicit operator int(AxisId id) { return (int)id.Id; }
		public static implicit operator AxisId(int id) { return new AxisId((EAxisId)id); }

		public override string ToString() => ((int)Id).ToString();
	}
}
