using ClaimsModule.Domain.Common;
using Xunit;

namespace ClaimsModule.Tests.Domain;

public class FileNameSanitizerTests
{
    [Fact]
    public void Strips_directory_traversal_segments()
    {
        // BR-D-01: path traversal must not survive sanitisation.
        var result = FileNameSanitizer.Sanitize("../../etc/passwd");

        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("..", result);
        Assert.Equal("passwd", result);
    }

    [Fact]
    public void Removes_double_dot_sequences_within_a_name()
    {
        Assert.Equal("report.pdf", FileNameSanitizer.Sanitize("re..port.pdf"));
    }

    [Fact]
    public void Preserves_a_clean_filename()
    {
        Assert.Equal("police-report.pdf", FileNameSanitizer.Sanitize("police-report.pdf"));
    }

    [Fact]
    public void Falls_back_when_nothing_usable_remains()
    {
        Assert.Equal("unnamed-file", FileNameSanitizer.Sanitize("   "));
    }
}
