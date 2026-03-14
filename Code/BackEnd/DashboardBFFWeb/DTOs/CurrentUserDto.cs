namespace DashboardBff.Models.Auth;

public sealed class CurrentUserDto
{
    public string? Name { get; set; }
    public string[] Roles { get; set; } = [];
    public ClaimDto[] Claims { get; set; } = [];
}

public sealed class ClaimDto
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}