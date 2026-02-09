using System.ComponentModel;
using System.Windows;
using Rylogic.Gui.WPF;

namespace UFADO;

public partial class ConnectUI : Window, INotifyPropertyChanged
{
    public ConnectUI(Window owner)
    {
        Owner = owner;

        Accept = Command.Create(this, AcceptInternal);

        InitializeComponent();
        DataContext = this;
    }

    /// <summary>The ADO organisation</summary>
    public string Organization
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyPropertyChanged(nameof(Organization));
            NotifyPropertyChanged(nameof(AdoUrl));
        }
    } = string.Empty;

    /// <summary>The ADO project</summary>
    public string Project
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyPropertyChanged(nameof(Project));
            NotifyPropertyChanged(nameof(AdoUrl));
        }
    } = string.Empty;

    /// <summary>The access token for ADO</summary>
    public string PersonalAccessToken
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyPropertyChanged(nameof(PersonalAccessToken));
        }
    } = string.Empty;

    /// <summary>The ADO URL</summary>
    public string AdoUrl => $"https://dev.azure.com/{Organization}/{Project}";

    /// <summary>Accept action</summary>
    public Command Accept { get; }
    private void AcceptInternal()
    {
        DialogResult = true;
        Close();
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
}
