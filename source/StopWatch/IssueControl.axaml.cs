using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace StopWatch;

public partial class IssueControl : UserControl
{
    private static readonly Bitmap PlayIconBitmap = LoadBitmap("avares://StopWatch/icons/play26.png");
    private static readonly Bitmap PauseIconBitmap = LoadBitmap("avares://StopWatch/icons/pause26.png");
    private static readonly Bitmap LogWorkIconBitmap = LoadBitmap("avares://StopWatch/icons/posttime26.png");
    private static readonly Bitmap DeleteIconBitmap = LoadBitmap("avares://StopWatch/icons/delete24.png");
    private static readonly Bitmap AddIconBitmap = LoadBitmap("avares://StopWatch/icons/addissue22.png");

    public IssueViewModel Issue { get; set; }

    public event EventHandler<string> IssueKeyChanged;
    public event EventHandler<string> IssueKeyEntered;
    public event EventHandler CommentChanged;
    public event EventHandler StartStopClicked;
    public event EventHandler LogWorkClicked;
    public event EventHandler IssueSummaryClicked;
    public event EventHandler TransitionClicked;
    public event EventHandler ResetClicked;
    public event EventHandler AddRemoveClicked;

    public IssueControl()
    {
        InitializeComponent();
        // Wire text change handlers
        tbIssueKey.TextChanged += TbIssueKey_TextChanged;
        tbIssueKey.KeyDown += TbIssueKey_KeyDown;
        tbComment.TextChanged += TbComment_TextChanged;
        InitializeControlVisuals();
    }

    public void SetIssue(IssueViewModel issue)
    {
        Issue = issue;
        tbIssueKey.Text = issue.Key;
        tbComment.Text = issue.Comment;
        lblTime.Text = issue.Time;
        tbTimeBox.Text = FormatMinutes(issue.Time);
        // Localize controls
        tbComment.Watermark = Localization.Localizer.T("Issue_Comment");
        SetStartStopIcon(issue.IsRunning);
        lblSummary.Text = ""; // Will be updated later
        SetButtonsForFetched(false); // Enable only start/stop and remove for unfetched issues
        SetAddRemoveMode(issue.IsLocal);
    }

    public void UpdateSummary(string summary)
    {
        lblSummary.Text = summary;
    }

