using System.Windows;
using System.Windows.Input;
using StS2ModManager.ViewModels;

namespace StS2ModManager;

public partial class MainWindow : Window
{
    private MainViewModel? Vm => DataContext as MainViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void OnMetaEditorLostFocus(object sender, RoutedEventArgs e)
    {
        Vm?.SaveModMetaOnBlurCommand.Execute(null);
    }

    private void OnTagInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        Vm?.AddNewTagCommand.Execute(null);
        e.Handled = true;
    }
}
