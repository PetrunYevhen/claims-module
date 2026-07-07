namespace ClaimsModule.Application.Reference.Dtos;

public class CauseOfLossCodeDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PerilCategory { get; set; } = null!;
    public int SortOrder { get; set; }
}