    private void TbIssueKey_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Issue != null)
        {
            Issue.Key = tbIssueKey.Text;
            IssueKeyChanged?.Invoke(this, tbIssueKey.Text);
        }
    }

    private void TbIssueKey_KeyDown(object sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            IssueKeyEntered?.Invoke(this, tbIssueKey.Text);
        }
    }

    private void TbComment_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Issue != null)
        {
            Issue.Comment = tbComment.Text;
            CommentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void BtnStartStop_Click(object sender, RoutedEventArgs e)
    {
        StartStopClicked?.Invoke(this, EventArgs.Empty);
    }

    private void BtnLogWork_Click(object sender, RoutedEventArgs e)
    {
        LogWorkClicked?.Invoke(this, EventArgs.Empty);
    }

    private void BtnTransition_Click(object sender, RoutedEventArgs e)
    {
        TransitionClicked?.Invoke(this, EventArgs.Empty);
    }

    private void LblSummary_PointerPressed(object sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        IssueSummaryClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
    {
        IssueSummaryClicked?.Invoke(this, EventArgs.Empty);
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        ResetClicked?.Invoke(this, EventArgs.Empty);
    }

    private void BtnAddRemove_Click(object sender, RoutedEventArgs e)
    {
        AddRemoveClicked?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateTime(string time)
    {
        lblTime.Text = time;
        tbTimeBox.Text = FormatMinutes(time);
    }

    public void UpdateStartStopButton(bool isRunning)
    {
        SetStartStopIcon(isRunning);
    }

    private void SetStartStopIcon(bool isRunning)
    {
        SetButtonImage(btnStartStop, imgStartStop, isRunning ? PauseIconBitmap : PlayIconBitmap, isRunning ? "Stop" : "Start", 16, 16);
    }

    public void SetIssueKeyReadOnly(bool readOnly)
    {
        try { tbIssueKey.IsReadOnly = readOnly; } catch { }
    }

    public void SetButtonsEnabled(bool enabled)
    {
        try
        {
            btnStartStop.IsEnabled = enabled;
            btnLogWork.IsEnabled = enabled;
            btnTransition.IsEnabled = enabled;
        }
        catch { }
    }

    public void SetButtonsForFetched(bool fetched)
    {
        try
        {
            // All main actions should be clickable regardless of fetch status
            btnStartStop.IsEnabled = true;
            btnLogWork.IsEnabled = true;
            btnTransition.IsEnabled = true;
            btnRemove.IsEnabled = true;
        }
        catch { }
    }

    public void FocusIssueKey()
    {
        try { tbIssueKey.Focus(); } catch { }
    }

    public void SetDoneVisual(bool done)
    {
        try
        {
            this.Opacity = done ? 0.5 : 1.0;
            // Keep actions enabled even if the issue looks done; user can still operate
            btnStartStop.IsEnabled = true;
            btnLogWork.IsEnabled = true;
            btnTransition.IsEnabled = true;
            btnRemove.IsEnabled = true;
            tbComment.IsEnabled = true;
        }
        catch { }
    }

    private static string FormatMinutes(string hhmmss)
    {
        try
        {
            if (TimeSpan.TryParse(hhmmss, out var t))
            {
                var m = (int)Math.Floor(t.TotalMinutes);
                return m + "m";
            }
        }
        catch { }
        return "0m";
    }

    private void InitializeControlVisuals()
    {
        // Button captions/icons to match compact layout
        // Log work (calendar icon without exclamation)
        SetButtonImage(btnLogWork, imgLogWork, LogWorkIconBitmap, "W", 18, 18);
        SetAddRemoveMode(true);
        // Done/status mark as check emoji (smaller to avoid clipping)
        btnTransition.Content = new TextBlock { Text = " ✅ ", FontSize = 14, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

        // Wire add/remove handler
        btnRemove.Click += BtnAddRemove_Click;

        // Ustaw lokalizowane tooltippy
        ToolTip.SetTip(btnOpenBrowser, Localization.Localizer.T("Tooltip_OpenInBrowser"));
        ToolTip.SetTip(btnReset, Localization.Localizer.T("Tooltip_Reset"));
        ToolTip.SetTip(btnLogWork, Localization.Localizer.T("Tooltip_LogWork"));
        ToolTip.SetTip(btnRemove, Localization.Localizer.T("Tooltip_Delete"));
        ToolTip.SetTip(btnTransition, Localization.Localizer.T("Tooltip_MarkDone"));
    }

    public void SetAddRemoveMode(bool isLocal)
    {
        if (isLocal)
        {
            SetButtonImage(btnRemove, imgRemove, DeleteIconBitmap, "X", 16, 16);
            ToolTip.SetTip(btnRemove, Localization.Localizer.T("Tooltip_Delete"));
        }
        else
        {
            SetButtonImage(btnRemove, imgRemove, AddIconBitmap, "+", 18, 18);
            ToolTip.SetTip(btnRemove, Localization.Localizer.T("Tooltip_AddLocal"));
        }
    }

    private static Bitmap LoadBitmap(string assetUri)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(assetUri));
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private static void SetButtonImage(Button button, Image image, Bitmap bitmap, string fallbackText, double width, double height)
    {
        if (bitmap != null)
        {
            image.Source = bitmap;
            image.Width = width;
            image.Height = height;
            if (!ReferenceEquals(button.Content, image))
            {
                button.Content = image;
            }
        }
        else
        {
            image.Source = null;
            button.Content = fallbackText;
        }
    }
}
