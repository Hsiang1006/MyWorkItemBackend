using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyWorkItemBackend.Data;
using MyWorkItemBackend.Entities;
using MyWorkItemBackend.Models.DTOs;
using MyWorkItemBackend.Services;
using Xunit;

namespace MyWorkItemBackend.Tests
{
    public class WorkItemServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetWorkItemsAsync_ReturnsPagedData()
        {
            using var context = GetInMemoryDbContext();
            var service = new WorkItemService(context);
            var userId = Guid.NewGuid();
            context.WorkItems.Add(new WorkItem { WorkItemId = Guid.NewGuid(), Title = "A" });
            await context.SaveChangesAsync();

            var result = await service.GetWorkItemsAsync(userId);

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task BatchConfirmAsync_Success_ReturnsTrue()
        {
            using var context = GetInMemoryDbContext();
            var service = new WorkItemService(context);
            var userId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            context.WorkItems.Add(new WorkItem { WorkItemId = workItemId, Title = "A" });
            await context.SaveChangesAsync();

            var result = await service.BatchConfirmAsync(userId, new List<Guid> { workItemId });

            Assert.True(result);
            var status = await context.UserWorkItemStatuses.FirstOrDefaultAsync();
            Assert.NotNull(status);
            Assert.Equal("Confirmed", status.Status);
        }

        [Fact]
        public async Task BatchConfirmAsync_ItemNotFound_ReturnsFalse()
        {
            using var context = GetInMemoryDbContext();
            var service = new WorkItemService(context);
            var result = await service.BatchConfirmAsync(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });
            Assert.False(result);
        }

        [Fact]
        public async Task RevokeConfirmAsync_Success_ReturnsTrue()
        {
            using var context = GetInMemoryDbContext();
            var service = new WorkItemService(context);
            var userId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            context.UserWorkItemStatuses.Add(new UserWorkItemStatus { UserId = userId, WorkItemId = workItemId, Status = "Confirmed" });
            await context.SaveChangesAsync();

            var result = await service.RevokeConfirmAsync(userId, workItemId);

            Assert.True(result);
            var status = await context.UserWorkItemStatuses.FirstAsync();
            Assert.Equal("Pending", status.Status);
        }

        [Fact]
        public async Task CreateAdminWorkItem_Success_ReturnsDto()
        {
            using var context = GetInMemoryDbContext();
            var service = new WorkItemService(context);
            
            var req = new CreateWorkItemRequest { Title = "T", Description = "D" };
            var result = await service.CreateAdminWorkItemAsync(req);
            
            Assert.NotNull(result);
            Assert.Equal("T", result.Title);
            Assert.Equal(1, await context.WorkItems.CountAsync());
        }

        [Fact]
        public async Task UpdateAdminWorkItem_Success_ReturnsDto()
        {
            using var context = GetInMemoryDbContext();
            var service = new WorkItemService(context);
            var id = Guid.NewGuid();
            context.WorkItems.Add(new WorkItem { WorkItemId = id, Title = "Old" });
            await context.SaveChangesAsync();

            var result = await service.UpdateAdminWorkItemAsync(id, new UpdateWorkItemRequest { Title = "New" });

            Assert.NotNull(result);
            Assert.Equal("New", result.Title);
            Assert.Equal("New", (await context.WorkItems.FirstAsync()).Title);
        }

        [Fact]
        public async Task DeleteAdminWorkItem_Success_ReturnsTrue()
        {
            using var context = GetInMemoryDbContext();
            var service = new WorkItemService(context);
            var id = Guid.NewGuid();
            context.WorkItems.Add(new WorkItem { WorkItemId = id, Title = "A" });
            await context.SaveChangesAsync();

            var result = await service.DeleteAdminWorkItemAsync(id);

            Assert.True(result);
            // 驗證軟刪除有成功設定 IsDeleted
            var item = await context.WorkItems.IgnoreQueryFilters().FirstAsync();
            Assert.True(item.IsDeleted);
        }
    }
}
