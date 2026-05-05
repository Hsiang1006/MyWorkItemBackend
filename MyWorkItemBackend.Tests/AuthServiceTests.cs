using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyWorkItemBackend.Data;
using MyWorkItemBackend.Entities;
using MyWorkItemBackend.Models.DTOs;
using MyWorkItemBackend.Services;
using Xunit;

namespace MyWorkItemBackend.Tests
{
    public class AuthServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsResponseWithToken()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            
            // 加入測試使用者與角色
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234")
            };
            context.Users.Add(user);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "User" };
            context.Roles.Add(role);
            
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });
            await context.SaveChangesAsync();

            // 模擬 JWT 設定 (加上 null 允許型別以解決警告)
            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?> {
                {"Jwt:Key", "ThisIsASuperSecretKeyForTestingPurposes123!"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpireMinutes", "60"}
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // 補上 Logger 的 Mock
            var mockLogger = new Mock<ILogger<AuthService>>();
            var authService = new AuthService(context, configuration, mockLogger.Object);
            
            var request = new LoginRequest { Username = "testuser", Password = "Test1234" };

            // Act
            var result = await authService.LoginAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Status);
            Assert.NotNull(result.Token);
            Assert.Equal("登入成功", result.Message);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsFailedResponse()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234")
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockConfig = new Mock<IConfiguration>();
            // 補上 Logger 的 Mock
            var mockLogger = new Mock<ILogger<AuthService>>();
            
            var authService = new AuthService(context, mockConfig.Object, mockLogger.Object);
            var request = new LoginRequest { Username = "testuser", Password = "WrongPassword" };

            // Act
            var result = await authService.LoginAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Status);
            Assert.Equal("帳號或密碼錯誤", result.Message);
        }
    }
}
