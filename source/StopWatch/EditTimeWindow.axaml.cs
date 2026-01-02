using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace StopWatch;

public partial class EditTimeWindow : Window
{
    public TimeSpan Time { get; private set; }

    public EditTimeWindow()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        try
        {
            Title = Localization.Localizer.T("EditTime_Title");
            var headerText = this.FindControl<TextBlock>("HeaderText");
            if (headerText != null) headerText.Text = Localization.Localizer.T("EditTime_Header");
            
            var helpText = this.FindControl<TextBlock>("HelpText");
            if (helpText != null) helpText.Text = Localization.Localizer.T("EditTime_Help");
            
            btnOk.Content = Localization.Localizer.T("Btn_OK");
            btnCancel.Content = Localization.Localizer.T("Btn_Cancel");
        }
        catch { }
    }

    private void BtnOk_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateTimeInput())
        {
            tbTime.Background = Avalonia.Media.Brushes.Tomato;
            return;
        }
        tbTime.Background = Avalonia.Media.Brushes.White;
        Close();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public void SetInitialTime(TimeSpan time)
    {
        Time = time;
        tbTime.Text = JiraTimeHelpers.TimeSpanToJiraTime(Time);
    }

    private bool ValidateTimeInput()
    {
        var time = JiraTimeHelpers.JiraTimeToTimeSpan(tbTime.Text ?? string.Empty);
        if (time == null)
            return false;
        Time = time.Value;
        return true;
    }
}