using System;

namespace MyWorkItemBackend.Models.DTOs;

public class WorkItemDto
{
    public Guid WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending" 或 "Confirmed"
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkItemListDto
{
    public Guid WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}