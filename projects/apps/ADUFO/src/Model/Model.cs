using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ADUFO.DomainObjects;
using Microsoft.VisualStudio.Services.Common;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace ADUFO;

public class Model :IDisposable, INotifyPropertyChanged
{
	public Model(Settings settings)
	{
		Settings = settings;
		Items = new WorkItemDB(settings);
		Ado = null!;
	}
	public void Dispose()
	{
		Util.Dispose(Items);
		Ado = null!;
	}

	/// <summary>Application settings</summary>
	private Settings Settings { get; }

	/// <summary>Database of </summary>
	public WorkItemDB Items { get; }

	/// <summary>Access to ADO</summary>
	public AdoInterface? Ado
	{
		get => m_ado;
		set
		{
			if (m_ado == value) return;
			m_ado = value;
			NotifyPropertyChanged(nameof(Ado));
		}
	}
	private AdoInterface? m_ado;

	/// <summary>Reload the data from ADO</summary>
	public async Task Refresh(ChartControl chart)
	{
		if (Ado is not AdoInterface)
			return;

		var ws_items = await Ado.WorkStreams();
		foreach (var item in ws_items)
		{
			if (item.Id == null) continue;
			var ws = Items.WorkStreams.GetOrAddValue(item.Id.Value, () => new WorkStream(item));
			ws.Item = item;
			ws.Chart = chart;
		}

		//// Persist the layout to storage
		//Items.Save();
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

}
