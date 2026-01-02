using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace StopWatch;

public partial class WorklogWindow : Window
{
    private List<IssueViewModel> issuesToSubmit;

    public WorklogWindow()
    {
        InitializeComponent();
        issuesToSubmit = new List<IssueViewModel>();
    }

    public WorklogWindow(List<IssueViewModel> issues)
    {
        InitializeComponent();
        ApplyLocalization();
        issuesToSubmit = issues;
        LoadWorklogSummary();
    }

    private void ApplyLocalization()
    {
        try
        {
            Title = Localization.Localizer.T("Worklog_SubmitTitle");
            var header = this.FindControl<TextBlock>("headerText");
            if (header != null) header.Text = Localization.Localizer.T("Worklog_SummaryHeader");
            btnSubmit.Content = Localization.Localizer.T("Worklog_SubmitAll");
            btnCancel.Content = Localization.Localizer.T("Btn_Cancel");
        }
        catch { }
    }

    private void LoadWorklogSummary()
    {
        worklogPanel.Children.Clear();

        foreach (var issue in issuesToSubmit)
        {
            var border = new Border
            {
                BorderBrush = Avalonia.Media.Brushes.Gray,
                BorderThickness = new Avalonia.Thickness(1),
                Margin = new Avalonia.Thickness(0, 0, 0, 5),
                Padding = new Avalonia.Thickness(10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var issueText = new TextBlock 
            { 
                Text = $"{issue.Key}: {issue.Comment}", 
                FontWeight = Avalonia.Media.FontWeight.Bold 
            };
            var timeText = new TextBlock 
            { 
                Text = issue.Time, 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right 
            };

            Grid.SetColumn(issueText, 0);
            Grid.SetColumn(timeText, 1);

            grid.Children.Add(issueText);
            grid.Children.Add(timeText);

            border.Child = grid;
            worklogPanel.Children.Add(border);
        }
    }

    private async void BtnSubmit_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            btnSubmit.IsEnabled = false;
            btnSubmit.Content = Localization.Localizer.T("Worklog_Submitting");

            int submittedCount = 0;
            foreach (var issue in issuesToSubmit)
            {
                // TODO: Implement actual worklog submission
                // await SubmitWorklogToJira(issue);
                submittedCount++;
                await Task.Delay(100); // Simulate submission delay
            }

            // Show success message
            var messageBox = new Window
            {
                Title = Localization.Localizer.T("Msg_Success"),
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = string.Format(Localization.Localizer.T("Worklog_SubmitSuccess"), submittedCount),
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(0, 0, 0, 20)
                        },
                        new Button
                        {
                            Content = Localization.Localizer.T("Btn_OK"),
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        }
                    }
                }
            };

            var okButton = (messageBox.Content as StackPanel)?.Children[1] as Button;
            if (okButton != null)
            {
                okButton.Click += (s, args) => messageBox.Close();
            }

            await messageBox.ShowDialog(this);
            Close();
        }
        catch (Exception ex)
        {
            var errorBox = new Window
            {
                Title = Localization.Localizer.T("Msg_Title_Error"),
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = string.Format(Localization.Localizer.T("Worklog_SubmitError"), ex.Message),
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(0, 0, 0, 20)
                        },
                        new Button
                        {
                            Content = Localization.Localizer.T("Btn_OK"),
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        }
                    }
                }
            };

            var okButton = (errorBox.Content as StackPanel)?.Children[1] as Button;
            if (okButton != null)
            {
                okButton.Click += (s, args) => errorBox.Close();
            }

            await errorBox.ShowDialog(this);
            btnSubmit.IsEnabled = true;
            btnSubmit.Content = Localization.Localizer.T("Worklog_SubmitAll");
        }
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}