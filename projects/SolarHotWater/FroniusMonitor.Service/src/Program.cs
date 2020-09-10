using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rylogic.Utility;

namespace FroniusMonitor.Service
{
	public class Program
	{
		/// <summary></summary>
		public static void Main(string[] args)
		{
			CreateHostBuilder(args)
				.Build()
				.Run();
		}

		/// <summary></summary>
		public static IHostBuilder CreateHostBuilder(string[] args) => Host
			.CreateDefaultBuilder(args)
			.UseWindowsService()
			.ConfigureAppConfiguration((context, config) =>
			{
			})
			.ConfigureServices((host_context, services) =>
			{
				services.AddHostedService<Worker>();
				services.Configure<AppSettings>(host_context.Configuration.GetSection(nameof(AppSettings)));
			});
	}
}
