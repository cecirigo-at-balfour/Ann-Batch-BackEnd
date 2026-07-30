using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace Service.Ann.Batch.Api.Features.Batches;


/// <summary>
/// Command to send email
/// </summary>
/// <param name="To">List of TO email addresses</param>
/// <param name="Cc">List of CC email addresses</param>
/// <param name="Message">Email message body</param>
public record SendEmailCommand(
    List<string> To,
    List<string>? Cc,
    string Message
) : IRequest<bool>;

public class SendEmailValidator : AbstractValidator<SendEmailCommand>
{
    public SendEmailValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty()
            .WithMessage("At least one recipient is required");

        RuleForEach(x => x.To)
            .EmailAddress()
            .WithMessage("Invalid TO email address");

        RuleForEach(x => x.Cc!)
            .EmailAddress()
            .When(x => x.Cc != null);

        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(2000);
    }
}

[ApiController]
[Route("emails")]
[Tags("Emails")]
public class SendEmailController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Send email
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> Send(
        [FromBody] SendEmailCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }
}


public class SendEmailHandler : IRequestHandler<SendEmailCommand, bool>
{
    private readonly ILogger<SendEmailHandler> _logger;

    public SendEmailHandler(ILogger<SendEmailHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> Handle(SendEmailCommand request, CancellationToken ct)
    {
        try
        {
            var mail = new MailMessage
            {
                From = new MailAddress("no-reply@yourdomain.com"),
                Subject = "Notification",
                Body = request.Message,
                IsBodyHtml = false
            };

            // ✅ TO recipients
            foreach (var to in request.To)
            {
                mail.To.Add(to);
            }

            // ✅ CC recipients (optional)
            if (request.Cc != null)
            {
                foreach (var cc in request.Cc)
                {
                    mail.CC.Add(cc);
                }
            }

            using var smtp = new SmtpClient("smtp.yourserver.com", 587)
            {
                Credentials = new NetworkCredential("user", "password"),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail, ct);

            _logger.LogInformation("Email sent to {To} with CC {Cc}",
                string.Join(",", request.To),
                request.Cc != null ? string.Join(",", request.Cc) : "none");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            throw;
        }
    }
}

