using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace StopWatch;

public partial class IssueControl : UserControl
{
    public IssueViewModel Issue { get; set; }

    public event EventHandler<string> IssueKeyChanged;
    public event EventHandler CommentChanged;
    public event EventHandler StartStopClicked;
    public event EventHandler LogWorkClicked;
    public event EventHandler IssueSummaryClicked;
    public event EventHandler TransitionClicked;

    public IssueControl()
    {
        InitializeComponent();
        // Wire text change handlers
        tbIssueKey.TextChanged += TbIssueKey_TextChanged;
        tbComment.TextChanged += TbComment_TextChanged;
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
        var uri = new Uri(isRunning ? "avares://StopWatch/icons/pause26.png" : "avares://StopWatch/icons/play26.png");
        try
        {
            using var s = AssetLoader.Open(uri);
            btnStartStop.Content = new Image { Source = new Bitmap(s), Width = 16, Height = 16 };
        }
        catch
        {
            btnStartStop.Content = isRunning ? "Stop" : "Start";
        }
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

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // Button captions/icons to match compact layout
        // Log work (calendar icon without exclamation)
        try
        {
            var logUri = new Uri("avares://StopWatch/icons/posttime26.png");
            using var ls = AssetLoader.Open(logUri);
            btnLogWork.Content = new Image { Source = new Bitmap(ls), Width = 18, Height = 18 };
        }
        catch { btnLogWork.Content = "W"; }
        // Delete icon
        try
        {
            var delUri = new Uri("avares://StopWatch/icons/delete24.png");
            using var ds = AssetLoader.Open(delUri);
            btnRemove.Content = new Image { Source = new Bitmap(ds), Width = 16, Height = 16 };
        }
        catch { btnRemove.Content = "X"; }
        // Done/status mark as check emoji (smaller to avoid clipping)
        btnTransition.Content = new TextBlock { Text = "✅", FontSize = 14, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
    }
}