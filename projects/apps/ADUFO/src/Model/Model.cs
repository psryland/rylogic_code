using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UFADO.DomainObjects;
using Microsoft.VisualStudio.Services.Common;
using Rylogic.Gui.WPF;
using Rylogic.Utility;
using UFADO.Gfx;
using Rylogic.Gfx;
using Rylogic.Common;

namespace UFADO;

public sealed class Model :IDisposable, INotifyPropertyChanged
{
	private readonly Settings m_settings;
	private readonly Logger m_log;

	public Model(Settings settings, Logger log)
	{
		m_settings = settings;
		m_log = log;

		View3d = View3d.Create();
		Gfx = new GfxModels();
		Items = new WorkItemDB();
		Ado = null!;

		try
		{
			// If the DB is on disk, load it
			if (Path_.FileExists(m_settings.DBFilepath))
				Items.Load(m_settings.DBFilepath);
		}
		catch (Exception e)
		{
			m_log.Write(ELogLevel.Error, e.Message, e.StackTrace);
		}
	}
	public void Dispose()
	{
		Util.Dispose(Items);
		Util.Dispose(Gfx);
		Ado = null!;
		View3d = null!;
		GC.SuppressFinalize(this);
	}

	/// <summary>The view3d DLL context </summary>
	public View3d View3d
	{
		get => m_view3d;
		set
		{
			if (m_view3d == value) return;
			if (m_view3d != null)
			{
				m_view3d.Error -= ReportError;
				Util.Dispose(ref m_view3d!);
			}
			m_view3d = value;
			if (m_view3d != null)
			{
				m_view3d.Error += ReportError;
			}

			// Handlers
			static void ReportError(object? sender, View3d.ErrorEventArgs e)
			{
				//Log.Write(ELogLevel.Error, e.Message, e.Filepath, e.FileLine);
			}
		}
	}
	private View3d m_view3d = null!;

	/// <summary>Shared geometry</summary>
	private GfxModels Gfx { get; }

	/// <summary>Database of </summary>
	public WorkItemDB Items { get; }

	/// <summary>Access to ADO</summary>
	public AdoInterface? Ado
	{
		get => m_ado;
		set
		{
			// We don't own the Ado object
			if (m_ado == value) return;
			m_ado = value;
			NotifyPropertyChanged(nameof(Ado));
		}
	}
	private AdoInterface? m_ado;

	/// <summary>Reload the data from ADO</summary>
	public async Task Refresh(ChartControl chart)
	{
		if (Ado is null)
			return;

		//TODO: remove items that aren't on ADO any more

		using var cancel = new CancellationTokenSource();
		/*
		// Download work streams
		var task0 = Ado.WorkStreams(cancel.Token)
			.ContinueWith(workstreams =>
			{
				if (workstreams.IsFaulted) return;
				foreach (var workstream in workstreams.Result)
				{
					if (workstream.Id == null) continue;
					var ws = Items.WorkStreams.GetOrAddValue(workstream.Id.Value, () => new WorkStream(workstream));
					ws.Item = workstream;
					ws.Chart = chart;
				}
			});

		// Download epics
		var task1 = Ado.Epics(cancel.Token)
			.ContinueWith(epics =>
			{
				if (epics.IsFaulted) return;
				foreach (var epic in epics.Result)
				{
					if (epic.Id == null) continue;
					var ws = Items.Epics.GetOrAddValue(epic.Id.Value, () => new Epic(epic));
					ws.Item = epic;
					ws.Chart = chart;
				}
			});

		await Task.WhenAll(task0, task1);
*/
		var workstreams = Ado.WorkStreams(cancel.Token);
		var epics = Ado.Epics(cancel.Token);

		// Download work streams
		foreach (var workstream in await workstreams)
		{
			if (workstream.Id is not int id) continue;
			var ws = Items.WorkStreams.GetOrAddValue(new(id), () => new WorkStream(new(id), "workstream"));
			ws.Item = workstream;
			ws.Chart = chart;
		}

		// Download epics
		foreach (var epic in await epics)
		{
			if (epic.Id is not int id) continue;
			var ws = Items.Epics.GetOrAddValue(new(id), () => new Epic(new(id), "epic", epic.ParentItemId()));
			ws.Item = epic;
			ws.Chart = chart;
		}

		// Persist the data to storage
		Items.Save(m_settings.DBFilepath);
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
}
