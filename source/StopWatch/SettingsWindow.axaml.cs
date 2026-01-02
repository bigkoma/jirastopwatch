using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.Linq;

namespace StopWatch;

public partial class SettingsWindow : Window
{
    public Settings settings { get; private set; }

    public SettingsWindow()
    {
        // Default constructor for XAML
    }

    public SettingsWindow(Settings settings)
    {
        this.settings = settings;
        InitializeComponent();
        ApplyLocalization();
        LoadSettings();
        SetupEnumCombos();
    }

    private void ApplyLocalization()
    {
        try
        {
            Title = Localization.Localizer.T("Settings_Title");
            txtJiraSettingsHeader.Text = Localization.Localizer.T("JiraSettings_Header");
            txtJiraBaseUrl.Text = Localization.Localizer.T("Jira_BaseUrl");
            txtUsername.Text = Localization.Localizer.T("Jira_Username");
            txtApiToken.Text = Localization.Localizer.T("Jira_Token");
            txtGetToken.Text = Localization.Localizer.T("Jira_GetToken");
            txtAppSettingsHeader.Text = Localization.Localizer.T("AppSettings_Header");
            cbAlwaysOnTop.Content = Localization.Localizer.T("App_AlwaysOnTop");
            cbMinimizeToTray.Content = Localization.Localizer.T("App_MinimizeToTray");
            cbAllowMultipleTimers.Content = Localization.Localizer.T("App_AllowMultiple");
            cbIncludeProjectName.Content = Localization.Localizer.T("App_IncludeProject");
            cbLoggingEnabled.Content = Localization.Localizer.T("App_EnableLogging");
            txtOpenLogFolder.Text = Localization.Localizer.T("App_OpenLogFolder");
            txtTimersHeader.Text = Localization.Localizer.T("Timers_Header");
            txtSaveTimerOnExit.Text = Localization.Localizer.T("SaveTimerOnExit");
            txtPauseOnLock.Text = Localization.Localizer.T("PauseOnLock");
            txtPostComment.Text = Localization.Localizer.T("PostComment");
            txtStartTransitions.Text = Localization.Localizer.T("StartTransitions");
            cbCheckForUpdate.Content = Localization.Localizer.T("CheckForUpdates");
            btnAbout.Content = Localization.Localizer.T("Btn_About");
            btnOK.Content = Localization.Localizer.T("Btn_OK");
            btnCancel.Content = Localization.Localizer.T("Btn_Cancel");
            
            // Language settings
            txtLanguage.Text = Localization.Localizer.T("Language");
        }
        catch { }
    }

    private void LoadSettings()
    {
        tbJiraBaseUrl.Text = settings.JiraBaseUrl;
        tbUsername.Text = settings.Username;
        tbApiToken.Text = settings.ApiToken;
        cbAlwaysOnTop.IsChecked = settings.AlwaysOnTop;
        cbMinimizeToTray.IsChecked = true;
        cbMinimizeToTray.IsEnabled = false;
        cbAllowMultipleTimers.IsChecked = settings.AllowMultipleTimers;
        cbIncludeProjectName.IsChecked = settings.IncludeProjectName;
        cbLoggingEnabled.IsChecked = settings.LoggingEnabled;
        cbCheckForUpdate.IsChecked = settings.CheckForUpdate;
        tbStartTransitions.Text = settings.StartTransitions;
        // Set selected enum values if items already populated
        if (cbSaveTimerState.ItemCount > 0)
            cbSaveTimerState.SelectedIndex = (int)settings.SaveTimerState;
        if (cbPauseOnSessionLock.ItemCount > 0)
            cbPauseOnSessionLock.SelectedIndex = (int)settings.PauseOnSessionLock;
        if (cbPostWorklogComment.ItemCount > 0)
            cbPostWorklogComment.SelectedIndex = (int)settings.PostWorklogComment;
    }

    private void SetupEnumCombos()
    {
        // Save timer state
        cbSaveTimerState.Items.Clear();
        cbSaveTimerState.Items.Add(Localization.Localizer.T("SaveTimer_ResetAll"));
        cbSaveTimerState.Items.Add(Localization.Localizer.T("SaveTimer_SavePause"));
        cbSaveTimerState.Items.Add(Localization.Localizer.T("SaveTimer_SaveContinue"));
        cbSaveTimerState.SelectedIndex = (int)settings.SaveTimerState;

        // Pause on session lock
        cbPauseOnSessionLock.Items.Clear();
        cbPauseOnSessionLock.Items.Add(Localization.Localizer.T("Pause_None"));
        cbPauseOnSessionLock.Items.Add(Localization.Localizer.T("Pause_Pause"));
        cbPauseOnSessionLock.Items.Add(Localization.Localizer.T("Pause_PauseResume"));
        cbPauseOnSessionLock.SelectedIndex = (int)settings.PauseOnSessionLock;

        // Worklog comment posting
        cbPostWorklogComment.Items.Clear();
        cbPostWorklogComment.Items.Add(Localization.Localizer.T("PostComment_WorklogOnly"));
        cbPostWorklogComment.Items.Add(Localization.Localizer.T("PostComment_CommentOnly"));
        cbPostWorklogComment.Items.Add(Localization.Localizer.T("PostComment_Both"));
        cbPostWorklogComment.SelectedIndex = (int)settings.PostWorklogComment;

        // Language selection
        cbLanguage.Items.Clear();
        cbLanguage.Items.Add(Localization.Localizer.T("Language_Default"));
        cbLanguage.Items.Add(Localization.Localizer.T("Language_English"));
        cbLanguage.Items.Add(Localization.Localizer.T("Language_Polish"));
        
        // Set selected language
        if (string.IsNullOrEmpty(settings.LanguageCode))
            cbLanguage.SelectedIndex = 0; // System default
        else if (settings.LanguageCode.StartsWith("en"))
            cbLanguage.SelectedIndex = 1; // English
        else if (settings.LanguageCode.StartsWith("pl"))
            cbLanguage.SelectedIndex = 2; // Polish
        else
            cbLanguage.SelectedIndex = 0; // Default to system default
    }

