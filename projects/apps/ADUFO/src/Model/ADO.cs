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

public class AdoInterface : IDisposable, INotifyPropertyChanged
{
    public AdoInterface(Settings settings, CancellationToken shutdown)
    {
        Settings = settings;
        Shutdown = shutdown;
    }
    public void Dispose()
    {
        Client = null!;
        Connection = null!;
    }

    /// <summary>App shutdown token</summary>
    private CancellationToken Shutdown { get; }

    /// <summary>Application settings</summary>
    private Settings Settings { get; }

    /// <summary>The ADO URL</summary>
    public Uri AdoUrl => new Uri($"https://dev.azure.com/{Organization}");///{Project}");

                                                                          /// <summary>The ADO instance to connect to</summary>
    private string Organization
    {
        get => Settings.Organization;
        set
        {
            if (Organization == value) return;
            Settings.Organization = value;
            NotifyPropertyChanged(nameof(Organization));

            // Force a reconnection
            Client = null!;
            Connection = null!;
        }
    }

    /// <summary>The project to connect to</summary>
    private string Project
    {
        get => Settings.Project;
        set
        {
            if (Project == value) return;
            Settings.Project = value;
            NotifyPropertyChanged(nameof(Project));

            // Force a reconnection
            Client = null!;
            Connection = null!;
        }
    }

    /// <summary>Token to access ADO</summary>
    private string PersonalAccessToken
    {
        get => Settings.PersonalAccessToken;
        set
        {
            if (PersonalAccessToken == value) return;
            Settings.PersonalAccessToken = value;
            NotifyPropertyChanged(nameof(PersonalAccessToken));

            // Force a reconnection
            Client = null!;
            Connection = null!;
        }
    }

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

    /// <summary>Load Work Items details from Ids</summary>
    public async Task<IEnumerable<WorkItem>> GetWorkItems(IEnumerable<int> ids, CancellationToken cancel)
    {
        var tasks = new List<Task<List<WorkItem>>>();
        foreach (var id_batch in ids.Batch(100))
            tasks.Add(Client.GetWorkItemsAsync(ids, expand: WorkItemExpand.All, cancellationToken: cancel));

        await Task.WhenAll(tasks);
        return tasks.SelectMany(x => x.Result);
    }

    /// <summary>Return all work streams</summary>
    public async Task<IEnumerable<WorkItem>> WorkStreams(CancellationToken cancel)
    {
        var result = await Client.QueryByWiqlAsync(
            Settings.QueryWorkStreams.AsQuery(),
            project: Project,
            cancellationToken: cancel);

        return await GetWorkItems(result.WorkItems.Select(x => x.Id), cancel);
    }

    /// <summary>Return all epics</summary>
    public async Task<IEnumerable<WorkItem>> Epics(CancellationToken cancel)
    {
        var result = await Client.QueryByWiqlAsync(
            "select [System.Id] from [WorkItems] where [Work Item Type] = 'Epic'".AsQuery(),
            project: Project,
            cancellationToken: cancel);

        return await GetWorkItems(result.WorkItems.Select(x => x.Id), cancel);
    }

    /// <summary>Run an arbitrary query</summary>
    public async Task<WorkItemQueryResult> RunQuery(string sql, CancellationToken cancel)
    {
        var query = new Wiql { Query = sql };
        var result = await Client.QueryByWiqlAsync(
            query,
            project: Project,
            cancellationToken: cancel);
        return result;
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
