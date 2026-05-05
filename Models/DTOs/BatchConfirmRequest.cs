using System;
using System.Collections.Generic;

namespace MyWorkItemBackend.Models.DTOs;

public class BatchConfirmRequest
{
    public List<Guid> WorkItemIds { get; set; } = new List<Guid>();
}

public class CreateWorkItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateWorkItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}
