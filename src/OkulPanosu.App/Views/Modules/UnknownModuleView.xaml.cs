using System.Windows.Controls;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

public partial class UnknownModuleView : UserControl
{
    public UnknownModuleView(BoardModule module)
    {
        InitializeComponent();
        MessageText.Text = $"Bilinmeyen modül tipi: \"{module.Type}\"";
    }
}