    private void BtnOK_Click(object sender, RoutedEventArgs e)
    {
        settings.JiraBaseUrl = tbJiraBaseUrl.Text;
        settings.Username = tbUsername.Text;
        settings.ApiToken = tbApiToken.Text;
        settings.AlwaysOnTop = cbAlwaysOnTop.IsChecked ?? false;
        settings.MinimizeToTray = true;
        settings.AllowMultipleTimers = cbAllowMultipleTimers.IsChecked ?? false;
        settings.IncludeProjectName = cbIncludeProjectName.IsChecked ?? false;
        settings.LoggingEnabled = cbLoggingEnabled.IsChecked ?? false;
        settings.CheckForUpdate = cbCheckForUpdate.IsChecked ?? true;
        settings.StartTransitions = tbStartTransitions.Text ?? string.Empty;
        settings.SaveTimerState = (SaveTimerSetting)(cbSaveTimerState.SelectedIndex >= 0 ? cbSaveTimerState.SelectedIndex : 0);
        settings.PauseOnSessionLock = (PauseAndResumeSetting)(cbPauseOnSessionLock.SelectedIndex >= 0 ? cbPauseOnSessionLock.SelectedIndex : 0);
        settings.PostWorklogComment = (WorklogCommentSetting)(cbPostWorklogComment.SelectedIndex >= 0 ? cbPostWorklogComment.SelectedIndex : 0);
        
        // Save language setting
        string oldLanguageCode = settings.LanguageCode;
        switch (cbLanguage.SelectedIndex)
        {
            case 0: settings.LanguageCode = ""; break; // System default
            case 1: settings.LanguageCode = "en"; break; // English
            case 2: settings.LanguageCode = "pl"; break; // Polish
            default: settings.LanguageCode = ""; break;
        }
        
        // Check if language changed
        bool languageChanged = oldLanguageCode != settings.LanguageCode;
        
        settings.Save();
        
        if (languageChanged)
        {
            // Show message that app needs restart with option to restart now
            var messageBox = new Window
            {
                Title = Localization.Localizer.T("Msg_Title_Info"),
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Topmost = true,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = Localization.Localizer.T("Msg_LanguageRestart"),
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(0, 0, 0, 20)
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Spacing = 10,
                            Children =
                            {
                                new Button
                                {
                                    Content = Localization.Localizer.T("Btn_RestartNow"),
                                    Tag = "restart"
                                },
                                new Button
                                {
                                    Content = Localization.Localizer.T("Btn_OK"),
                                    Tag = "ok"
                                }
                            }
                        }
                    }
                }
            };

            var buttonPanel = (messageBox.Content as StackPanel)?.Children[1] as StackPanel;
            if (buttonPanel != null)
            {
                var restartButton = buttonPanel.Children[0] as Button;
                var okButton = buttonPanel.Children[1] as Button;
                
                restartButton.Click += (s, args) => 
                {
                    messageBox.Close();
                    RestartApplication();
                };
                
                okButton.Click += (s, args) => messageBox.Close();
            }

            messageBox.ShowDialog(this);
        }
        
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenLogFolder_Tapped(object sender, Avalonia.Input.TappedEventArgs e)
    {
        var path = System.IO.Path.GetDirectoryName(Logging.Logger.Instance.LogfilePath);
        if (!string.IsNullOrEmpty(path))
            CrossPlatformHelpers.OpenFolder(path);
    }

    private void OpenApiTokens_Tapped(object sender, Avalonia.Input.TappedEventArgs e)
    {
        CrossPlatformHelpers.OpenUrl("https://id.atlassian.com/manage/api-tokens");
    }

    private async void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        await aboutWindow.ShowDialog(this);
    }

    private void RestartApplication()
    {
        try
        {
            // Get the current process path
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            
            // Start new instance
            Process.Start(currentExe);
            
            // Close current application
            var mainWindow = (Avalonia.Application.Current.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Close();
            }
            else
            {
                Environment.Exit(0);
            }
        }
        catch
        {
            // If restart fails, show error message
            var errorBox = new Window
            {
                Title = Localization.Localizer.T("Msg_Title_Error"),
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Topmost = true,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = Localization.Localizer.T("Msg_RestartFailed"),
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

            errorBox.ShowDialog(this);
        }
    }
}
