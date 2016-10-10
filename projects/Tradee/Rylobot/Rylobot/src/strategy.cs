using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.util;

namespace Rylobot
{
	public abstract class Strategy :IDisposable
	{
		public Strategy(RylobotModel model)
		{
			Model = model;
		}
		public virtual void Dispose()
		{
			Model = null;
		}

		/// <summary>Application logic</summary>
		public RylobotModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.PositionClosed -= HandlePositionClosed;
					Util.Dispose(ref m_model);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.PositionClosed += HandlePositionClosed;
				}
			}
		}
		private RylobotModel m_model;

		/// <summary>Step the strategy</summary>
		public abstract void Step();

		/// <summary>Called when a position closes</summary>
		protected virtual void HandlePositionClosed(object sender, PositionEventArgs e)
		{}
	}
}
