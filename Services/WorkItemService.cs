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

    public async Task<PagedResultDto<WorkItemListDto>> GetWorkItemsAsync(Guid userId, string sort = "latest", int page = 1, int pageSize = 10)
    {
        // 核心邏輯：LEFT JOIN WorkItems 與 UserWorkItemStatuses (投影優化)
        var query = from w in _context.WorkItems
                    join s in _context.UserWorkItemStatuses.Where(x => x.UserId == userId)
                    on w.WorkItemId equals s.WorkItemId into joinedStatus
                    from status in joinedStatus.DefaultIfEmpty()
                    select new
                    {
                        w.WorkItemId,
                        w.Title,
                        Status = status != null ? status.Status : "Pending",
                        w.CreatedAt
                    };

        // 根據參數進行排序
        if (sort.ToLower() == "oldest")
            query = query.OrderBy(x => x.CreatedAt);
        else
            query = query.OrderByDescending(x => x.CreatedAt);

        // 取得總筆數 (用於計算分頁)
        var totalCount = await query.CountAsync();

        // 應用分頁邏輯 (Skip & Take)，並最終轉換為 DTO 回傳
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WorkItemListDto
            {
                WorkItemId = x.WorkItemId,
                Title = x.Title,
                Status = x.Status
            }).ToListAsync();

        return new PagedResultDto<WorkItemListDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
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

        // 1. 確保這些任務真的存在 (且未被軟刪除)
        var existingWorkItemIds = await _context.WorkItems
            .Where(w => workItemIds.Contains(w.WorkItemId))
            .Select(w => w.WorkItemId)
            .ToListAsync();

        if (!existingWorkItemIds.Any()) return false;

        // 2. 一次性抓取該使用者針對這批任務「已經存在」的所有狀態
        var existingStatuses = await _context.UserWorkItemStatuses
            .Where(s => s.UserId == userId && existingWorkItemIds.Contains(s.WorkItemId))
            .ToListAsync();

        // 3. 在記憶體中比對並更新或新增
        foreach (var id in existingWorkItemIds)
        {
            var status = existingStatuses.FirstOrDefault(s => s.WorkItemId == id);

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

    public async Task<PagedResultDto<WorkItemListDto>> GetAdminWorkItemsAsync(string sort = "latest", int page = 1, int pageSize = 10)
    {
        var query = _context.WorkItems.AsQueryable();

        // 根據參數進行排序
        if (sort.ToLower() == "oldest")
            query = query.OrderBy(w => w.CreatedAt);
        else
            query = query.OrderByDescending(w => w.CreatedAt);

        var totalCount = await query.CountAsync();

        // Admin 列表不需要個人狀態，預設給 "Pending" 即可
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WorkItemListDto
            {
                WorkItemId = w.WorkItemId,
                Title = w.Title,
                Status = "Pending" 
            }).ToListAsync();

        return new PagedResultDto<WorkItemListDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<WorkItemDto?> GetAdminWorkItemByIdAsync(Guid workItemId)
    {
        return await _context.WorkItems
            .Where(w => w.WorkItemId == workItemId)
            .Select(w => new WorkItemDto
            {
                WorkItemId = w.WorkItemId,
                Title = w.Title,
                Description = w.Description,
                Status = "Pending",
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

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
