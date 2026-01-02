using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace StopWatch;

public partial class WorklogDialogWindow : Window
{
    public WorklogDialogWindow()
    {
        InitializeComponent();
    }

    public string Comment => tbComment.Text ?? string.Empty;
    public EstimateUpdateMethods EstimateUpdateMethod { get; private set; } = EstimateUpdateMethods.Auto;
    public string EstimateUpdateValue { get; private set; }
    public DateTimeOffset InitialStartTime
    {
        get
        {
            var date = startDatePicker.SelectedDate.HasValue ? startDatePicker.SelectedDate.Value.Date : DateTimeOffset.Now.Date;
            var time = startTimePicker.SelectedTime ?? TimeSpan.Zero;
            return date + time;
        }
    }

    public string RemainingEstimate
    {
        get => _remainingEstimate;
        set { _remainingEstimate = value; UpdateRemainingText(); }
    }

    public int RemainingEstimateSeconds
    {
        get => _remainingEstimateSeconds;
        set { _remainingEstimateSeconds = value; UpdateRemainingText(); }
    }

    private string _remainingEstimate;
    private int _remainingEstimateSeconds;

    public WorklogDialogWindow(string issueKey, DateTimeOffset startTime, TimeSpan timeElapsed, string comment)
    {
        InitializeComponent();
        Title = Localization.Localizer.T("Worklog_Title");
        lblIssue.Text = $"{issueKey} - {JiraTimeHelpers.TimeSpanToJiraTime(timeElapsed)}";
        // Localize labels
        try
        {
            (this.FindControl<TextBlock>("txtAddComment") ?? new TextBlock()).Text = Localization.Localizer.T("Worklog_AddComment");
        }
        catch { }
        if (!string.IsNullOrEmpty(comment))
            tbComment.Text = comment;

        startDatePicker.SelectedDate = startTime.Date;
        startTimePicker.SelectedTime = startTime.TimeOfDay;
        // Pre-fill time spent with current elapsed (editable by user)
        tbTimeSpent.Text = JiraTimeHelpers.TimeSpanToJiraTime(timeElapsed);

        // Localize static labels and buttons
        txtTimeSpent.Text = Localization.Localizer.T("Worklog_TimeSpent");
        txtRemainingHeader.Text = Localization.Localizer.T("Worklog_RemainingHeader");
        rdAuto.Content = Localization.Localizer.T("Worklog_AdjustAuto");
        rdLeave.Content = Localization.Localizer.T("Worklog_LeaveAsIs");
        rdSetTo.Content = Localization.Localizer.T("Worklog_SetTo");
        rdReduceBy.Content = Localization.Localizer.T("Worklog_ReduceBy");
        txtStartDate.Text = Localization.Localizer.T("Worklog_StartDate");
        txtStartTime.Text = Localization.Localizer.T("Worklog_StartTime");
        btnSave.Content = Localization.Localizer.T("Worklog_SaveForLater");
        btnOk.Content = Localization.Localizer.T("Worklog_Submit");
        btnCancel.Content = Localization.Localizer.T("Worklog_Cancel");
    }

    private void Estimate_Checked(object sender, RoutedEventArgs e)
    {
        tbSetTo.IsEnabled = rdSetTo.IsChecked == true;
        tbReduceBy.IsEnabled = rdReduceBy.IsChecked == true;

        if (rdAuto.IsChecked == true) EstimateUpdateMethod = EstimateUpdateMethods.Auto;
        else if (rdLeave.IsChecked == true) EstimateUpdateMethod = EstimateUpdateMethods.Leave;
        else if (rdSetTo.IsChecked == true) EstimateUpdateMethod = EstimateUpdateMethods.SetTo;
        else if (rdReduceBy.IsChecked == true) EstimateUpdateMethod = EstimateUpdateMethods.ManualDecrease;
    }

    private void TbEstimate_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb != null)
        {
            var ts = JiraTimeHelpers.JiraTimeToTimeSpan(tb.Text ?? string.Empty);
            if (ts == null)
            {
                tb.Background = Avalonia.Media.Brushes.Tomato;
            }
            else
            {
                tb.Background = Avalonia.Media.Brushes.White;
            }
        }
    }

    private void BtnOk_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateEstimateInputs()) return;
        Close(true);
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateEstimateInputs()) return;
        Close(null); // indicate save for later
    }

    public TimeSpan? TimeSpentOverride
    {
        get
        {
            var ts = JiraTimeHelpers.JiraTimeToTimeSpan(tbTimeSpent.Text ?? string.Empty);
            return ts;
        }
    }

    private bool ValidateEstimateInputs()
    {
        if (EstimateUpdateMethod == EstimateUpdateMethods.SetTo)
        {
            if (!ValidateTimeText(tbSetTo)) return false;
            EstimateUpdateValue = tbSetTo.Text;
        }
        else if (EstimateUpdateMethod == EstimateUpdateMethods.ManualDecrease)
        {
            if (!ValidateTimeText(tbReduceBy)) return false;
            EstimateUpdateValue = tbReduceBy.Text;
        }
        else
        {
            EstimateUpdateValue = null;
        }
        return true;
    }

    private bool ValidateTimeText(TextBox tb)
    {
        var ts = JiraTimeHelpers.JiraTimeToTimeSpan(tb.Text ?? string.Empty);
        if (ts == null)
        {
            tb.Background = Avalonia.Media.Brushes.Tomato;
            return false;
        }
        tb.Background = Avalonia.Media.Brushes.White;
        return true;
    }

    private void UpdateRemainingText()
    {
        if (string.IsNullOrWhiteSpace(RemainingEstimate))
        {
            lblRemaining.Text = "";
        }
        else
        {
            lblRemaining.Text = $"Remaining: {RemainingEstimate} ({RemainingEstimateSeconds / 60}m)";
        }
    }
}
