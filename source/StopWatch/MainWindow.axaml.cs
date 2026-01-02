using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using StopWatch.Logging;

namespace StopWatch;

public class IssueViewModel
{
    public string Key { get; set; }
    public string Time { get; set; }
    public string Comment { get; set; }
    public bool IsRunning { get; set; }
    internal WatchTimer Timer { get; set; } = new WatchTimer();
}

public class FilterItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Jql { get; set; }
}

public partial class MainWindow : Window
{
    private System.Timers.Timer updateTimer;
    private System.Timers.Timer ticker;
    private ObservableCollection<IssueViewModel> issues;
    private List<FilterItem> filters;
    private JiraApiRequestFactory jiraApiRequestFactory;
    private RestRequestFactory restRequestFactory;
    private JiraApiRequester jiraApiRequester;
    private RestClientFactory restClientFactory;
    private JiraClient jiraClient;
    private TrayIcon _trayIcon;
    private bool _trayWarningShown;

    public MainWindow()
    {
        InitializeComponent();

        // Restore window size and position
        if (Settings.Instance.WindowWidth > 0)
            Width = Settings.Instance.WindowWidth;
        if (Settings.Instance.WindowHeight > 0)
            Height = Settings.Instance.WindowHeight;
        if (!double.IsNaN(Settings.Instance.WindowPositionX) && !double.IsNaN(Settings.Instance.WindowPositionY))
        {
            Position = new PixelPoint((int)Settings.Instance.WindowPositionX, (int)Settings.Instance.WindowPositionY);
        }
        // Some platforms ignore Position until window is opened
        Opened += MainWindow_Opened;

        issues = new ObservableCollection<IssueViewModel>();
        filters = new List<FilterItem>();

        // Setup UI
        issuesPanel.Children.Clear(); // Clear any existing children
        ApplyLocalization();

        // Setup timer for UI updates
        updateTimer = new Timer(1000);
        updateTimer.Elapsed += UpdateTimer_Elapsed;
        updateTimer.Start();

        // Setup ticker for Jira updates
        ticker = new Timer(30000); // 30 seconds
        ticker.Elapsed += Ticker_Elapsed;
        ticker.Start();

        // Initialize logger
        InitializeLogger();

        // Initialize Jira components
        InitializeJiraComponents();

        // Apply window behavior settings
        Topmost = Settings.Instance.AlwaysOnTop;

        // Set window icon (cross-platform)
        try
        {
            var iconUri = new Uri("avares://StopWatch/icons/stopwatchimg.png");
            using var s = AssetLoader.Open(iconUri);
            this.Icon = new WindowIcon(s);
        }
        catch { }

        // Initialize tray icon if enabled
        InitializeTrayIcon();
        // Hide to tray on minimize if enabled
        this.PropertyChanged += (s, e) =>
        {
            if (Settings.Instance.MinimizeToTray && e.Property == Window.WindowStateProperty && WindowState == WindowState.Minimized)
            {
                if (EnsureTrayOrWarn())
                {
                    Hide();
                }
            }
        };

        // Note: PropertyChanged hook above handles minimize-to-tray across platforms

        // Load persisted issues
        LoadPersistedIssues();

        // Setup filters
        SetupFilters();

        // Event handlers
        btnStart.Click += BtnStart_Click;
        btnPause.Click += BtnPause_Click;
        btnStop.Click += BtnStop_Click;
        btnAddIssue.Click += BtnAddIssue_Click;
        btnSettings.Click += BtnSettings_Click;
        btnAbout.Click += BtnAbout_Click;
        btnHelp.Click += BtnHelp_Click;
        btnSubmitWorklog.Click += BtnSubmitWorklog_Click;
        tbIssueKey.TextChanged += TbIssueKey_TextChanged;
        cbFilters.SelectionChanged += CbFilters_SelectionChanged;

        // Menu handlers
        menuSettings.Click += async (s, e) => await OpenSettingsAsync();
        menuAbout.Click += MenuAbout_Click;
        menuExit.Click += MenuExit_Click;

        // Save on close
        Closed += MainWindow_Closed;

        // Initial status update
        UpdateConnectionStatus();
    }

