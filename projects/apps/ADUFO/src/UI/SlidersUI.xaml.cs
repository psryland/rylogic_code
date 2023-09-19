using System.Windows;
using Rylogic.Gui.WPF;

namespace UFADO;

public partial class SlidersUI : Window
{
    public SlidersUI(Window owner, Sliders sliders)
    {
        InitializeComponent();
        Owner = owner;
        PinState = new PinData(this, EPin.Centre);
        DataContext = sliders;
    }

    /// <summary>Support pinning this window</summary>
    private PinData PinState { get; }
}
