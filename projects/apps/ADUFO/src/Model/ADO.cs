using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Linq;
using Rylogic.Utility;

namespace ADUFO;

public class AdoInterface
{
    public AdoInterface(string ado_url, string pat)
    {
        AdoURL = ado_url;
        PersonalAccessToken = pat;
        Connection = new VssConnection(new Uri(AdoURL), new VssBasicCredential(string.Empty, PersonalAccessToken));
        Client = Connection.GetClient<WorkItemTrackingHttpClient>();

        var query = new Wiql()
        {
            Query = "Select [State], [Title], [Assigned To] From WorkItems Where [Work Item Type] = 'Task' order by [State] asc, [Changed Date] desc"
        };

        var result = Client.QueryByWiqlAsync(query).Result;

        if (result.WorkItems.Any())
        {
            var ids = result.WorkItems.Select(item => item.Id).ToArray();

            var workItems = Client.GetWorkItemsAsync(ids).Result;

            foreach (var workItem in workItems)
            {
                Console.WriteLine("{0} - {1}", workItem.Id, workItem.Fields["System.Title"]);
            }
        }

    }
    public void Dispose()
    {
        Client = null!;
        Connection = null!;
    }

    /// <summary>The ADO instance to connect to</summary>
    private string AdoURL { get; }

    /// <summary>Token to access ADO</summary>
    private string PersonalAccessToken { get; }

    /// <summary>Visual Studio Services Connection</summary>
    private VssConnection Connection
    {
        get => m_connection;
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
        get => m_client;
        set
        {
            if (m_client == value) return;
            Util.Dispose(ref m_client!);
            m_client = value;
        }
    }
    private WorkItemTrackingHttpClient m_client = null!;
}