    private void ApplyLocalization()
    {
        try
        {
            Title = Localization.Localizer.T("App_Title");
            menuFileRoot.Header = Localization.Localizer.T("Menu_File");
            menuToolsRoot.Header = Localization.Localizer.T("Menu_Tools");
            txtActiveFilterLabel.Text = Localization.Localizer.T("Lbl_ActiveFilter");
            txtIssueLabel.Text = Localization.Localizer.T("Lbl_Issue");
            lblTotalTime.Text = Localization.Localizer.T("Lbl_Total");
            btnStart.Content = Localization.Localizer.T("Btn_Start");
            btnPause.Content = Localization.Localizer.T("Btn_Pause");
            btnStop.Content = Localization.Localizer.T("Btn_Stop");
            btnAddIssue.Content = Localization.Localizer.T("Btn_AddIssue");
            btnSubmitWorklog.Content = Localization.Localizer.T("Btn_SubmitWorklog");
            btnSettings.Content = Localization.Localizer.T("Btn_Settings");
            btnAbout.Content = Localization.Localizer.T("Btn_About");
            btnHelp.Content = Localization.Localizer.T("Btn_Help");
            menuSettings.Header = Localization.Localizer.T("Menu_Settings");
            menuAbout.Header = Localization.Localizer.T("Menu_About");
            menuExit.Header = Localization.Localizer.T("Menu_Exit");
            lblConnectionStatus.Text = Localization.Localizer.T("Status_NotConnected");
            lblActiveFilter.Text = Localization.Localizer.T("NoFilterSelected");
        }
        catch { }
    }

    private void MainWindow_Opened(object sender, EventArgs e)
    {
        if (!double.IsNaN(Settings.Instance.WindowPositionX) && !double.IsNaN(Settings.Instance.WindowPositionY))
        {
            Position = new PixelPoint((int)Settings.Instance.WindowPositionX, (int)Settings.Instance.WindowPositionY);
        }
    }

