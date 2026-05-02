using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyWorkItemBackend.Data;
using MyWorkItemBackend.Models.DTOs;

namespace MyWorkItemBackend.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // 1. 查詢使用者與關聯的角色
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        // 2. 驗證帳號與密碼 (使用 BCrypt)
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new LoginResponse
            {
                Message = "帳號或密碼錯誤",
                Status = false
            };
        }

        // 3. 取得使用者的角色列表
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // 4. 產生 JWT Token
        var token = GenerateJwtToken(user.Id, roles);

        // 5. 回傳成功結果
        return new LoginResponse
        {
            Message = "登入成功",
            Status = true,
            Token = token,
            Roles = roles
        };
    }

    private string GenerateJwtToken(Guid userId, List<string> roles)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 設定 JWT 的 Claims (包含 sub 與 role)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 加入角色 Claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8), // Token 效期設為 8 小時
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
