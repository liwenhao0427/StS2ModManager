using System.Windows;

namespace StS2ModManager;

public partial class DonateWindow : Window
{
    public DonateWindow()
    {
        InitializeComponent();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