    private async Task OpenSettingsAsync()
    {
        var settingsWindow = new SettingsWindow(Settings.Instance)
        {
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        await settingsWindow.ShowDialog(this);

        // Re-apply settings that affect runtime
        Topmost = Settings.Instance.AlwaysOnTop;
        InitializeTrayIcon();
        if (IsJiraEnabled)
        {
            restClientFactory.BaseUrl = Settings.Instance.JiraBaseUrl;
            jiraClient.Authenticate(Settings.Instance.Username, Settings.Instance.ApiToken);
            jiraClient.ValidateSession();
        }
    }

    private void InitializeTrayIcon()
    {
        try
        {
            if (_trayIcon != null)
            {
                _trayIcon.IsVisible = true;
                return;
            }

            var iconUri = new Uri("avares://StopWatch/icons/stopwatchimg.png");
            using var s = AssetLoader.Open(iconUri);
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(s),
                ToolTipText = Localization.Localizer.T("App_Title")
            };
            var menu = new NativeMenu();
            var showItem = new NativeMenuItem("Show");
            showItem.Click += (_, __) => { Show(); WindowState = WindowState.Normal; Activate(); };
            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, __) => Close();
            menu.Items.Add(showItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exitItem);
            _trayIcon.Menu = menu;
            _trayIcon.Clicked += (_, __) => { Show(); WindowState = WindowState.Normal; Activate(); };
            _trayIcon.IsVisible = true;
        }
        catch { }
    }

    private bool EnsureTrayOrWarn()
    {
        InitializeTrayIcon();
        if (_trayIcon == null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !_trayWarningShown)
            {
                _trayWarningShown = true;
                _ = ShowMessage("Tray", "System tray not available. On Linux install appindicator library (e.g. libayatana-appindicator3). Window will not hide to tray.");
            }
            return false;
        }
        _trayIcon.IsVisible = true;
        return true;
    }

    private void InitializeJiraComponents()
    {
        restClientFactory = new RestClientFactory();
        restRequestFactory = new RestRequestFactory();
        jiraApiRequestFactory = new JiraApiRequestFactory(restRequestFactory);
        jiraApiRequester = new JiraApiRequester(restClientFactory, jiraApiRequestFactory);
        jiraClient = new JiraClient(jiraApiRequestFactory, jiraApiRequester);

        // Authenticate if credentials are available
        if (IsJiraEnabled)
        {
            restClientFactory.BaseUrl = Settings.Instance.JiraBaseUrl;
            jiraClient.Authenticate(Settings.Instance.Username, Settings.Instance.ApiToken);
            jiraClient.ValidateSession();
        }

        // Localize tray menu entries
        try
        {
            if (_trayIcon != null && _trayIcon.Menu is NativeMenu menu && menu.Items.Count >= 3)
            {
                if (menu.Items[0] is NativeMenuItem showItem) showItem.Header = Localization.Localizer.T("Tray_Show");
                if (menu.Items[2] is NativeMenuItem exitItem) exitItem.Header = Localization.Localizer.T("Tray_Exit");
            }
        }
        catch { }
    }

    private void InitializeLogger()
    {
        if (Settings.Instance.LoggingEnabled)
        {
            Logger.Instance.Enabled = true;
            Logger.Instance.LogfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jirastopwatch", "jirastopwatch.log");
        }
    }

    private void SetupFilters()
    {
        cbFilters.Items.Clear();
        filters.Clear();
        try
        {
            if (IsJiraEnabled && jiraClient.ValidateSession())
            {
                var favs = jiraClient.GetFavoriteFilters();
                if (favs != null && favs.Count > 0)
                {
                    foreach (var f in favs)
                    {
                        filters.Add(new FilterItem { Id = f.Id, Name = f.Name, Jql = f.Jql });
                        cbFilters.Items.Add(f.Name);
                    }
                    cbFilters.SelectedIndex = Math.Min(Math.Max(0, Settings.Instance.CurrentFilter), filters.Count - 1);
                    lblActiveFilter.Text = filters[cbFilters.SelectedIndex].Name;
                    return;
                }
            }
        }
        catch { }

        // Fallback defaults
        filters.Add(new FilterItem { Id = 0, Name = Localization.Localizer.T("Filter_All"), Jql = "" });
        filters.Add(new FilterItem { Id = 1, Name = Localization.Localizer.T("Filter_Mine"), Jql = "assignee = currentUser()" });
        filters.Add(new FilterItem { Id = 2, Name = Localization.Localizer.T("Filter_Recent"), Jql = "updated > -7d" });

        foreach (var filter in filters)
            cbFilters.Items.Add(filter.Name);
        cbFilters.SelectedIndex = 0;
        lblActiveFilter.Text = filters[0].Name;
    }

    private void LoadPersistedIssues()
    {
        var persisted = Settings.Instance.ReadIssues(Settings.Instance.PersistedIssues);
        issues.Clear();
        issuesPanel.Children.Clear();

        foreach (var issue in persisted)
        {
            var issueViewModel = new IssueViewModel
            {
                Key = issue.Key,
                Time = issue.TotalTime.ToString(@"hh\:mm\:ss"),
                Comment = issue.Comment ?? "",
                IsRunning = issue.TimerRunning
            };
            issueViewModel.Timer.TimeElapsed = issue.TotalTime;
            issues.Add(issueViewModel);
            AddIssueControl(issueViewModel);

            // If this issue was running, resume the timer
            if (issue.TimerRunning)
            {
                issueViewModel.Timer.Start();
            }
        }
    }

    private void AddIssueControl(IssueViewModel issue)
    {
        var issueControl = new IssueControl();
        issueControl.SetIssue(issue);
        issueControl.IssueKeyChanged += async (s, key) => await UpdateIssueSummaryFromKey(issue, (IssueControl)s);
        issueControl.CommentChanged += (s, e) => UpdateIssueComment(issue.Key, issue.Comment);
        issueControl.StartStopClicked += (s, e) => ToggleIssueTimer(issue.Key, ((IssueControl)s).btnStartStop);
        issueControl.btnRemove.Click += (s, e) => RemoveIssue(issue.Key);
        issueControl.LogWorkClicked += async (s, e) => await LogWorkForIssue(issue.Key);
        issueControl.TransitionClicked += async (s, e) => await TransitionIssueToDone(issue.Key);
        issueControl.IssueSummaryClicked += (s, e) => OpenIssueInBrowser(((IssueControl)s).Issue.Key);

        // Wrap in border for removal
        var border = new Border
        {
            BorderBrush = Avalonia.Media.Brushes.Gray,
            BorderThickness = new Avalonia.Thickness(0, 0, 0, 1),
            Margin = new Avalonia.Thickness(0, 0, 0, 2),
            Child = issueControl
        };
        issuesPanel.Children.Add(border);

        // Initially load summary if we have a key
        _ = UpdateIssueSummaryFromKey(issue, issueControl);
    }



    private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Update all running issues
            foreach (var issue in issues.Where(i => i.IsRunning))
            {
                var displayTime = issue.Timer.TimeElapsed;
                UpdateIssueDisplayTime(issue.Key, displayTime);
            }

            // Update main timer display if any issue is running
            var runningIssue = issues.FirstOrDefault(i => i.IsRunning);
            if (runningIssue != null)
            {
                tbTimer.Text = runningIssue.Timer.TimeElapsed.ToString(@"hh\:mm\:ss");
                tbStatus.Text = $"Running on {runningIssue.Key}";
                tbCurrentIssue.Text = runningIssue.Key;
            }
            else
            {
                tbTimer.Text = "00:00:00";
                tbStatus.Text = "Stopped";
                tbCurrentIssue.Text = "";
            }

            tbTotalTime.Text = CalculateTotalTime();
        });
    }

    private void Ticker_Elapsed(object sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await UpdateIssuesFromJira();
        });
    }

    private async Task UpdateIssuesFromJira()
    {
        if (!IsJiraEnabled) return;

        try
        {
            // Update connection status
            UpdateConnectionStatus();

            // Here we would fetch issues from Jira based on current filter
            // For now, just update the status
            lblConnectionStatus.Text = "Connected to Jira";
        }
        catch (Exception ex)
        {
            lblConnectionStatus.Text = string.Format(Localization.Localizer.T("Status_ConnectionError"), ex.Message);
        }
    }

    private void UpdateConnectionStatus()
    {
        if (IsJiraEnabled)
        {
            lblConnectionStatus.Text = Localization.Localizer.T("Status_Connecting");
        }
        else
        {
            lblConnectionStatus.Text = Localization.Localizer.T("Status_NotConnected");
        }
    }

    private string CalculateTotalTime()
    {
        try
        {
            var total = TimeSpan.Zero;
            foreach (var i in issues)
            {
                if (i.IsRunning)
                    total += i.Timer.TimeElapsed;
                else if (TimeSpan.TryParse(i.Time, out var t))
                    total += t;
            }
            return total.ToString(@"hh\:mm\:ss");
        }
        catch
        {
            return "00:00:00";
        }
    }

    private void BtnStart_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string issueKey = tbIssueKey.Text?.Trim();
        if (!string.IsNullOrEmpty(issueKey))
        {
            var issue = issues.FirstOrDefault(i => i.Key == issueKey);
            if (issue != null)
            {
                ToggleIssueTimer(issueKey, null); // null button since we're using the main controls
            }
            else
            {
                // Add the issue first if it doesn't exist
                BtnAddIssue_Click(sender, e);
                // Then start it
                issue = issues.FirstOrDefault(i => i.Key == issueKey);
                if (issue != null)
                {
                    ToggleIssueTimer(issueKey, null);
                }
            }
        }
    }

    private void BtnPause_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string issueKey = tbIssueKey.Text?.Trim();
        if (!string.IsNullOrEmpty(issueKey))
        {
            var issue = issues.FirstOrDefault(i => i.Key == issueKey);
            if (issue != null && issue.IsRunning)
            {
                ToggleIssueTimer(issueKey, null);
            }
        }
    }

    private void BtnStop_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string issueKey = tbIssueKey.Text?.Trim();
        if (!string.IsNullOrEmpty(issueKey))
        {
            var issue = issues.FirstOrDefault(i => i.Key == issueKey);
            if (issue != null && issue.IsRunning)
            {
                ToggleIssueTimer(issueKey, null);
            }
        }
    }

    private void BtnAddIssue_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string issueKey = tbIssueKey.Text?.Trim();
        if (!string.IsNullOrEmpty(issueKey) && !issues.Any(i => i.Key == issueKey))
        {
            var newIssue = new IssueViewModel
            {
                Key = issueKey,
                Time = "00:00:00",
                Comment = "",
                IsRunning = false
            };
            issues.Add(newIssue);
            AddIssueControl(newIssue);
        }
    }

    private void ToggleIssueTimer(string issueKey, Button button)
    {
        var issue = issues.FirstOrDefault(i => i.Key == issueKey);
        if (issue != null)
        {
            if (issue.IsRunning)
            {
                issue.Timer.Pause();
                issue.IsRunning = false;
                // Store current displayed time
                issue.Time = issue.Timer.TimeElapsed.ToString(@"hh\:mm\:ss");
                UpdateIssueDisplayTime(issueKey, issue.Timer.TimeElapsed);
                SaveIssues();
            }
            else
            {
                issue.Timer.Start();
                issue.IsRunning = true;
            }
            // Update the button in the IssueControl
            var issueControl = issuesPanel.Children.OfType<Border>()
                .Select(b => b.Child as IssueControl)
                .FirstOrDefault(ic => ic?.Issue.Key == issueKey);
            if (issueControl != null)
            {
                issueControl.UpdateStartStopButton(issue.IsRunning);
            }
        }
    }

    private async Task LogWorkForIssue(string key)
    {
        var issue = issues.FirstOrDefault(i => i.Key == key);
        if (issue != null)
        {
            await PostAndReset(issue, key);
        }
    }

    private async Task TransitionIssueToDone(string key)
    {
        if (jiraClient == null || !jiraClient.SessionValid)
        {
            await ShowMessage(Localization.Localizer.T("Msg_Title_Error"), Localization.Localizer.T("Msg_Body_NotConfigured"));
            return;
        }

        try
        {
            // Get available transitions
            var transitions = jiraClient.GetAvailableTransitions(key);
            if (transitions == null || transitions.Transitions == null || transitions.Transitions.Count == 0)
            {
                await ShowMessage(Localization.Localizer.T("Msg_Title_Error"), Localization.Localizer.T("Msg_NoTransitions"));
                return;
            }

            // Find "Done" transition (case insensitive search for common "done" variations in multiple languages)
            var doneTransition = transitions.Transitions.FirstOrDefault(t =>
                t.Name.ToLower().Contains("done") ||
                t.Name.ToLower().Contains("closed") ||
                t.Name.ToLower().Contains("resolved") ||
                t.Name.ToLower().Contains("completed") ||
                t.Name.ToLower().Contains("finished") ||
                t.Name.ToLower().Contains("gotowe") || // Polish
                t.Name.ToLower().Contains("zamknięte") || // Polish
                t.Name.ToLower().Contains("zakończone") || // Polish
                t.Name.ToLower().Contains("ukończone")); // Polish

            if (doneTransition == null)
            {
                // If no "done" transition found, show available transitions
                var transitionNames = string.Join(", ", transitions.Transitions.Select(t => t.Name));
                await ShowMessage(Localization.Localizer.T("Msg_Title_Error"), 
                    string.Format(Localization.Localizer.T("Msg_NoDoneTransition"), transitionNames));
                return;
            }

            // Execute the transition
            bool success = jiraClient.DoTransition(key, doneTransition.Id);
            if (success)
            {
                await ShowMessage(Localization.Localizer.T("Msg_Title_Success"), 
                    string.Format(Localization.Localizer.T("Msg_TransitionSuccess"), key, doneTransition.Name));
            }
            else
            {
                await ShowMessage(Localization.Localizer.T("Msg_Title_Error"), Localization.Localizer.T("Msg_TransitionFailed"));
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error transitioning issue {key} to done: {ex.Message}", ex);
            await ShowMessage(Localization.Localizer.T("Msg_Title_Error"), Localization.Localizer.T("Msg_TransitionFailed"));
        }
    }

    private async Task ShowMessage(string title, string message)
    {
        var messageBox = new Window
        {
            Title = title,
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Topmost = true
        };

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        var okButton = new Button
        {
            Content = Localization.Localizer.T("MsgBox_OK"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        okButton.Click += (s, e) => messageBox.Close();

        var stackPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Children = { textBlock, okButton }
        };

        messageBox.Content = stackPanel;
        await messageBox.ShowDialog(this);
    }

    private void RemoveIssue(string issueKey)
    {
        var issue = issues.FirstOrDefault(i => i.Key == issueKey);
        if (issue != null)
        {
            issues.Remove(issue);
            // Remove from UI - find and remove the border containing the IssueControl
            for (int i = issuesPanel.Children.Count - 1; i >= 0; i--)
            {
                if (issuesPanel.Children[i] is Border border && border.Child is IssueControl issueControl)
                {
                    if (issueControl.Issue.Key == issueKey)
                    {
                        issuesPanel.Children.RemoveAt(i);
                        break;
                    }
                }
            }
            SaveIssues();
        }
    }

    private void SaveIssues()
    {
        try
        {
            // Update time for running issues
            foreach (var issue in issues.Where(i => i.IsRunning))
            {
                issue.Time = issue.Timer.TimeElapsed.ToString(@"hh\:mm\:ss");
            }

            var persisted = issues.Select(i => new PersistedIssue
            {
                Key = i.Key,
                TotalTime = TimeSpan.Parse(i.Time),
                Comment = i.Comment,
                TimerRunning = i.IsRunning
            }).ToList();
            Settings.Instance.PersistedIssues = Settings.Instance.WriteIssues(persisted);
            Settings.Instance.Save();
        }
        catch
        {
            // Handle error
        }
    }

    private void CbFilters_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cbFilters.SelectedIndex >= 0 && cbFilters.SelectedIndex < filters.Count)
        {
            var selectedFilter = filters[cbFilters.SelectedIndex];
            lblActiveFilter.Text = selectedFilter.Name;
            Settings.Instance.CurrentFilter = cbFilters.SelectedIndex;
            Settings.Instance.Save();
            LoadIssuesFromJira(selectedFilter.Jql);
        }
    }

    private void TbIssueKey_TextChanged(object sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (tbIssueKey.Text != null)
        {
            // Extract issue key from URL if pasted
            var text = tbIssueKey.Text.Trim();
            if (text.Contains("/browse/"))
            {
                var parts = text.Split(new[] { "/browse/" }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    var key = parts[1].Split('/')[0].Split('?')[0];
                    tbIssueKey.Text = key;
                }
            }
        }
    }

    private async void LoadIssuesFromJira(string jql)
    {
        if (!IsJiraEnabled || string.IsNullOrEmpty(jql))
            return;

        try
        {
            lblConnectionStatus.Text = "Loading issues...";
            var searchResult = await Task.Run(() => jiraClient.GetIssuesByJQL(jql));

            if (searchResult != null && searchResult.Issues != null)
            {
                // Add issues from Jira that are not already in our list
                foreach (var jiraIssue in searchResult.Issues)
                {
                    if (!issues.Any(i => i.Key == jiraIssue.Key))
                    {
                        var newIssue = new IssueViewModel
                        {
                            Key = jiraIssue.Key,
                            Time = "00:00:00",
                            Comment = jiraIssue.Fields.Summary ?? "",
                            IsRunning = false
                        };
                        issues.Add(newIssue);
                        AddIssueControl(newIssue);
                    }
                }
                lblConnectionStatus.Text = string.Format(Localization.Localizer.T("Status_ConnectedLoaded"), searchResult.Issues.Count);
            }
            else
            {
                lblConnectionStatus.Text = "Failed to load issues";
            }
        }
        catch (Exception ex)
        {
            lblConnectionStatus.Text = string.Format(Localization.Localizer.T("Status_LoadError"), ex.Message);
        }
    }

    private void MenuSettings_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Kept for compatibility, actual handler wired to OpenSettingsAsync
    }

    private void MenuAbout_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        aboutWindow.ShowDialog(this);
    }

    private void MenuExit_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private async void BtnSettings_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await OpenSettingsAsync();
    }

    private async void BtnAbout_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        await aboutWindow.ShowDialog(this);
    }

    private async void BtnSubmitWorklog_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!IsJiraEnabled)
        {
            await ShowMessage(Localization.Localizer.T("Msg_Title_NotConfigured"), Localization.Localizer.T("Msg_Body_NotConfigured"));
            return;
        }

        var issuesWithTime = issues.Where(i => TimeSpan.Parse(i.Time) > TimeSpan.Zero).ToList();
        if (issuesWithTime.Count == 0)
        {
            await ShowMessage(Localization.Localizer.T("Msg_Title_NoWorklogs"), Localization.Localizer.T("Msg_Body_NoWorklogs"));
            return;
        }

        // Show worklog summary window
        var worklogWindow = new WorklogWindow(issuesWithTime)
        {
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        await worklogWindow.ShowDialog(this);
    }

    private async System.Threading.Tasks.Task PostAndReset(IssueViewModel issue, string key)
    {
        if (!IsJiraEnabled)
        {
            await ShowMessage(Localization.Localizer.T("Msg_Title_NotConfigured"), Localization.Localizer.T("Msg_Body_NotConfigured"));
            return;
        }

        var startTime = issue.Timer.GetInitialStartTime();
        var elapsed = issue.Timer.TimeElapsed;
        var dlg = new WorklogDialogWindow(key, startTime, elapsed, issue.Comment)
        {
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        // Fetch remaining estimate
        try
        {
            var tt = jiraClient.GetIssueTimetracking(key);
            if (tt != null)
            {
                dlg.RemainingEstimate = tt.RemainingEstimate;
                dlg.RemainingEstimateSeconds = tt.RemainingEstimateSeconds;
            }
        }
        catch { }

        bool? submit = await dlg.ShowDialog<bool?>(this);
        if (submit == null)
        {
            // Save for later: keep time but pause
            issue.Timer.Pause();
            issue.IsRunning = false;
            issue.Time = elapsed.ToString(@"hh\:mm\:ss");
            UpdateIssueDisplayTime(issue.Key, elapsed);
            SaveIssues();
            return;
        }
        if (submit == true)
        {
            // Submit
            var timeToLog = dlg.TimeSpentOverride ?? elapsed;
            var ok = jiraClient.PostWorklog(key, dlg.InitialStartTime, timeToLog, dlg.Comment, dlg.EstimateUpdateMethod, dlg.EstimateUpdateValue);
            if (ok)
            {
                // Optional post comment alone
                if (Settings.Instance.PostWorklogComment == WorklogCommentSetting.CommentOnly)
                {
                    jiraClient.PostComment(key, dlg.Comment);
                }
                else if (Settings.Instance.PostWorklogComment == WorklogCommentSetting.WorklogAndComment)
                {
                    jiraClient.PostComment(key, dlg.Comment);
                }

                // Reset timer
                issue.Timer.Reset();
                issue.IsRunning = false;
                issue.Time = "00:00:00";
                UpdateIssueDisplayTime(issue.Key, TimeSpan.Zero);
                SaveIssues();
            }
            else
            {
                await ShowMessage(Localization.Localizer.T("Msg_Title_Error"), Localization.Localizer.T("Msg_Body_SubmitFailed"));
            }
        }
    }

    private async Task SubmitWorklogToJira(IssueViewModel issue)
    {
        // This would integrate with Jira API to submit worklog
        // For now, just log it
        Logger.Instance.Log($"Submitting worklog for {issue.Key}: {issue.Time}, Comment: {issue.Comment}");

        // TODO: Implement actual Jira worklog submission
        // await jiraClient.SubmitWorklog(issue.Key, TimeSpan.Parse(issue.Time), issue.Comment);
    }

    private void BtnHelp_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // For now, just show a message. Could open help documentation later.
        var messageBox = new Window
        {
            Title = Localization.Localizer.T("Help_Title"),
            Width = 300,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock
            {
                Text = Localization.Localizer.T("Help_Content"),
                Margin = new Avalonia.Thickness(20),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };
        messageBox.Show();
    }

    private void MainWindow_Closed(object sender, System.EventArgs e)
    {
        Settings.Instance.WindowWidth = Width;
        Settings.Instance.WindowHeight = Height;
        Settings.Instance.WindowPositionX = Position.X;
        Settings.Instance.WindowPositionY = Position.Y;
        Settings.Instance.Save();
        SaveIssues();
    }

    private bool IsJiraEnabled
    {
        get
        {
            return !string.IsNullOrEmpty(Settings.Instance.JiraBaseUrl) &&
                   !string.IsNullOrEmpty(Settings.Instance.Username) &&
                   !string.IsNullOrEmpty(Settings.Instance.ApiToken);
        }
    }

    private void UpdateIssueComment(string issueKey, string comment)
    {
        var issue = issues.FirstOrDefault(i => i.Key == issueKey);
        if (issue != null)
        {
            issue.Comment = comment;
            SaveIssues();
        }
    }

    private void UpdateIssueTime(string issueKey, TimeSpan time)
    {
        var issue = issues.FirstOrDefault(i => i.Key == issueKey);
        if (issue != null)
        {
            issue.Time = time.ToString(@"hh\:mm\:ss");
            UpdateIssueDisplayTime(issueKey, time);
        }
    }

    private void UpdateIssueDisplayTime(string issueKey, TimeSpan time)
    {
        // Find the IssueControl and update the time display
        var issueControl = issuesPanel.Children.OfType<Border>()
            .Select(b => b.Child as IssueControl)
            .FirstOrDefault(ic => ic?.Issue.Key == issueKey);
        if (issueControl != null)
        {
            issueControl.UpdateTime(time.ToString(@"hh\:mm\:ss"));
        }
    }

    private async Task UpdateIssueSummaryFromKey(IssueViewModel issue, IssueControl issueControl)
    {
        var text = issueControl.tbIssueKey.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
            return;
        // Extract key if URL
        if (text.Contains("/browse/"))
        {
            var parts = text.Split(new[] { "/browse/" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var key = parts[1].Split('/')[0].Split('?')[0];
                issueControl.tbIssueKey.Text = key;
                text = key;
            }
        }
        issue.Key = text;
        try
        {
            if (IsJiraEnabled && jiraClient.ValidateSession())
            {
                var details = await Task.Run(() => jiraClient.GetIssueDetails(text));
                if (details != null)
                {
                    var summary = Settings.Instance.IncludeProjectName ? details.Fields.Project.Name + ": " + details.Fields.Summary : details.Fields.Summary;
                    issueControl.UpdateSummary(summary ?? "");
                    var tooltip = details.Fields.Summary;
                    if (!string.IsNullOrEmpty(details.Fields.Description))
                    {
                        var desc = details.Fields.Description.Length > 1000 ? details.Fields.Description.Substring(0, 1000) + "..." : details.Fields.Description;
                        tooltip += "\n\n" + desc;
                    }
                    ToolTip.SetTip(issueControl.lblSummary, tooltip);
                }
                var tt = await Task.Run(() => jiraClient.GetIssueTimetracking(text));
                if (tt != null && !string.IsNullOrEmpty(tt.RemainingEstimate))
                {
                    var currentTip = ToolTip.GetTip(issueControl.lblSummary) as string ?? "";
                    ToolTip.SetTip(issueControl.lblSummary, currentTip + $"\n\nRemaining: {tt.RemainingEstimate}");
                }
            }
        }
        catch { }
        SaveIssues();
    }

    private void OpenIssueInBrowser(string key)
    {
        if (string.IsNullOrEmpty(Settings.Instance.JiraBaseUrl) || string.IsNullOrEmpty(key))
            return;
        var url = Settings.Instance.JiraBaseUrl;
        if (!url.EndsWith("/")) url += "/";
        CrossPlatformHelpers.OpenUrl(url + "browse/" + key);
    }
}
