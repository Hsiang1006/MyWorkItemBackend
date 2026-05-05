using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyWorkItemBackend.Controllers;
using MyWorkItemBackend.Models.DTOs;
using MyWorkItemBackend.Services;
using Xunit;

namespace MyWorkItemBackend.Tests
{
    public class AdminWorkItemControllerTests
    {
        [Fact]
        public async Task GetList_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.GetAdminWorkItemsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PagedResultDto<WorkItemListDto>());
            var controller = new AdminWorkItemController(mock.Object);

            var result = await controller.GetList();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetDetail_Found_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            var id = Guid.NewGuid();
            mock.Setup(s => s.GetAdminWorkItemByIdAsync(id)).ReturnsAsync(new WorkItemDto());
            var controller = new AdminWorkItemController(mock.Object);

            var result = await controller.GetDetail(id);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetDetail_NotFound_ReturnsNotFound()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.GetAdminWorkItemByIdAsync(It.IsAny<Guid>())).ReturnsAsync((WorkItemDto?)null);
            var controller = new AdminWorkItemController(mock.Object);

            var result = await controller.GetDetail(Guid.NewGuid());
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Create_ValidRequest_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.CreateAdminWorkItemAsync(It.IsAny<CreateWorkItemRequest>())).ReturnsAsync(new WorkItemDto());
            var controller = new AdminWorkItemController(mock.Object);

            // 必須給定 Title 才能通過 Controller 的驗證
            var request = new CreateWorkItemRequest { Title = "Test Title" };
            var result = await controller.Create(request);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Create_EmptyTitle_ReturnsBadRequest()
        {
            var mock = new Mock<IWorkItemService>();
            var controller = new AdminWorkItemController(mock.Object);

            // Title 為空，預期回傳 BadRequest
            var request = new CreateWorkItemRequest { Title = "" };
            var result = await controller.Create(request);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_Found_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.UpdateAdminWorkItemAsync(It.IsAny<Guid>(), It.IsAny<UpdateWorkItemRequest>())).ReturnsAsync(new WorkItemDto());
            var controller = new AdminWorkItemController(mock.Object);

            var request = new UpdateWorkItemRequest { Title = "Updated Title" };
            var result = await controller.Update(Guid.NewGuid(), request);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.UpdateAdminWorkItemAsync(It.IsAny<Guid>(), It.IsAny<UpdateWorkItemRequest>())).ReturnsAsync((WorkItemDto?)null);
            var controller = new AdminWorkItemController(mock.Object);

            var request = new UpdateWorkItemRequest { Title = "Updated Title" };
            var result = await controller.Update(Guid.NewGuid(), request);
            Assert.IsType<NotFoundObjectResult>(result);
        }
        
        [Fact]
        public async Task Update_EmptyTitle_ReturnsBadRequest()
        {
            var mock = new Mock<IWorkItemService>();
            var controller = new AdminWorkItemController(mock.Object);

            var request = new UpdateWorkItemRequest { Title = null };
            var result = await controller.Update(Guid.NewGuid(), request);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_Found_ReturnsOk()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.DeleteAdminWorkItemAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            var controller = new AdminWorkItemController(mock.Object);

            var result = await controller.Delete(Guid.NewGuid());
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            var mock = new Mock<IWorkItemService>();
            mock.Setup(s => s.DeleteAdminWorkItemAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            var controller = new AdminWorkItemController(mock.Object);

            var result = await controller.Delete(Guid.NewGuid());
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
