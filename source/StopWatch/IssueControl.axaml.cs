using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

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
    }

    public void SetIssue(IssueViewModel issue)
    {
        Issue = issue;
        tbIssueKey.Text = issue.Key;
        tbComment.Text = issue.Comment;
        lblTime.Text = issue.Time;
        // Localize controls
        tbComment.Watermark = Localization.Localizer.T("Issue_Comment");
        btnStartStop.Content = issue.IsRunning ? Localization.Localizer.T("Btn_Stop") : Localization.Localizer.T("Btn_Start");
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

    public void UpdateTime(string time)
    {
        lblTime.Text = time;
    }

    public void UpdateStartStopButton(bool isRunning)
    {
        btnStartStop.Content = isRunning ? Localization.Localizer.T("Btn_Stop") : Localization.Localizer.T("Btn_Start");
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // Localize static button captions
        btnLogWork.Content = Localization.Localizer.T("Btn_Log");
        btnRemove.Content = Localization.Localizer.T("Btn_Remove");
        btnTransition.Content = Localization.Localizer.T("Btn_Done");
    }
}