using System.Xml.Linq;
using Rylogic.Extn;

namespace EDTradeAdvisor.DomainObjects
{
	public struct LocationID
	{
		public LocationID(long star_system_id, long station_id)
		{
			StarSystemID = star_system_id;
			StationID = station_id;
		}
		public LocationID(XElement node)
		{
			StarSystemID = node.Element(nameof(StarSystemID)).As<long>();
			StationID = node.Element(nameof(StationID)).As<long>();
		}
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(StarSystemID), StarSystemID, false);
			node.Add2(nameof(StationID), StationID, false);
			return node;
		}

		/// <summary></summary>
		public long StarSystemID { get; set; }

		/// <summary></summary>
		public long StationID { get; set; }

		#region Equals
		public static bool operator ==(LocationID lhs, LocationID rhs)
		{
			return lhs.Equals(rhs);
		}
		public static bool operator !=(LocationID lhs, LocationID rhs)
		{
			return !lhs.Equals(rhs);
		}
		public bool Equals(LocationID rhs)
		{
			return
				StarSystemID == rhs.StarSystemID &&
				StationID == rhs.StationID;
		}
		public override bool Equals(object obj)
		{
			return obj is LocationID loc && Equals(loc);
		}
		public override int GetHashCode()
		{
			return new { StarSystemID, StationID }.GetHashCode();
		}
		#endregion
	}
}
