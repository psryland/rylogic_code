using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rylogic.Common;
using Rylogic.Core.Windows;
using Rylogic.Utility;
using SolarHotWater.Common;

namespace FroniusMonitor.Service
{
	public class Worker : BackgroundService
	{
		public Worker(ILogger<Worker> log, IOptions<AppSettings> app_settings)
		{
			Log = log;
			AppSettings = app_settings;
		}

		/// <summary></summary>
		private ILogger<Worker> Log { get; }

		/// <summary></summary>
		private IOptions<AppSettings> AppSettings { get; }

		/// <summary>User profile path</summary>
		private string OutputDirectory { get; set; } = string.Empty;

		/// <summary></summary>
		public override async Task StartAsync(CancellationToken cancellationToken)
		{
			// If the user profile doesn't exist, stop
			if (!(WinOS.UserProfilePath(AppSettings.Value.UserProfile) is string profile_path))
				throw new Exception($"UserProfile '{AppSettings.Value.UserProfile}' does not have a home directory");

			// Output directory not found? stop
			OutputDirectory = Path.Combine(profile_path, "Documents", "Rylogic", "SolarHotWater");
			if (!Path_.PathExists(OutputDirectory))
				throw new Exception($"Output directory path '{OutputDirectory}' does not exist");

			await base.StartAsync(cancellationToken);
		}

		/// <summary></summary>
		protected override async Task ExecuteAsync(CancellationToken shutdown)
		{
			var settings = new SettingsData(Path.Combine(OutputDirectory, "settings.xml")) ?? throw new Exception("Settings unavailable");
			using var fronius = new FroniusAPI(settings.SolarInverterIP, shutdown);
			using var history = new History(OutputDirectory);

			var prev = (SolarData?)null;
			for (;!shutdown.IsCancellationRequested; await Task.Delay(settings.SolarPollPeriod, shutdown))
			{
				try
				{
					// Read the inverter data
					var solar = await fronius.RealTimeData(shutdown);

					// Ignore consecutive zero output records
					if (solar.CurrentPower == 0 && prev?.CurrentPower == 0)
					{
						prev = solar;
						continue;
					}

					// Add the zero record before a non-zero record
					if (prev?.CurrentPower == 0)
					{
						history.Add(prev);
					}

					// Add the solar data to the history
					history.Add(solar);
					prev = solar;
				}
				catch (OperationCanceledException) { }
				catch (Exception ex)
				{
					Log.LogError(ex, "{time} - Error reading solar data", DateTimeOffset.Now);
				}
			}
		}
	}
}
