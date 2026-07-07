namespace ClaimsModule.Domain.Common;

/// <summary>BR-D-01: strips directory components and invalid/path-traversal characters from an uploaded filename.</summary>
public static class FileNameSanitizer
{
    public static string Sanitize(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalidChars = Path.GetInvalidFileNameChars();

        var cleaned = new string(name.Where(c => !invalidChars.Contains(c)).ToArray())
            .Replace("..", string.Empty);

        return string.IsNullOrWhiteSpace(cleaned) ? "unnamed-file" : cleaned;
    }
}
