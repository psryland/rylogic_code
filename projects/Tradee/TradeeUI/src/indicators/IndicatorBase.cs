using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Base class for instrument indicators</summary>
	public abstract class IndicatorBase :ChartControl.Element, IDisposable
	{
		/// <summary>Buffers for creating the chart graphics</summary>
		protected List<View3d.Vertex> m_vbuf;
		protected List<ushort>        m_ibuf;
		protected List<View3d.Nugget> m_nbuf;

		protected IndicatorBase(Guid id, string name, ISettingsSet settings)
			:base(id, m4x4.Identity, name)
		{
			IndicatorSettingsInternal = settings;
			Init();
		}
		protected IndicatorBase(XElement node)
			:base(node)
		{
			IndicatorSettingsInternal = (ISettingsSet)node.Element(XmlTag.Settings).ToObject();
			Init();
		}
		private void Init()
		{
			m_vbuf = new List<View3d.Vertex>();
			m_ibuf = new List<ushort>();
			m_nbuf = new List<View3d.Nugget>();

			// Don't set the instrument on construction, most indicators will want to sign up to events
			VisibleToFindRange = false;
		}
		protected override void Dispose(bool disposing)
		{
			IndicatorSettingsInternal = null;
			Instrument = null;
			base.Dispose(disposing);
		}

		/// <summary>Export to XML</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(XmlTag.Settings, IndicatorSettingsInternal, true);
			return node;
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

		/// <summary>Settings for this indicator</summary>
		protected ISettingsSet IndicatorSettingsInternal
		{
			get { return m_settings; }
			set
			{
				if (ReferenceEquals(m_settings, value)) return;
				if (m_settings != null)
				{
					m_settings.SettingChanged -= HandleSettingChanged;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChanged += HandleSettingChanged;
				}
			}
		}
		private ISettingsSet m_settings;

		/// <summary>Save this indicator in the PerChartSettings, can recreate when the chart is loaded</summary>
		public virtual bool SaveWithChart
		{
			get { return true; }
		}

		/// <summary>True if this indicator can be dragged around</summary>
		public virtual bool Dragable
		{
			get { return false; }
		}
		public virtual ChartControl.MouseOp CreateDragMouseOp()
		{
			throw new NotSupportedException("Indicator not drag-able");
		}

		/// <summary>Update the instrument that this indicator applies to</summary>
		protected virtual void SetInstrumentCore(Instrument instr)
		{
			// Don't clone the instrument, derived types choose whether the instrument is cloned or not
			m_instr = instr;
		}

		/// <summary>Called when the instrument data is modified or added to</summary>
		protected virtual void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{}

		/// <summary>Called when the instrument time frame changes</summary>
		protected virtual void HandleTimeFrameChanged(object sender, EventArgs e)
		{}

		/// <summary>Update the graphics and chart when the settings change</summary>
		protected virtual void HandleSettingChanged(object sender, SettingChangedEventArgs e)
		{
			// Notify chart element changed
			OnDataChanged();

			// Invalidate this chart element and the chart
			Invalidate();
			InvalidateChart();
		}
	}
}
