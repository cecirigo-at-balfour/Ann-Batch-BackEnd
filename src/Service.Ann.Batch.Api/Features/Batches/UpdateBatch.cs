namespace Service.Ann.Batch.Api.Features.Batches;


using FluentValidation;
using global::Service.Ann.Batch.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


#region COMMAND

public record UpdateBatchCommand(
    Guid Id,
    DateTime BatchDate,
    string Fo,
    string Item,
    string School,
    string StudentName,
    string? StudentLastName,
    string? Sr,
    bool? Srapproved,    
    string? Magento,
    string? Po,
    string? Uuid,
    string? ShipTracking,
    string? ShipMethod,
    DateTime? ShipDate,
    string? AddressName,
    string? ShipAddress,
    string? ShipAddress2,
    string? ShipAddress3,
    string? ShipCity,
    string? ShipState,
    string? ShipCode,
    decimal? OrderTotal,
    decimal? Payments,
    decimal? BalanceDue,
    DateTime? DeliveryDate,
    DateTime? BookDate,
    string? Files,
    DateTime? FilesDate,
    string? Priority,
    string? Status,
    int? Quantity
) : IRequest<bool>;

#endregion

#region VALIDATOR

public class UpdateBatchValidator
    : AbstractValidator<UpdateBatchCommand>
{
    public UpdateBatchValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Fo)
            .NotEmpty();      
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class UpdateBatchController(
    IMediator mediator)
    : ControllerBase
{
    /// <summary>
    /// Update Batch
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBatchCommand command,
        CancellationToken ct)
    {
        var request = command with { Id = id };

        var result = await mediator.Send(
            request,
            ct);

        return Ok(result);
    }
}

#endregion

#region HANDLER

public class UpdateBatchHandler(
    AppDbContext context)
    : IRequestHandler<UpdateBatchCommand, bool>
{
    public async Task<bool> Handle(
        UpdateBatchCommand request,
        CancellationToken ct)
    {
        var batch = await context.Batches
            .AsTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.Id,
                ct);

        if (batch == null)
            throw new Exception("Batch not found");

        batch.BatchDate = request.BatchDate;
        batch.Fo = request.Fo;
        batch.Item = request.Item;
        batch.School = request.School;
        batch.StudentName = request.StudentName;
        batch.StudentLastName = request.StudentLastName;
        batch.Sr = request.Sr;       
        batch.Magento = request.Magento;
        batch.Po = request.Po;
        batch.Uuid = request.Uuid;

        batch.ShipTracking = request.ShipTracking;
        batch.ShipMethod = request.ShipMethod;
        batch.ShipDate = request.ShipDate;

        batch.AddressName = request.AddressName;
        batch.ShipAddress = request.ShipAddress;
        batch.ShipAddress2 = request.ShipAddress2;
        batch.ShipAddress3 = request.ShipAddress3;
        batch.ShipCity = request.ShipCity;
        batch.ShipState = request.ShipState;
        batch.ShipCode = request.ShipCode;

        batch.OrderTotal = request.OrderTotal;
        batch.Payments = request.Payments;
        batch.BalanceDue = request.BalanceDue;

        batch.DeliveryDate = request.DeliveryDate;
        batch.BookDate = request.BookDate;

        batch.Files = request.Files;
        batch.FilesDate = request.FilesDate;

        batch.Priority = request.Priority;
        batch.Status = request.Status;
        batch.Quantity = request.Quantity;
        await context.SaveChangesAsync(ct);

        return true;
    }
}

#endregion