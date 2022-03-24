using System.Collections.Generic;
using System.Linq;
using EDTradeAdvisor.DomainObjects;
using Rylogic.Container;
using Rylogic.Maths;
using Rylogic.Utility;

namespace EDTradeAdvisor
{
	public class Map
	{
		// Notes:
		//  - This data structure is used to optimally find the systems around a given point

		private readonly List<StarSystemRef> m_tree;
		public Map()
		{
			m_tree = new List<StarSystemRef>();
		}

		/// <summary>True if the map needs building</summary>
		public bool BuildNeeded => m_tree.Count == 0;
		public void Invalidate() => m_tree.Clear();

		/// <summary>Rebuild the map</summary>
		public void BuildSystemMap(IEnumerable<StarSystem> stars)
		{
			Log.Write(ELogLevel.Info, $"Building systems map");
			using (StatusStack.Instance.Push($"Updating star map..."))
			{
				m_tree.Clear();
				m_tree.AddRange(stars.Select(x => new StarSystemRef(x)));
				KDTree_.Build(m_tree, 3);
			}
		}

		/// <summary>Search the map for nearby systems</summary>
		public IEnumerable<StarSystemRef> Search(v4 position, double radius)
		{
			return KDTree_.Search(m_tree, 3, (double[])position, radius);
		}
	}
}
