using System.Windows;
using StS2ModManager.ViewModels;

namespace StS2ModManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
