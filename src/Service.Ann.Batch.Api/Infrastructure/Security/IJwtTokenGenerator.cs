
using Service.Ann.Batch.Api.Domain.Entities;
namespace Service.Ann.Batch.Api.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(UserEntity user);
}
