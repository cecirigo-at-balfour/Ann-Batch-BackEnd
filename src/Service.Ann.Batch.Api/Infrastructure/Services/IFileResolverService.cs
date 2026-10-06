namespace Service.Ann.Batch.Api.Infrastructure.Services;

public interface IFileResolverService
{
    (string files, DateTime?) ResolveFiles(
        string fo,
        string item);
}