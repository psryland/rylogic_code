using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tradee
{
	/// <summary>Find and make trades</summary>
	public class AutoTrade :IDisposable
	{
		public AutoTrade(MainModel model)
		{
			Model = model;
		}
		public virtual void Dispose()
		{
			Model = null;
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				//	Model.Acct.AccountChanged -= HandleAcctChanged;
				}
				m_model = value;
				if (m_model != null)
				{
				//	Model.Acct.AccountChanged += HandleAcctChanged;
				}
			}
		}
		private MainModel m_model;


		public void Step()
		{
			// Look for:
			//  
		}

	}
}
