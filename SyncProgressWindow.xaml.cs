using System.Windows;

namespace StS2ModManager;

public partial class SyncProgressWindow : Window
{
    public SyncProgressWindow()
    {
        InitializeComponent();
    }

    public void SetTitle(string text)
    {
        TitleText.Text = text;
    }

    public void UpdateProgress(string status, int current, int total)
    {
        StatusText.Text = status;
        CounterText.Text = $"{current}/{total}";

        if (total <= 0)
        {
            SyncProgressBar.IsIndeterminate = true;
            return;
        }

        SyncProgressBar.IsIndeterminate = false;
        SyncProgressBar.Maximum = total;
        SyncProgressBar.Value = Math.Min(Math.Max(current, 0), total);
    }
}
