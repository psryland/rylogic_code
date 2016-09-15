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
	public abstract class IndicatorBase :ChartElement
	{
		protected IndicatorBase(Guid id, string name, ISettingsSet settings)
			:base(id, name)
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
			// Don't set the instrument on construction,
			// most indicators will want to sign up to events
		}
		protected override void Dispose(bool disposing)
		{
			IndicatorSettingsInternal = null;
			base.Dispose(disposing);
		}

		/// <summary>Export to XML</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(XmlTag.Settings, IndicatorSettingsInternal, true);
			return node;
		}

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
