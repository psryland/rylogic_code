//***********************************************
// Singleton
//	(c)opyright Paul Ryland 2008
//***********************************************

using System;
using System.Collections.Generic;

namespace PR
{
	public class Grid :IComparer<v4>
	{
		private readonly v4[]	m_container;
		private readonly int[]	m_grid;
		private readonly v4		m_min;
		private readonly v4		m_size;
		private readonly v4		m_dim;

		private int GridX(v4 vec) { return (int)((vec.x - m_min.x) / m_size.x); }
		private int GridY(v4 vec) { return (int)((vec.y - m_min.y) / m_size.y); }
		private int GridZ(v4 vec) { return (int)((vec.z - m_min.z) / m_size.z); }

	    public Grid(v4[] container, v4 min, v4 max, v4 dim)
	    {
	        m_container = container;
			m_min = min;
			m_dim = dim;
			m_size = v4.ComponentDivide(max - min, m_dim);
			Array.Sort(m_container, this);

            // Find the bucket boundaries
			m_grid = new int[m_dim.ix * m_dim.iy * m_dim.iz + 1];
			int i = 0, i_end = m_container.Length;
			for( int z = 0; z != m_dim.iz; ++z )
			{
				for( int y = 0; y != m_dim.iy; ++y )
				{
					for( int x = 0; x != m_dim.ix; ++x )
					{
						m_grid[x + y*m_dim.ix + z*m_dim.iy*m_dim.ix] = i;
						while( i != i_end && GridX(m_container[i]) <= x && GridY(m_container[i]) <= y && GridZ(m_container[i]) <= z )
							++i;
					}
				}
			}
			m_grid[m_dim.ix*m_dim.iy*m_dim.iz] = m_container.Length;
		}

		/// <summary>
		/// Compare v4's
		/// </summary>
		public int Compare(v4 lhs, v4 rhs)
		{
			int l, r;
			
			l = GridZ(lhs); r = GridZ(rhs);
			if( l != r ) return l < r ? -1 : 1;

			l = GridY(lhs); r = GridY(rhs);
			if( l != r ) return l < r ? -1 : 1;
			
			l = GridX(lhs); r = GridX(rhs);
			if( l != r ) return l < r ? -1 : 1;
			
			return 0;
		}

		/// <summary>
		/// Iterate over points within a radius of position
		/// </summary>
		public IEnumerable<v4> Search(v4 position, float radius)
		{
			float radius_sq = radius * radius;
			v4 rad = new v4(radius, radius, radius, 0f);
			v4 min = v4.Clamp3(v4.ComponentDivide(position - m_min - rad, m_size), v4.Zero, m_dim - v4.One);
			v4 max = v4.Clamp3(v4.ComponentDivide(position - m_min + rad, m_size), v4.Zero, m_dim - v4.One);
			for( int z = min.iz; z <= max.iz; ++z )
			{
				for( int y = min.iy; y <= max.iy; ++y )
				{
					for( int x = min.ix; x <= max.ix; ++x )
					{
						int k = x + y*m_dim.ix + z*m_dim.iy*m_dim.ix;
						for( int i = m_grid[k]; i != m_grid[k+1]; ++i )
						{
							if( (m_container[i] - position).Length3Sq < radius_sq )
								yield return m_container[i];
						}
					}
				}
			}
		}
	}
}
