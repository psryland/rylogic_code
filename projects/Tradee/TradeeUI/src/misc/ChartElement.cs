using System;
using System.Diagnostics;
using System.Xml.Linq;
using pr.gui;
using pr.maths;

namespace Tradee
{
	/// <summary>Base class for graphics elements added to a chart</summary>
	public abstract class ChartElement :ChartControl.Element
	{
		protected ChartElement(Guid id, string name)
			:base(id, m4x4.Identity, name)
		{
			VisibleToFindRange = false;
		}
		protected ChartElement(XElement node)
			:base(node)
		{
			VisibleToFindRange = false;
		}
		protected override void Dispose(bool disposing)
		{
			Instrument = null;
			base.Dispose(disposing);
		}

		/// <summary>The parent UI</summary>
		protected MainModel Model
		{
			[DebuggerStepThrough] get { return Instrument.Model; }
		}

		/// <summary>The instrument that this indicator is associated with</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instr; }
			set
			{
				if (m_instr == value) return;
				if (m_instr != null)
				{
					m_instr.DataChanged -= HandleInstrumentDataChanged;
					m_instr.TimeFrameChanged -= HandleTimeFrameChanged;
					// The indicator does not own the instrument
				}
				SetInstrumentCore(value);
				if (m_instr != null)
				{
					Debug.Assert(m_instr.TimeFrame != ETimeFrame.None);
					m_instr.TimeFrameChanged += HandleTimeFrameChanged;
					m_instr.DataChanged += HandleInstrumentDataChanged;
				}
			}
		}
		private Instrument m_instr;

		/// <summary>Update the instrument that this indicator applies to</summary>
		protected virtual void SetInstrumentCore(Instrument instr)
		{
			// Don't clone the instrument, derived types choose whether the instrument is cloned or not
			m_instr = instr;
		}

		/// <summary>Called when the instrument time frame changes</summary>
		protected virtual void HandleTimeFrameChanged(object sender, EventArgs e)
		{}

		/// <summary>Called when the instrument data is modified or added to</summary>
		protected virtual void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{}
	}
}
