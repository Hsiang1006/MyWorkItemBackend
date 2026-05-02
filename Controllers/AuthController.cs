using Microsoft.AspNetCore.Mvc;
using MyWorkItemBackend.Models.DTOs;
using MyWorkItemBackend.Services;

namespace MyWorkItemBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new LoginResponse
            {
                Message = "帳號或密碼不能為空",
                Status = false
            });
        }

        var response = await _authService.LoginAsync(request);

        if (!response.Status)
        {
            // 登入失敗 (例如：帳密錯誤)，回傳 HTTP 400
            return BadRequest(response);
        }

        // 登入成功，回傳 HTTP 200
        return Ok(response);
    }
}
