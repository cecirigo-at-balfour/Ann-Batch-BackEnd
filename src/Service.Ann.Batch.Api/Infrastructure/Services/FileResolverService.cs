using Service.Ann.Batch.Api.Common.Settings;

namespace Service.Ann.Batch.Api.Infrastructure.Services;

public class FileResolverService : IFileResolverService
{
    private readonly BatchSettings _batchSettings =
        AppSettingsAnn.AppSettings.BatchSettings;

    public (string files, DateTime?) ResolveFiles(
        string fo,
        string item)
    {
        var directories = new List<string>();

        directories.Add(_batchSettings.DefaultDirectory);

        if (item.Contains(
            "GRETADLAB DGTL",
            StringComparison.OrdinalIgnoreCase))
        {
            directories.Add(_batchSettings.AdLabDirectory);
        }

        if (item.Contains(
            "PERSONAL",
            StringComparison.OrdinalIgnoreCase))
        {
            directories.Add(_batchSettings.PersonalNoteDirectory);
        }

        if (item.Contains(
            "GPHOTO CON",
            StringComparison.OrdinalIgnoreCase))
        {
            directories.Add(_batchSettings.ContAdLabDirectory);
        }

        var matches = new List<FileInfo>();

        foreach (var dir in directories)
        {

            if (string.IsNullOrWhiteSpace(dir))
                continue;


            var filesFound = Directory.EnumerateFiles(dir)
                .Where(f => Path.GetFileName(f)
                    .Contains(fo, StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f));

            matches.AddRange((IEnumerable<FileInfo>)filesFound);
        }

        matches = matches
            .OrderBy(f => f.CreationTime)
            .ToList();

        return (
            string.Join("; ", 
            matches.Select(f => f.FullName)),
            matches.FirstOrDefault()?.CreationTime

        );
    }
    
}