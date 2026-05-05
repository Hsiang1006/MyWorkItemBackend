using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWorkItemBackend.Models.DTOs;
using MyWorkItemBackend.Services;

namespace MyWorkItemBackend.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/work-items")]
public class AdminWorkItemController : ControllerBase
{
    private readonly IWorkItemService _workItemService;

    public AdminWorkItemController(IWorkItemService workItemService)
    {
        _workItemService = workItemService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string sort = "latest", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _workItemService.GetAdminWorkItemsAsync(sort, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var item = await _workItemService.GetAdminWorkItemByIdAsync(id);
        
        if (item == null)
            return NotFound(new { message = "找不到該任務項目", status = false });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "任務標題為必填項目", status = false });

        var result = await _workItemService.CreateAdminWorkItemAsync(request);
        return Ok(new { message = "任務建立成功", status = true, data = result });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "任務標題為必填項目", status = false });

        var result = await _workItemService.UpdateAdminWorkItemAsync(id, request);
        
        if (result == null)
            return NotFound(new { message = "找不到該任務項目，無法修改", status = false });

        return Ok(new { message = "任務修改成功", status = true, data = result });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _workItemService.DeleteAdminWorkItemAsync(id);

        if (!result)
            return NotFound(new { message = "找不到該任務項目，無法刪除", status = false });

        return Ok(new { message = "任務已成功刪除", status = true });
    }
}
