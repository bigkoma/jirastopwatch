using StopWatch;
using System.Reflection;

public static class Tools
{
    public static string GetProductName()
    {
        return "Jira StopWatch by Komasa";
    }

    public static string GetProductVersion()
    {
        try
        {
            var attr = typeof(AboutWindow).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            return attr?.InformationalVersion ?? "";
        }
        catch
        {
            return "";
        }
    }
}