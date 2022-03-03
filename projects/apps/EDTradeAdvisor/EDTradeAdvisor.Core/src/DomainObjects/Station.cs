using System;
using System.Diagnostics;

namespace EDTradeAdvisor.DomainObjects
{
	[DebuggerDisplay("{Name}")]
	public class Station
	{
		/// <summary></summary>
		public long ID { get; set; }

		/// <summary></summary>
		public string Name { get; set; }

		/// <summary>The ID of this system this station belongs to</summary>
		public long SystemID { get; set; }

		/// <summary>Station type</summary>
		public EStationType? Type { get; set; }

		/// <summary>Distance from the system arrival point in LS. Null means unknown</summary>
		public int? Distance { get; set; }

		/// <summary></summary>
		public EFacilities Facilities { get; set; }

		/// <summary>S,M,L Pad size</summary>
		public ELandingPadSize MaxPadSize { get; set; }

		/// <summary>Station is on a planet surface</summary>
		public bool Planetary { get; set; }

		/// <summary></summary>
		public long UpdatedAt { get; set; }

		/// <summary></summary>
		public DateTimeOffset LastUpdated => DateTimeOffset.FromUnixTimeSeconds(UpdatedAt);

		#region Equals
		public bool Equals(Station rhs)
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
