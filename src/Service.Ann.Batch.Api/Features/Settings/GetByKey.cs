using MediatR;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Settings;

public record GetSettingByKeyQuery(string ConfigKey) : IRequest<string?>;

public class GetSettingByKeyHandler : IRequestHandler<GetSettingByKeyQuery, string?>
{
    private readonly AppDbContext _context;

    public GetSettingByKeyHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string?> Handle(GetSettingByKeyQuery request, CancellationToken cancellationToken)
    {
        return await _context?.Settings
            .Where(x => x.ConfigKey == request.ConfigKey)
            .Select(x => x.ConfigValue)
            .FirstOrDefaultAsync(cancellationToken);
    }
}