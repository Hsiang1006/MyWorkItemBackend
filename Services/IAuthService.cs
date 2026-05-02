using MyWorkItemBackend.Models.DTOs;

namespace MyWorkItemBackend.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
