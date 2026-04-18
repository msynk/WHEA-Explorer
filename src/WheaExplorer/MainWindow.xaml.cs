using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WheaExplorer.Models;
using WheaExplorer.Services;

namespace WheaExplorer;

public partial class MainWindow
{
    private readonly WheaEventLogService _eventLogService = new();
    private readonly ObservableCollection<WheaLogEntry> _entries = new();

    public MainWindow()
    {
        InitializeComponent();
        LogListView.ItemsSource = _entries;
        // Do not call ApplyDecodeViewVisibility here: ViewJsonRadio's Checked can fire during
        // InitializeComponent before every x:Name field (e.g. ViewTreeRadio) is wired up.
        Loaded += (_, _) =>
        {
            ApplyDecodeViewVisibility();
            _ = LoadLogsAsync();
        };
    }

    private async Task LoadLogsAsync()
    {
        RefreshButton.IsEnabled = false;
        StatusText.Text = "Loading WHEA logs…";
        try
        {
            var list = await Task.Run(() => _eventLogService.LoadEntries());
            _entries.Clear();
            foreach (var e in list)
                _entries.Add(e);

            CountLabel.Text = $"{_entries.Count} event(s)";
            StatusText.Text = _entries.Count == 0
                ? "No events with RawData found (or logs unavailable)."
                : "Loaded. Select a row to decode.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e) => _ = LoadLogsAsync();

    private void ViewMode_OnChanged(object sender, RoutedEventArgs e) => ApplyDecodeViewVisibility();

    private void ApplyDecodeViewVisibility()
    {
        if (ViewTreeRadio is null || ViewJsonRadio is null ||
            SelectionJsonText is null || SelectionTreeView is null ||
            ManualOutputText is null || ManualOutputTree is null)
            return;

        var tree = ViewTreeRadio.IsChecked == true;
        SelectionJsonText.Visibility = tree ? Visibility.Collapsed : Visibility.Visible;
        SelectionTreeView.Visibility = tree ? Visibility.Visible : Visibility.Collapsed;
        ManualOutputText.Visibility = tree ? Visibility.Collapsed : Visibility.Visible;
        ManualOutputTree.Visibility = tree ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string FormatJsonForDisplay(string raw)
    {
        try
        {
            var t = JToken.Parse(raw);
            return t.ToString(Formatting.Indented);
        }
        catch
        {
            return raw;
        }
    }

    private void ShowSelectionDecode(string rawText)
    {
        var formatted = FormatJsonForDisplay(rawText);
        SelectionJsonText.Text = formatted;
        JsonTreeVisualizer.Populate(SelectionTreeView, formatted);
    }

    private void ShowManualDecode(string rawText)
    {
        var formatted = FormatJsonForDisplay(rawText);
        ManualOutputText.Text = formatted;
        JsonTreeVisualizer.Populate(ManualOutputTree, formatted);
    }

    private void ShowSelectionRawCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (SelectionRawPanel is null)
            return;
        SelectionRawPanel.Visibility = ShowSelectionRawCheckBox.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void LogListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LogListView.SelectedItem is not WheaLogEntry entry)
        {
            SelectionJsonText.Clear();
            JsonTreeVisualizer.Populate(SelectionTreeView, "");
            SelectionRawDataText.Clear();
            return;
        }

        SelectionRawDataText.Text = entry.RawHex;

        try
        {
            ShowSelectionDecode(WheaRecordDecodeService.Decode(entry.RawHex));
            StatusText.Text = $"Decoded event {entry.EventId} ({entry.ChannelShort}).";
        }
        catch (Exception ex)
        {
            ShowSelectionDecode(ex.ToString());
            StatusText.Text = "Decode failed.";
        }
    }

    private void DecodeManualButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowManualDecode(WheaRecordDecodeService.Decode(ManualInputText.Text));
            StatusText.Text = "Decode succeeded.";
        }
        catch (Exception ex)
        {
            ShowManualDecode(ex.ToString());
            StatusText.Text = "Decode failed.";
        }
    }
}
