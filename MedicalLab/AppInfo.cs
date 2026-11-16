namespace MedicalLab;
public static class AppInfo
{
 public static string VersionLabel => "Version " + (typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.1.0");
}
