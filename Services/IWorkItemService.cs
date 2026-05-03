using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyWorkItemBackend.Models.DTOs;

namespace MyWorkItemBackend.Services;

public interface IWorkItemService
{
    // --- 前台 User 端 ---
    
    /// <summary>
    /// 取得任務列表（根據 userId 執行 LEFT JOIN 合併個人狀態）
    /// </summary>
    Task<List<WorkItemListDto>> GetWorkItemsAsync(Guid userId, string sort = "latest");

    /// <summary>
    /// 取得單一任務詳情
    /// </summary>
    Task<WorkItemDto?> GetWorkItemByIdAsync(Guid userId, Guid workItemId);

    /// <summary>
    /// 批次確認任務
    /// </summary>
    Task<bool> BatchConfirmAsync(Guid userId, List<Guid> workItemIds);

    /// <summary>
    /// 撤銷任務確認狀態
    /// </summary>
    Task<bool> RevokeConfirmAsync(Guid userId, Guid workItemId);


    // --- 後台 Admin 端 ---

    /// <summary>
    /// 管理員：建立任務
    /// </summary>
    Task<WorkItemDto> CreateAdminWorkItemAsync(CreateWorkItemRequest request);

    /// <summary>
    /// 管理員：修改任務
    /// </summary>
    Task<WorkItemDto?> UpdateAdminWorkItemAsync(Guid workItemId, UpdateWorkItemRequest request);

    /// <summary>
    /// 管理員：刪除任務（軟刪除）
    /// </summary>
    Task<bool> DeleteAdminWorkItemAsync(Guid workItemId);
}