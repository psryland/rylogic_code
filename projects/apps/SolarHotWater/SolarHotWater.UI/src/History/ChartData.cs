using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace SolarHotWater
{
	public sealed class ChartData :IDisposable
	{
		public ChartData(ChartControl chart, History history, IReadOnlyList<Consumer> consumers)
		{
			Chart = chart;
			History = history;

			// Create the solar output chart data
			Solar = new ChartDataSeries("Solar Output", ChartDataSeries.EFormat.XRealYReal, new ChartDataSeries.OptionsData
			{
				PlotType = ChartDataSeries.EPlotType.Line,
				Colour = Colour32.DarkGreen,
				LineWidth = 3.0,
				PointsOnLinePlot = false,
			}) { Chart = chart };

			// Create the combined consumption chart data
			Consumption = new ChartDataSeries("Combined", ChartDataSeries.EFormat.XRealYReal, new ChartDataSeries.OptionsData
			{
				PlotType = ChartDataSeries.EPlotType.StepLine,
				Colour = Colour32.DarkRed,
				LineWidth = 3.0,
				PointsOnLinePlot = false,
			}) { Chart = chart };

			// Create chart data for each existing consumer
			Consumers = consumers.Select(x => CreateSeriesForConsumer(x)).ToList();
		}
		public void Dispose()
		{
			Consumers = null!;
			Consumption = null!;
			Solar = null!;
			History = null!;
			Chart = null!;
		}

		/// <summary>The chart to draw the series' on</summary>
		private ChartControl Chart
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.BuildScene -= HandleBuildScene;
				}
				field = value;
				if (field != null)
				{
					field.BuildScene += HandleBuildScene;
				}

				// Handlers
				void HandleBuildScene(object? sender, View3dControl.BuildSceneEventArgs e)
				{
					// Update the 'now' point for each data series
					{
						using var lk = Solar.Lock();
						RemoveNowPoint(lk);
						AddNowPoint(lk);
					}
					{
						using var lk = Consumption.Lock();
						RemoveNowPoint(lk);
						AddNowPoint(lk);
					}
					foreach (var consumer in Consumers)
					{
						using var lk = consumer.Lock();
						RemoveNowPoint(lk);
						AddNowPoint(lk);
					}
				}
			}
		} = null!;

		/// <summary>Access to the history database</summary>
		private History History
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.DataAdded -= HandleDataAdded;
				}
				field = value;
				if (field != null)
				{
					field.DataAdded += HandleDataAdded;
				}

				// Handlers
				void HandleDataAdded(object? sender, History.DataAddedEventArgs e)
				{
					// Notes:
					//  - All series' have a fake last data point representing 'now'

					// A solar output record was added
					if (e.Solar is History.SolarOutputRecord solar)
					{
						using var lk = Solar.Lock();
						RemoveNowPoint(lk);
						var x = (DateTimeOffset.FromUnixTimeSeconds(solar.Timestamp) - History.Epoch).TotalHours;
						lk.Add(new ChartDataSeries.Pt(x, solar.Output));
						AddNowPoint(lk);
					}

					// A combined consumption record was added
					if (e.Consumption is History.ConsumptionRecord consumption)
					{
						using var lk = Consumption.Lock();
						RemoveNowPoint(lk);
						var x = (DateTimeOffset.FromUnixTimeSeconds(consumption.Timestamp) - History.Epoch).TotalHours;
						lk.Add(new ChartDataSeries.Pt(x, consumption.Power));
						AddNowPoint(lk);
					}

					// A consumer record was added
					if (e.Consumer is History.ConsumerRecord consumer)
					{
						// Find the series corresponding to this record
						var series = Consumers.FirstOrDefault(x => x.UserData[ConsumerKey] is Consumer c && c.DeviceID == consumer.DeviceID)
							?? throw new Exception($"Consumer series {ConsumerKey} (ID {consumer.DeviceID}) not found ");
						
						using var lk = series.Lock();
						RemoveNowPoint(lk);
						var x = (DateTimeOffset.FromUnixTimeSeconds(consumer.Timestamp) - History.Epoch).TotalHours;
						var y = consumer.On != 0 ? (consumer.Power ?? 0.0) : 0.0;
						lk.Add(new ChartDataSeries.Pt(x, y));
						AddNowPoint(lk);
					}

					Chart.Invalidate();
				}
			}
		} = null!;

		/// <summary>Chart data for the solar output</summary>
		public ChartDataSeries Solar
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					Util.Dispose(ref field!);
				}
				field = value;
				if (field != null)
				{
					// Initialise from the history
					using var lk = field.Lock();
					foreach (var record in History.Solar())
					{
						var x = (DateTimeOffset.FromUnixTimeSeconds(record.Timestamp) - History.Epoch).TotalHours;
						lk.Add(new ChartDataSeries.Pt(x, record.Output));
					}
					AddNowPoint(lk);
				}
			}
		} = null!;

		/// <summary>Nett consumed by all active consumers</summary>
		public ChartDataSeries Consumption
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					Util.Dispose(ref field!);
				}
				field = value;
				if (field != null)
				{
					// Initialise from the history
					using var lk = field.Lock();
					foreach (var record in History.Consumption())
					{
						var x = (DateTimeOffset.FromUnixTimeSeconds(record.Timestamp) - History.Epoch).TotalHours;
						lk.Add(new ChartDataSeries.Pt(x, record.Power));
					}
					AddNowPoint(lk);
				}
			}
		} = null!;

		/// <summary>Chart data for each consumer</summary>
		public List<ChartDataSeries> Consumers
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					Consumer.Genesis -= HandleConsumerGenesis;
					Util.DisposeRange(field);
				}
				field = value;
				if (field != null)
				{
					Consumer.Genesis += HandleConsumerGenesis;
				}

				// Handler
				void HandleConsumerGenesis(Consumer consumer, bool created)
				{
					if (created)
					{
						Consumers.Add(CreateSeriesForConsumer(consumer));
					}
					else
					{
						var idx = Consumers.IndexOf(x => x.UserData[ConsumerKey] == consumer);
						if (idx != -1) Consumers.RemoveRange(idx, 1, dispose: true);
					}
				}
			}
		} = null!;

		/// <summary>Create a data series for a consumer</summary>
		private ChartDataSeries CreateSeriesForConsumer(Consumer consumer)
		{
			var series = new ChartDataSeries(consumer.Name, ChartDataSeries.EFormat.XRealYReal, new ChartDataSeries.OptionsData
			{
				PlotType = ChartDataSeries.EPlotType.StepLine,
				Colour = consumer.Colour,
				LineWidth = 3.0,
				PointsOnLinePlot = false,
			});
			series.UserData[ConsumerKey] = consumer;
			series.Chart = Chart;
			series.Visible = false;

			// Initialise from the history
			Initialise(series, consumer.DeviceID);
			void Initialise(ChartDataSeries s, string device_id)
			{
				// Consumers live longer that this 'ChartData' so watch out
				// for consumers disposing after this object has been disposed.
				if (History == null || device_id.Length == 0)
					return;

				using var lk = s.Lock();
				lk.Clear();
				foreach (var record in History.Consumer(device_id: device_id))
				{
					var x = (DateTimeOffset.FromUnixTimeSeconds(record.Timestamp) - History.Epoch).TotalHours;
					var y = record.On != 0 ? record.Power ?? 0.0 : 0.0;
					lk.Add(new ChartDataSeries.Pt(x, y));
				}
				AddNowPoint(lk);
			}

			// Observe for name/colour changes
			consumer.PropertyChanged += WeakRef.MakeWeak(HandleConsumerPropertyChanged, h => consumer.PropertyChanged -= h);
			void HandleConsumerPropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				if (!(sender is Consumer consumer)) return;
				switch (e.PropertyName)
				{
					case nameof(Consumer.Name):
					{
						series.Name = consumer.Name;
						break;
					}
					case nameof(Consumer.Colour):
					{
						series.Options.Colour = consumer.Colour;
						break;
					}
					case nameof(Consumer.EweSwitch):
					{
						if (consumer.EweSwitch != null)
							Initialise(series, consumer.DeviceID);
						break;
					}
				}
			}

			return series;
		}

		/// <summary>Add a data point representing 'now'</summary>
		private void AddNowPoint(ChartDataSeries.LockData lk)
		{
			if (lk.Count == 0)
				return;

			// Add a fake data point for "now"
			var now = (DateTimeOffset.Now - History.Epoch).TotalHours;
			lk.Add(new ChartDataSeries.Pt(now, lk[lk.Count - 1].y));
		}

		/// <summary>Remove the last point assuming it's the 'now' point</summary>
		private void RemoveNowPoint(ChartDataSeries.LockData lk)
		{
			lk.Count -= lk.Count != 0 ? 1 : 0;
		}

		/// <summary>User data key</summary>
		private static readonly Guid ConsumerKey = new("EF22CB4E-3F6D-44BD-A1CD-31F920B386DF");
	}
}
