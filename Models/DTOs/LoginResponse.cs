namespace MyWorkItemBackend.Models.DTOs;

public class LoginResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Status { get; set; }
    public string? Token { get; set; }
    public List<string>? Roles { get; set; }
}
