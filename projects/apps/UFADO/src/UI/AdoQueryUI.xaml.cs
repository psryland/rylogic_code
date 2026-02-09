using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Gui.WPF;

namespace UFADO;

public partial class AdoQueryUI : Window, INotifyPropertyChanged
{
    public AdoQueryUI(Window owner, Settings settings, AdoInterface ado)
    {
        InitializeComponent();
        PinState = new PinData(this, EPin.Centre);
        Settings = settings;
        Owner = owner;
        Ado = ado;

        QueryString = Settings.TestQuery;
        RunQuery = Command.Create(this, RunQueryInternal);
        DataContext = this;
    }

    /// <summary>Support pinning this window</summary>
    private PinData PinState { get; }

    /// <summary>Ado interface</summary>
    private AdoInterface Ado { get; }

    /// <summary>App settings</summary>
    private Settings Settings { get; }

    /// <summary>Test Query String</summary>
    public string QueryString
    {
        get;
        set
        {
            if (QueryString == value) return;
            field = value;
            NotifyPropertyChanged(nameof(QueryString));
        }
    } = string.Empty;

    /// <summary>The result of the query</summary>
    public string QueryResult
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyPropertyChanged(nameof(QueryResult));
        }
    } = string.Empty;

    /// <summary></summary>
    public Color ResultColor
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyPropertyChanged(nameof(ResultColor));
        }
    }

    /// <summary></summary>
    public Command RunQuery { get; }
    private async void RunQueryInternal()
    {
        try
        {
            using var cancel = new CancellationTokenSource();

            // Save the query string
            Settings.TestQuery = QueryString;

            // Use the selection only if there is one
            var query = m_input.SelectionLength != 0
                ? m_input.SelectedText
                : QueryString;

            // Ignore empty queries
            if (query.Length == 0)
            {
                QueryResult = "<no result>";
                ResultColor = Colors.Black;
                return;
            }

            // Run the query
            var result = await Ado.RunQuery(QueryString, cancel.Token);
            if (result == null)
            {
                QueryResult = "<no result>";
                ResultColor = Colors.Black;
                return;
            }

            // Convert the result to JSON
            using var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, result, new JsonSerializerOptions
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

#if false
"Select [Title] From WorkItems Where [Work Item Type] = 'Epic'"
"Select [State], [Title], [Assigned To] From WorkItems Where [Work Item Type] = 'Task' order by [State] asc, [Changed Date] desc"


SELECT
	[System.Id] FROM WorkItemLinks
WHERE
(
	[Source].[System.WorkItemType] IN ('Work Stream', 'Epic', 'Feature', 'User Story') AND 
	[Source].[System.State] IN ('New', 'Active', 'Resolved') AND
	[System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward' AND
	[Target].[System.WorkItemType] IN ('User Story', 'Task') AND
	[Target].[System.State] IN ('New', 'Active', 'Resolved')
)
ORDER BY
	[Microsoft.VSTS.Common.StackRank], [System.Id]
mode(Recursive)

#endif
