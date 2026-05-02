namespace MyWorkItemBackend.Entities;

public class WorkItem
{
    public Guid WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty; // [cite: 472] 標題必填
    public string? Description { get; set; }
    public Guid? CreatedBy { get; set; } // 紀錄建立的管理員 ID
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<UserWorkItemStatus> UserStatuses { get; set; } = new List<UserWorkItemStatus>();
}