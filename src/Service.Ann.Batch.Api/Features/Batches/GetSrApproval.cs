using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Batches;


public class SyncSrApprovalResponse
{
    public bool? Appr { get; set; }
    public string Msg { get; set; } = "";
}

#region COMMAND

public record SyncSrApprovalCommand(
    string Sr,
    bool Appr
) : IRequest<SuccessResponse<SyncSrApprovalResponse>>;

#endregion



#region VALIDATOR

public sealed class SyncSrApprovalValidator
    : AbstractValidator<SyncSrApprovalCommand>
{
    public SyncSrApprovalValidator()
    {
        RuleFor(x => x.Sr)
            .NotEmpty()
            .WithMessage("SR is required.");
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class SyncSrApprovalController(
    IMediator mediator)
    : ControllerBase
{
    [HttpPost("sync-sr-approval")]
    public async Task<IActionResult> Sync(
        [FromBody] SyncSrApprovalCommand command,
        CancellationToken ct)
    {
        var response = await mediator.Send(
            command,
            ct);

        return Ok(response);
    }
}

#endregion

#region HANDLER

public sealed class SyncSrApprovalHandler(
    IConfiguration configuration,
    AppDbContext dbContext,
    ILogger<SyncSrApprovalHandler> logger)
    : IRequestHandler<SyncSrApprovalCommand, SuccessResponse<SyncSrApprovalResponse>>
{
    public async Task<SuccessResponse<SyncSrApprovalResponse>> Handle(
        SyncSrApprovalCommand request,
        CancellationToken ct)
    {
        try
        {
            var connectionString =
                configuration.GetConnectionString(
                    "DefaultConnectionOracle");

            await using var connection =
                new OracleConnection(connectionString);

            const string sql = @"
                SELECT T$APPR
                FROM TRITON.TTMXCH401100
                WHERE T$SRNO = :sr
            ";

            var approval =
                await connection.QueryFirstOrDefaultAsync<string>(
                    sql,
                    new { sr = request.Sr });

            if (string.IsNullOrWhiteSpace(approval))
            {
                return new SuccessResponse<SyncSrApprovalResponse>(
                    new SyncSrApprovalResponse { Appr = null, Msg = $"SR {request.Sr} not found" });
            }

            bool oracleApproved = approval == "1";

            if (!request.Appr && oracleApproved)
            {
                var batches = await dbContext.Batches
                    .AsTracking()
                    .Where(x => x.Sr == request.Sr)
                    .ToListAsync(ct);

                foreach (var batch in batches)
                {
                    batch.Srapproved = true;
                    batch.Srapprdate = DateTime.UtcNow;
                }

                await dbContext.SaveChangesAsync(ct);

                return new SuccessResponse<SyncSrApprovalResponse>(
                      new SyncSrApprovalResponse
                      {
                          Appr = true,
                          Msg = $"{batches.Count} batch(es) updated"
                      });
            }

            return new SuccessResponse<SyncSrApprovalResponse>(
                new SyncSrApprovalResponse { Appr = false,  Msg = "No changes required, Approval status already synchronized"});
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error synchronizing approval for SR {Sr}",
                request.Sr);

            throw;
        }
    }
}

#endregion