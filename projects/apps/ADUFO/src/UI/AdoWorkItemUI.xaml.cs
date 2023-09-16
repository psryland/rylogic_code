using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Gui.WPF;

namespace UFADO;

public partial class AdoWorkItemUI : Window, INotifyPropertyChanged
{
    public AdoWorkItemUI(Window owner, Settings settings, AdoInterface ado)
    {
        InitializeComponent();
        PinState = new PinData(this, EPin.Centre);
        Settings = settings;
        Owner = owner;
        Ado = ado;

        WorkItemIds = Settings.TestWorkItemIds;
        RunQuery = Command.Create(this, RunQueryInternal);
        DataContext = this;
    }

    /// <summary>Support pinning this window</summary>
    private PinData PinState { get; }

    /// <summary>Ado interface</summary>
    private AdoInterface Ado { get; }

    /// <summary>App settings</summary>
    private Settings Settings { get; }

    /// <summary>Delimited list of work item its to lookup</summary>
    public string WorkItemIds
    {
        get => m_work_item_ids;
        set
        {
            if (m_work_item_ids == value) return;
            m_work_item_ids = value;
            NotifyPropertyChanged(nameof(WorkItemIds));
        }
    }
    private string m_work_item_ids = string.Empty;

    /// <summary>The result of the query</summary>
    public string QueryResult
    {
        get => m_query_result;
        set
        {
            if (m_query_result == value) return;
            m_query_result = value;
            NotifyPropertyChanged(nameof(QueryResult));
        }
    }
    private string m_query_result = string.Empty;

    /// <summary></summary>
    public Color ResultColor
    {
        get => m_color;
        set
        {
            if (m_color == value) return;
            m_color = value;
            NotifyPropertyChanged(nameof(ResultColor));
        }
    }
    private Color m_color;

    /// <summary></summary>
    public Command RunQuery { get; }
    private async void RunQueryInternal()
    {
        try
        {
            using var cancel = new CancellationTokenSource();

            // Save the query string
            Settings.TestWorkItemIds = WorkItemIds;

            // Use the selection only if there is one
            var query = m_input.SelectionLength != 0
                ? m_input.SelectedText
                : WorkItemIds;

            var ids = query
                .Split(new[] { " ", ",", ";", "\t", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var id) ? id : -1)
                .Where(x => x != -1)
                .ToList();

            // Ignore empty queries
            if (ids.Count == 0)
            {
                QueryResult = "<no result>";
                ResultColor = Colors.Black;
                return;
            }

            // Run the query
            var result = await Ado.GetWorkItems(ids, cancel.Token);
            if (result == null)
            {
                QueryResult = "<no result>";
                ResultColor = Colors.Black;
                return;
            }

            // Convert the result to JSON
            var items = result.ToList();
            using var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, items, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                WriteIndented = true,
                MaxDepth = 20
            });

            // Convert to string
            ms.Position = 0;
            using var sr = new StreamReader(ms);
            QueryResult = sr.ReadToEnd();
            ResultColor = Colors.Green;
        }
        catch (Exception ex)
        {
            QueryResult = ex.Message;
            ResultColor = Colors.Red;
        }
    }

    /// <summary>Handle Shift+Enter to execute the command</summary>
    private void InputQuery_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            RunQuery.Execute();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
}
