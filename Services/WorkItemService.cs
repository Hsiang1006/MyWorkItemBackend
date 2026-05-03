using Microsoft.EntityFrameworkCore;
using MyWorkItemBackend.Data;
using MyWorkItemBackend.Entities;
using MyWorkItemBackend.Models.DTOs;

namespace MyWorkItemBackend.Services;

public class WorkItemService : IWorkItemService
{
    private readonly AppDbContext _context;

    public WorkItemService(AppDbContext context)
    {
        _context = context;
    }

    // --- 前台 User 端 ---

    public async Task<List<WorkItemListDto>> GetWorkItemsAsync(Guid userId, string sort = "latest")
    {
        // 核心邏輯：LEFT JOIN WorkItems 與 UserWorkItemStatuses
        var query = from w in _context.WorkItems
                    join s in _context.UserWorkItemStatuses.Where(x => x.UserId == userId)
                    on w.WorkItemId equals s.WorkItemId into joinedStatus
                    from status in joinedStatus.DefaultIfEmpty()
                    select new WorkItemListDto
                    {
                        WorkItemId = w.WorkItemId,
                        Title = w.Title,
                        Status = status != null ? status.Status : "Pending"
                    };

        // 根據參數進行排序 (列表通常依據建立時間，即便不回傳也需要排序)
        var sortQuery = from w in _context.WorkItems
                        join s in _context.UserWorkItemStatuses.Where(x => x.UserId == userId)
                        on w.WorkItemId equals s.WorkItemId into joinedStatus
                        from status in joinedStatus.DefaultIfEmpty()
                        select new { w, status };

        if (sort.ToLower() == "oldest")
            sortQuery = sortQuery.OrderBy(x => x.w.CreatedAt);
        else
            sortQuery = sortQuery.OrderByDescending(x => x.w.CreatedAt);

        return await sortQuery.Select(x => new WorkItemListDto
        {
            WorkItemId = x.w.WorkItemId,
            Title = x.w.Title,
            Status = x.status != null ? x.status.Status : "Pending"
        }).ToListAsync();
    }

    public async Task<WorkItemDto?> GetWorkItemByIdAsync(Guid userId, Guid workItemId)
    {
        var query = from w in _context.WorkItems
                    where w.WorkItemId == workItemId
                    join s in _context.UserWorkItemStatuses.Where(x => x.UserId == userId)
                    on w.WorkItemId equals s.WorkItemId into joinedStatus
                    from status in joinedStatus.DefaultIfEmpty()
                    select new WorkItemDto
                    {
                        WorkItemId = w.WorkItemId,
                        Title = w.Title,
                        Description = w.Description,
                        Status = status != null ? status.Status : "Pending",
                        ConfirmedAt = status != null ? status.ConfirmedAt : null,
                        CreatedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    };

        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> BatchConfirmAsync(Guid userId, List<Guid> workItemIds)
    {
        var utcNow = DateTime.UtcNow;
        var existingItems = await _context.WorkItems
            .Where(w => workItemIds.Contains(w.WorkItemId))
            .ToListAsync();

        if (!existingItems.Any()) return false;

        foreach (var id in workItemIds)
        {
            var status = await _context.UserWorkItemStatuses
                .FirstOrDefaultAsync(s => s.UserId == userId && s.WorkItemId == id);

            if (status == null)
            {
                _context.UserWorkItemStatuses.Add(new UserWorkItemStatus
                {
                    UserId = userId,
                    WorkItemId = id,
                    Status = "Confirmed",
                    ConfirmedAt = utcNow
                });
            }
            else
            {
                status.Status = "Confirmed";
                status.ConfirmedAt = utcNow;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeConfirmAsync(Guid userId, Guid workItemId)
    {
        var status = await _context.UserWorkItemStatuses
            .FirstOrDefaultAsync(s => s.UserId == userId && s.WorkItemId == workItemId);

        if (status == null) return false;

        status.Status = "Pending";
        status.ConfirmedAt = null;

        await _context.SaveChangesAsync();
        return true;
    }

    // --- 後台 Admin 端 ---

    public async Task<WorkItemDto> CreateAdminWorkItemAsync(CreateWorkItemRequest request)
    {
        var workItem = new WorkItem
        {
            Title = request.Title,
            Description = request.Description
        };

        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        return new WorkItemDto
        {
            WorkItemId = workItem.WorkItemId,
            Title = workItem.Title,
            Description = workItem.Description,
            Status = "Pending",
            CreatedAt = workItem.CreatedAt,
            UpdatedAt = workItem.UpdatedAt
        };
    }

    public async Task<WorkItemDto?> UpdateAdminWorkItemAsync(Guid workItemId, UpdateWorkItemRequest request)
    {
        var workItem = await _context.WorkItems.FindAsync(workItemId);
        if (workItem == null) return null;

        workItem.Title = request.Title;
        workItem.Description = request.Description;

        await _context.SaveChangesAsync();

        return new WorkItemDto
        {
            WorkItemId = workItem.WorkItemId,
            Title = workItem.Title,
            Description = workItem.Description,
            Status = "Pending", // 這裡只回傳基本資訊，Admin 端通常不關心個人狀態
            CreatedAt = workItem.CreatedAt,
            UpdatedAt = workItem.UpdatedAt
        };
    }

    public async Task<bool> DeleteAdminWorkItemAsync(Guid workItemId)
    {
        var workItem = await _context.WorkItems.FindAsync(workItemId);
        if (workItem == null) return false;

        // 這裡會觸發 AppDbContext 裡的軟刪除攔截邏輯
        _context.WorkItems.Remove(workItem);
        await _context.SaveChangesAsync();
        return true;
    }
}
