using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyWorkItemBackend.Controllers;
using MyWorkItemBackend.Models.DTOs;
using MyWorkItemBackend.Services;
using Xunit;

namespace MyWorkItemBackend.Tests
{
    public class AuthControllerTests
    {
        [Fact]
        public async Task Login_ValidRequest_ReturnsOk()
        {
            var mockAuthService = new Mock<IAuthService>();
            var request = new LoginRequest { Username = "u", Password = "p" };
            var response = new LoginResponse { Status = true, Token = "t", Message = "Success" };
            mockAuthService.Setup(s => s.LoginAsync(request)).ReturnsAsync(response);
            var controller = new AuthController(mockAuthService.Object);

            var result = await controller.Login(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, okResult.Value);
        }

        [Fact]
        public async Task Login_InvalidRequest_ReturnsBadRequest()
        {
            var mockAuthService = new Mock<IAuthService>();
            var request = new LoginRequest { Username = "u", Password = "p" };
            var response = new LoginResponse { Status = false, Message = "Fail" };
            mockAuthService.Setup(s => s.LoginAsync(request)).ReturnsAsync(response);
            var controller = new AuthController(mockAuthService.Object);

            var result = await controller.Login(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(response, badRequestResult.Value);
        }
    }
}
