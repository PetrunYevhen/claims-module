using FluentValidation;

namespace ClaimsModule.Application.Claims.Commands.UploadDocument;

/// <summary>FRS section 13: 50MB max file size, MIME allowlist (BR-D-01/13).</summary>
public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // DOCX
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",       // XLSX
        "text/plain",
        "text/csv"
    };

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.DocumentType).NotEmpty();

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File exceeds the 50 MB size limit.");

        RuleFor(x => x.ContentType)
            .Must(AllowedContentTypes.Contains)
            .WithMessage("File type is not allowed. Accepted types: PDF, JPEG, PNG, DOCX, XLSX, TXT, CSV.");
    }
}
