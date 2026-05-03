using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWorkItemBackend.Models.DTOs;
using MyWorkItemBackend.Services;

namespace MyWorkItemBackend.Controllers;

[Authorize]
[ApiController]
[Route("api/work-items")]
public class WorkItemController : ControllerBase
{
    private readonly IWorkItemService _workItemService;

    public WorkItemController(IWorkItemService workItemService)
    {
        _workItemService = workItemService;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string sort = "latest")
    {
        var userId = GetUserId();
        var items = await _workItemService.GetWorkItemsAsync(userId, sort);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var userId = GetUserId();
        var item = await _workItemService.GetWorkItemByIdAsync(userId, id);
        
        if (item == null)
            return NotFound(new { message = "找不到該任務項目", status = false });

        return Ok(item);
    }

    [HttpPost("batch-confirm")]
    public async Task<IActionResult> BatchConfirm([FromBody] BatchConfirmRequest request)
    {
        if (request.WorkItemIds == null || !request.WorkItemIds.Any())
            return BadRequest(new { message = "請提供至少一個任務 ID", status = false });

        var userId = GetUserId();
        var result = await _workItemService.BatchConfirmAsync(userId, request.WorkItemIds);

        if (!result)
            return BadRequest(new { message = "批次確認失敗，請檢查 ID 是否正確", status = false });

        return Ok(new { message = "批次確認成功", status = true });
    }

    [HttpPost("{id}/revoke")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var userId = GetUserId();
        var result = await _workItemService.RevokeConfirmAsync(userId, id);

        if (!result)
            return NotFound(new { message = "找不到該項目的確認紀錄，無法撤銷", status = false });

        return Ok(new { message = "已成功撤銷確認狀態", status = true });
    }
}
