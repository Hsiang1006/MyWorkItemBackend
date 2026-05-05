using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyWorkItemBackend.Controllers;
using MyWorkItemBackend.Models.DTOs;
using MyWorkItemBackend.Services;
using Xunit;

namespace MyWorkItemBackend.Tests
{
    public class WorkItemControllerTests
    {
        private WorkItemController CreateController(Mock<IWorkItemService> mockService)
        {
            var controller = new WorkItemController(mockService.Object);
            var userId = Guid.NewGuid();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }))
                }
            };
            return controller;
        }

        [Fact]
        public async Task GetList_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.GetWorkItemsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PagedResultDto<WorkItemListDto>());
            var controller = CreateController(mock);

            var result = await controller.GetList();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetDetail_Found_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.GetWorkItemByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(new WorkItemDto());
            var controller = CreateController(mock);

            var result = await controller.GetDetail(Guid.NewGuid());
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetDetail_NotFound_ReturnsNotFound()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.GetWorkItemByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((WorkItemDto?)null);
            var controller = CreateController(mock);

            var result = await controller.GetDetail(Guid.NewGuid());
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task BatchConfirm_Valid_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.BatchConfirmAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>())).ReturnsAsync(true);
            var controller = CreateController(mock);
            var req = new BatchConfirmRequest { WorkItemIds = new List<Guid> { Guid.NewGuid() } };

            var result = await controller.BatchConfirm(req);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task BatchConfirm_EmptyIds_ReturnsBadRequest()
        {
            var mock = new Mock<IWorkItemService>();
            var controller = CreateController(mock);
            var req = new BatchConfirmRequest { WorkItemIds = new List<Guid>() };

            var result = await controller.BatchConfirm(req);
            Assert.IsType<BadRequestObjectResult>(result);
        }
        
        [Fact]
        public async Task BatchConfirm_Fail_ReturnsBadRequest()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.BatchConfirmAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>())).ReturnsAsync(false);
            var controller = CreateController(mock);
            var req = new BatchConfirmRequest { WorkItemIds = new List<Guid> { Guid.NewGuid() } };

            var result = await controller.BatchConfirm(req);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task BatchConfirm_ExceedsLimit_ReturnsBadRequest()
        {
            var mock = new Mock<IWorkItemService>();
            var controller = CreateController(mock);
            
            // 產生 101 個 Guid 來模擬超過 100 筆上限的情況
            var tooManyIds = new List<Guid>();
            for (int i = 0; i < 101; i++) tooManyIds.Add(Guid.NewGuid());
            var req = new BatchConfirmRequest { WorkItemIds = tooManyIds };

            var result = await controller.BatchConfirm(req);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Revoke_Success_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.RevokeConfirmAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);
            var controller = CreateController(mock);

            var result = await controller.Revoke(Guid.NewGuid());
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Revoke_Fail_ReturnsNotFound()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.RevokeConfirmAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
            var controller = CreateController(mock);

            var result = await controller.Revoke(Guid.NewGuid());
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
