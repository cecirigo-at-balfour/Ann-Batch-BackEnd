namespace Service.Ann.Batch.Api.Domain.Entities;

public class EmailLogEntity : GenericAuditEntity
{
    // AS400 Identifiers (Keys)
    public long RunNumber { get; set; }        // EMRUN
    public int SequenceNumber { get; set; }    // EMSEQ

    // Tracking & Status
    public string CreatedByProgram { get; set; } = string.Empty; // EMPROG
    public string ReferenceCode { get; set; } = string.Empty;    // EMREF
    public string Status { get; set; } = string.Empty;           // EMSTAT
    public string ErrorMessage { get; set; } = string.Empty;     // EMERRMSG

    // Date & Time Fields (AS400 original values)
    public string CreationDate { get; set; } = string.Empty;     // EMCURDT
    public string CreationTime { get; set; } = string.Empty;     // EMCURTM
    public string DateSent { get; set; } = string.Empty;         // EMSENTDT
    public string TimeSent { get; set; } = string.Empty;         // EMSENTTM

    // Email Data
    public string Sender { get; set; } = string.Empty;           // EMSENDR
    public string Recipient { get; set; } = string.Empty;        // EMRECIP
    public string? Cc { get; set; }                              // EMCC
    public string? Bcc { get; set; }                             // EMBCC
    public string Subject { get; set; } = string.Empty;          // EMSUBJECT

    // Content & Attachments
    public string? Graphic { get; set; }                         // EMGRAPHIC
    public string? BodyFileLink { get; set; }                    // EMBODY
    public string? AttachmentFiles { get; set; }                 // EMATTACH

    public bool IsSent { get; set; } = false;

    public DateTime? SentAt { get; set; }
    public int Attempt { get; set; } = 0;
    public string? IssueMessage { get; set; }
}