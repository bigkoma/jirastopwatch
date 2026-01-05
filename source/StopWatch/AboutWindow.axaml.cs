using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace StopWatch;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        ApplyLocalization();
        lblNameVersion.Text = $"{Tools.GetProductName()} v. {Tools.GetProductVersion()}";
        try
        {
            var authorBlock = this.FindControl<TextBlock>("AuthorBlock");
            if (authorBlock != null)
                authorBlock.Text = Localization.Localizer.T("About_Author");
        }
        catch { }
    }

    private void ApplyLocalization()
    {
        try
        {
            Title = Localization.Localizer.T("About_Title");
            lblNameVersion.Text = $"{Localization.Localizer.T("About_ProductName")} v. {Tools.GetProductVersion()}";
            var authorBlock = this.FindControl<TextBlock>("AuthorBlock");
            if (authorBlock != null) authorBlock.Text = Localization.Localizer.T("About_Author");
            
            // Find and update the copyright/license text block
            var copyrightBlock = this.FindControl<TextBlock>("CopyrightBlock");
            if (copyrightBlock != null)
            {
                copyrightBlock.Text = $"{Localization.Localizer.T("About_Copyright")}\n{Localization.Localizer.T("About_License")}";
            }
            
            // Find and update link texts
            var readLicenseLink = this.FindControl<TextBlock>("ReadLicenseLink");
            if (readLicenseLink != null) readLicenseLink.Text = Localization.Localizer.T("About_ReadLicense");
            
            var visitHomepageLink = this.FindControl<TextBlock>("VisitHomepageLink");
            if (visitHomepageLink != null) visitHomepageLink.Text = Localization.Localizer.T("About_VisitHomepage");
            
            // Update button text
            btnOk.Content = Localization.Localizer.T("About_Close");
        }
        catch { }
    }

    private void BtnOk_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
    private void License_Tapped(object sender, Avalonia.Input.TappedEventArgs e)
    {
        CrossPlatformHelpers.OpenUrl("http://www.apache.org/licenses/LICENSE-2.0");
    }

    private void Homepage_Tapped(object sender, Avalonia.Input.TappedEventArgs e)
    {
        CrossPlatformHelpers.OpenUrl("https://github.com/bigkoma/jirastopwatch");
    } 
}