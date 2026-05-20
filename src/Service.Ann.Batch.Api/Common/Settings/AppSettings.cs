namespace Service.Ann.Batch.Api.Common.Settings;
public class AppSettingsAnn
{
    public static AppSettings AppSettings { get; set; } = new();
}

public class AppSettings
{
    public BatchSettings BatchSettings { get; set; } = new();
    public PrintboxSettings PrintboxSettings { get; set; } = new();
}

public class BatchSettings
{
    public string DefaultDirectory { get; set; } = "";
    public string AdLabDirectory { get; set; } = "";
    public string PersonalNoteDirectory { get; set; } = "";
}

public class PrintboxSettings
{
    public string TokenUrl { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
