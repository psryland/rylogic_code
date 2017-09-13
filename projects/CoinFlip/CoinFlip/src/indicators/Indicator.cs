using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	/// <summary>Base class for indicators</summary>
	public abstract class Indicator :IDisposable
	{
		/// <summary>Cached buffers for creating the graphics</summary>
		protected List<View3d.Vertex> m_vbuf;
		protected List<ushort>        m_ibuf;
		protected List<View3d.Nugget> m_nbuf;

		private Indicator()
		{
			m_vbuf = new List<View3d.Vertex>();
			m_ibuf = new List<ushort>();
			m_nbuf = new List<View3d.Nugget>();
		}
		protected Indicator(string name, ISettingsSet settings)
			:this()
		{
			Name = name;
			SettingsInternal = settings;
		}
		protected Indicator(XElement node)
			:this()
		{
			Name = name;
			SettingsInternal = (ISettingsSet)node.Element(nameof(Settings)).ToObject();
		}
		public virtual void Dispose()
		{
			Instrument = null;
			SettingsInternal = null;
		}
		protected virtual XElement ToXml(XElement node)
		{
			node.Add2(nameof(Settings), SettingsInternal, true);
			return node;
		}

		/// <summary>A string description of the indicator</summary>
		public string Name
		{
			get { return m_name; }
			set
			{
				if (m_name == value) return;
				m_name = value;
				Invalidate();
			}
		}
		private string m_name;

		/// <summary>The instrument that this indicator is associated with</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instr; }
			set
			{
				if (m_instr == value) return;
				if (m_instr != null)
				{
					//m_instr.DataChanged -= HandleInstrumentDataChanged;
					//m_instr.TimeFrameChanged -= HandleTimeFrameChanged;
					// The indicator does not own the instrument
				}
				SetInstrumentCore(value);
				if (m_instr != null)
				{
					//Debug.Assert(m_instr.TimeFrame != ETimeFrame.None);
					//m_instr.TimeFrameChanged += HandleTimeFrameChanged;
					//m_instr.DataChanged += HandleInstrumentDataChanged;
				}
			}
		}
		private Instrument m_instr;
		protected virtual void SetInstrumentCore(Instrument instr)
		{
			// Don't clone the instrument, derived types choose whether the instrument is cloned or not
			m_instr = instr;
		}

		/// <summary>Settings for this indicator</summary>
		protected ISettingsSet SettingsInternal
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
		protected virtual void HandleSettingChanged(object sender, SettingChangedEventArgs e)
		{
		//	// Notify chart element changed
		//	OnDataChanged();
		//
		//	// Invalidate this chart element and the chart
		//	Invalidate();
		//	InvalidateChart();
		}

		/// <summary>Invalidate this indicator, causing its graphics to be recreated</summary>
		public void Invalidate(object sender = null, EventArgs args = null)
		{
			Dirty = EDirtyFlags.RecreateModel;
		}

		/// <summary>Dirty flag for updating indicator graphics</summary>
		protected EDirtyFlags Dirty { get; set; }
		protected enum EDirtyFlags
		{
			None = 0,
			RecreateModel = 1 << 0,
		}

		/// <summary>Update the graphics for this indicator and add it to the scene</summary>
		public virtual void AddToScene(ChartControl.ChartRenderingEventArgs args)
		{
		}
	}
}
