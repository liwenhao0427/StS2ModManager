using System.Collections.ObjectModel;
using System.Windows;
using StS2ModManager.Models;

namespace StS2ModManager;

public partial class SyncSourceSelectWindow : Window
{
    public ObservableCollection<SyncSourceOption> SourceOptions { get; }
    private string _requiredMsg = "请至少勾选一个目录。";
    private string _tipTitle = "提示";

    public SyncSourceSelectWindow(IEnumerable<SyncSourceOption> options)
    {
        InitializeComponent();
        SourceOptions = new ObservableCollection<SyncSourceOption>(options);
        DataContext = this;
    }

    public void SetTexts(
        string title,
        string header,
        string hint,
        string selectAll,
        string unselectAll,
        string confirm,
        string cancel,
        string requiredMsg,
        string tipTitle)
    {
        Title = title;
        HeaderText.Text = header;
        HintText.Text = hint;
        SelectAllButton.Content = selectAll;
        UnselectAllButton.Content = unselectAll;
        ConfirmButton.Content = confirm;
        CancelButton.Content = cancel;
        _requiredMsg = requiredMsg;
        _tipTitle = tipTitle;
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        foreach (var option in SourceOptions)
        {
            option.IsSelected = true;
        }
    }

    private void OnUnselectAllClick(object sender, RoutedEventArgs e)
    {
        foreach (var option in SourceOptions)
        {
            option.IsSelected = false;
        }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (!SourceOptions.Any(x => x.IsSelected))
        {
            MessageBox.Show(_requiredMsg, _tipTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
