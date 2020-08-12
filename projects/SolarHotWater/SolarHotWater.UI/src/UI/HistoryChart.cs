using System;
using System.Collections.Generic;
using System.Text;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace SolarHotWater.UI
{
	public partial class MainWindow
	{
		/// <summary>Time Zero</summary>
		public static readonly DateTimeOffset Epoch = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

		/// <summary>Solar output history</summary>
		private ChartDataSeries m_chartdata_solar = new ChartDataSeries("Solar Output", ChartDataSeries.EFormat.XIntg | ChartDataSeries.EFormat.YReal);

		/// <summary></summary>
		private void LoadHistory()
		{
		}

		/// <summary>The history chart control</summary>
		public ChartControl Chart
		{
			get => m_chart;
			private set
			{
				if (m_chart == value) return;
				if (m_chart != null)
				{
					m_chart.BuildScene -= HandleBuildScene;
					m_chart.XAxis.TickText = m_chart.XAxis.DefaultTickText;
					Util.Dispose(ref m_chart!);
				}
				m_chart = value;
				if (m_chart != null)
				{
					m_chart.XAxis.TickText = HandleChartXAxisTickText;
					m_chart.BuildScene += HandleBuildScene;
				}

				// Handlers
				string HandleChartXAxisTickText(double x, double? step = null)
				{
					//// The range of indices
					//var first = 0;
					//var last = Instrument.Count;

					var prev = (int)(x - step ?? 0.0);
					var curr = (int)(x);

					// If the current tick mark represents the same time as the previous one, no text is required
					if (prev == curr && step != null)
						return string.Empty;

					// Get the date time for the tick
					var dt_curr = Epoch + TimeSpan.FromHours(curr);
					//	curr < first - 1000 ? default :
					//	curr < first ? Instrument[0].TimestampUTC + Misc.TimeFrameToTimeSpan(curr - first, Instrument.TimeFrame) :
					//	curr < last ? Instrument[curr].TimestampUTC :
					//	curr < last + 1000 ? Instrument[last - 1].TimestampUTC + Misc.TimeFrameToTimeSpan(curr - last + 1, Instrument.TimeFrame) :
					//	default;
					var dt_prev = Epoch + TimeSpan.FromHours(prev);
					//	prev < first - 1000 ? default :
					//	prev < first ? Instrument[0].TimestampUTC + Misc.TimeFrameToTimeSpan(prev - first, Instrument.TimeFrame) :
					//	prev < last ? Instrument[prev].TimestampUTC :
					//	prev < last + 1000 ? Instrument[last - 1].TimestampUTC + Misc.TimeFrameToTimeSpan(prev - last + 1, Instrument.TimeFrame) :
					//	default;
					if (dt_curr == default || dt_prev == default)
						return string.Empty;

					//// Get the date time values in the correct time zone
					//dt_curr = (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.LocalTime)
					//	? dt_curr.LocalDateTime
					//	: dt_curr.UtcDateTime;
					//dt_prev = (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.LocalTime)
					//	? dt_prev.LocalDateTime
					//	: dt_prev.UtcDateTime;

					// First tick on the x axis
					var first_tick = //curr == first || prev < first || 
						step == null || x - step < Chart.XAxis.Min;

					// Show more of the time stamp depending on how it differs from the previous time stamp
					return ShortTimeString(dt_curr, dt_prev, first_tick);
					//return x.ToString();
				}
				/// <summary>Return a timestamp string suitable for a chart X tick value</summary>
				static string ShortTimeString(DateTimeOffset dt_curr, DateTimeOffset dt_prev, bool first)
				{
					// First tick on the x axis
					if (first)
						return dt_curr.ToString("HH:mm'\r\n'dd-MMM-yy");

					// Show more of the time stamp depending on how it differs from the previous time stamp
					if (dt_curr.Year != dt_prev.Year)
						return dt_curr.ToString("HH:mm'\r\n'dd-MMM-yy");
					if (dt_curr.Month != dt_prev.Month)
						return dt_curr.ToString("HH:mm'\r\n'dd-MMM");
					if (dt_curr.Day != dt_prev.Day)
						return dt_curr.ToString("HH:mm'\r\n'ddd dd");

					return dt_curr.ToString("HH:mm");
				}

				void HandleBuildScene(object? sender, View3dControl.BuildSceneEventArgs e)
				{
					BuildScene();
				}
			}
		}
		private ChartControl m_chart = null!;

		/// <summary>Build the history scene</summary>
		private void BuildScene()
		{
			//	History.Solar()
			m_chartdata_solar.Chart = Chart;
		}

		private void UpdateChartSeriesData()
		{
			//m_chartdata_solar;
		}
	}

}
