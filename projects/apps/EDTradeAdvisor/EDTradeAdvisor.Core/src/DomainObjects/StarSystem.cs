using System;
using System.Diagnostics;
using Rylogic.Maths;

namespace EDTradeAdvisor.DomainObjects
{
	[DebuggerDisplay("{Name}")]
	public class StarSystem
	{
		/// <summary></summary>
		public long ID { get; set; }

		/// <summary>The ID of this system used by EDSM</summary>
		public long EdsmID { get; set; }

		/// <summary></summary>
		public string Name { get; set; }

		/// <summary></summary>
		public double X { get; set; }

		/// <summary></summary>
		public double Y { get; set; }

		/// <summary></summary>
		public double Z { get; set; }

		/// <summary></summary>
		public long Population { get; set; }

		/// <summary>True if a permit is required to enter this system</summary>
		public bool NeedPermit { get; set;}

		/// <summary></summary>
		public long UpdatedAt { get; set; }

		/// <summary></summary>
		public v4 Position => new((float)X, (float)Y, (float)Z, 1f);

		/// <summary></summary>
		public DateTimeOffset LastUpdated => DateTimeOffset.FromUnixTimeSeconds(UpdatedAt);

		#region Equals
		public bool Equals(StarSystem rhs)
		{
			return ID == rhs.ID;
		}
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return ID.GetHashCode();
		}
		#endregion
	}
}
