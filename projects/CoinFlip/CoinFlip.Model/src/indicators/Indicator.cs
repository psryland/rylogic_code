using System;
using System.Diagnostics;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;

namespace CoinFlip
{
	/// <summary>Base class for indicators</summary>
	public abstract class Indicator :ChartControl.Element ,IDisposable
	{
		// Notes:
		//  Indicators are chart elements so that they can be hit tested.
		//  It doesn't make sense to move indicators between charts anyway.

		protected Indicator(Guid id, string name, ISettingsSet settings)
			:base(id, m4x4.Identity, name)
		{
			SettingsInternal = settings;
		}
		protected Indicator(XElement node)
			:base(node)
		{
			SettingsInternal = (ISettingsSet)node.Element(XmlTag.Settings).ToObject();
		}
		protected override void Dispose(bool disposing)
		{
			Instrument = null;
			SettingsInternal = null;
			base.Dispose(disposing);
		}
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(XmlTag.Settings, SettingsInternal, true);
			return node;
		}

		/// <summary>The instrument that this indicator is associated with</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instrument; }
			set
			{
				if (m_instrument == value) return;
				if (m_instrument != null)
				{
					m_instrument.DataChanged -= HandleInstrumentDataChanged;
				}
				SetInstrumentCore(value);
				if (m_instrument != null)
				{
					m_instrument.DataChanged += HandleInstrumentDataChanged;
				}
				Reset();
			}
		}
		private Instrument m_instrument;
		protected virtual void SetInstrumentCore(Instrument instr)
		{
			m_instrument = instr;
		}
		protected virtual void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			Invalidate();
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
					m_settings.SettingChange -= HandleSettingChange;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChange += HandleSettingChange;
				}
			}
		}
		private ISettingsSet m_settings;
		protected virtual void HandleSettingChange(object sender, SettingChangeEventArgs e)
		{
			if (e.Before) return;
			Invalidate();
			InvalidateChart();
		}

		/// <summary>Set the owning chart</summary>
		protected override void SetChartCore(ChartControl chart)
		{
			if (Chart != null)
				Chart.ChartMoved -= HandleChartMoved;
			base.SetChartCore(chart);
			if (Chart != null)
				Chart.ChartMoved += HandleChartMoved;
		}
		protected virtual void HandleChartMoved(object sender, ChartControl.ChartMovedEventArgs e)
		{}

		/// <summary>Update the graphics model for this indicator</summary>
		protected override void UpdateGfxCore()
		{
			// Recreate underlying data if necessary
			ResetIfRequired();
		}

		/// <summary>Get/Set a flag to indicator the underlying data need recalculating</summary>
		public bool ResetRequired { get; set; }

		/// <summary>Recreate the underlying data for the indicator</summary>
		public void Reset()
		{
			ResetCore();
			Invalidate();
			OnDataChanged();
			ResetRequired = false;
		}
		public void ResetIfRequired()
		{
			if (!ResetRequired) return;
			Reset();
		}
		protected virtual void ResetCore()
		{}

		/// <summary>True if this indicator can be dragged around</summary>
		public virtual bool Dragable
		{
			get { return false; }
		}
		public virtual ChartControl.MouseOp CreateDragMouseOp(ChartControl.HitTestResult hit)
		{
			throw new NotSupportedException("Indicator not drag-able");
		}
	}
}
