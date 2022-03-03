using System.Diagnostics;

namespace EDTradeAdvisor.DomainObjects
{
	[DebuggerDisplay("{Description,nq}")]
	public class Location
	{
		public Location(StarSystem system, Station station)
		{
			System = system;
			Station = station;
		}

		/// <summary></summary>
		public StarSystem System { get; set; }

		/// <summary></summary>
		public Station Station { get; set; }

		/// <summary></summary>
		public static implicit operator LocationID(Location loc) => new LocationID(loc.System?.ID, loc.Station?.ID);

		/// <summary></summary>
		public string Description => $"{System.Name}/{Station.Name}";

		#region Equals
		public bool Equals(Location rhs)
		{
			return System.Equals(rhs.System) && Station.Equals(rhs.Station);
		}
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return new { System, Station }.GetHashCode();
		}
		#endregion
	}
}
