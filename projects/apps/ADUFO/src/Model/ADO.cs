using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ADUFO.DomainObjects;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Rylogic.Utility;

namespace ADUFO;

public class AdoInterface: IDisposable, INotifyPropertyChanged
{
	public AdoInterface(string organization, string project, string pat, CancellationToken shutdown)
	{
		Organization = organization;
		Project = project;
		PersonalAccessToken = pat;
		Shutdown = shutdown;
	}
	public void Dispose()
	{
		Client = null!;
		Connection = null!;
	}

	/// <summary>App shutdown token</summary>
	private CancellationToken Shutdown { get; }

	/// <summary>The ADO URL</summary>
	public Uri AdoUrl => new Uri($"https://dev.azure.com/{Organization}/{Project}");

	/// <summary>The ADO instance to connect to</summary>
	private string Organization
	{
		get => m_organization;
		set
		{
			if (m_organization == value) return;
			m_organization = value;
			NotifyPropertyChanged(nameof(Organization));
			
			// Force a reconnection
			Client = null!;
			Connection = null!;
		}
	}
	private string m_organization = null!;

	/// <summary>The project to connect to</summary>
	private string Project
	{
		get => m_project;
		set
		{
			if (m_project == value) return;
			m_project = value;
			NotifyPropertyChanged(nameof(Project));

			// Force a reconnection
			Client = null!;
			Connection = null!;
		}
	}
	private string m_project = null!;

	/// <summary>Token to access ADO</summary>
	private string PersonalAccessToken
	{
		get => m_pat;
		set
		{
			if (m_pat == value) return;
			m_pat = value;
			NotifyPropertyChanged(nameof(PersonalAccessToken));

			// Force a reconnection
			Client = null!;
			Connection = null!;
		}
	}
	private string m_pat = null!;

	/// <summary>Visual Studio Services Connection</summary>
	private VssConnection Connection
	{
		get => m_connection ?? (Connection = new VssConnection(AdoUrl, new VssBasicCredential(string.Empty, PersonalAccessToken)));
		set
		{
			if (m_connection == value) return;
			Util.Dispose(ref m_connection!);
			m_connection = value;
		}
	}
	private VssConnection m_connection = null!;

	/// <summary>Client for accessing work items</summary>
	private WorkItemTrackingHttpClient Client
	{
		get => m_client ?? (Client = Connection.GetClient<WorkItemTrackingHttpClient>(Shutdown));
		set
		{
			if (m_client == value) return;
			Util.Dispose(ref m_client!);
			m_client = value;
		}
	}
	private WorkItemTrackingHttpClient m_client = null!;

	/// <summary>Return all work items</summary>
	public async Task<IEnumerable<WorkItem>> WorkStreams()
	{
		var result = await Client.QueryByWiqlAsync(
			"select * from [WorkItems] where [Work Item Type] = 'Work Stream'"
			.AsQuery());

		var tasks = new List<Task<List<WorkItem>>>();
		foreach (var ids in result.WorkItems.Select(x => x.Id).Batch(100))
			tasks.Add(Client.GetWorkItemsAsync(ids));

		await Task.WhenAll(tasks);
		return tasks.SelectMany(x => x.Result);
	}

	public async Task Test()
	{
		var query = new Wiql()
		{
			//Query = "Select [State], [Title], [Assigned To] From WorkItems Where [Work Item Type] = 'Task' order by [State] asc, [Changed Date] desc"
			Query = "Select [Title] From WorkItems Where [Work Item Type] = 'Epic'"
		};

		var result = await Client.QueryByWiqlAsync(query);

		if (result.WorkItems.Any())
		{
			var ids = result.WorkItems.Select(item => item.Id).Take(100).ToArray();
			var workItems = await Client.GetWorkItemsAsync(ids);

			foreach (var workItem in workItems)
			{
				Console.WriteLine("{0} - {1}", workItem.Id, workItem.Fields["System.Title"]);
			}
		}
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

}


#if false // ADO doesn't have a web socket API
	/// <summary>SignalR connection to ADO</summary>
	public bool IsConnected
	{
		get => m_hub_connection != null;
		private set
		{
			if (IsConnected == value) return;
			if (m_hub_connection != null)
			{
				m_hub_shutdown.Cancel();
				if (m_hub_connection.IsAlive)
					m_hub_connection.Join();
			}
			m_hub_connection = value ? new Thread(new ParameterizedThreadStart(HubConnectionThread)) : null;
			if (m_hub_connection != null)
			{
				m_hub_shutdown = new CancellationTokenSource();
				m_hub_connection.Start(m_hub_shutdown.Token);
			}

			// Handlers
			async void HubConnectionThread(object? _)
			{
				var shutdown = (_ as CancellationToken?) ?? throw new Exception("Not a cancellation token");
				var hub =  new HubConnectionBuilder().WithUrl(AdoUrl).Build();
				hub.Closed += async (err) =>
				{
					// Tell the main thread the connection is lost
					Sync.Post(_ => IsConnected = false, null);
					await Task.CompletedTask;
				};

				await hub.StartAsync(shutdown);
				await SubscribeToWorkItemUpdates();
				shutdown.WaitHandle.WaitOne();
				await hub.StopAsync();
			}
			async Task SubscribeToWorkItemUpdates()
			{

			}
		}
	}
	private CancellationTokenSource m_hub_shutdown;
	private Thread? m_hub_connection;
#endif
