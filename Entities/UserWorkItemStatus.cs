namespace MyWorkItemBackend.Entities;

public class UserWorkItemStatus
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public Guid WorkItemId { get; set; }
    public virtual WorkItem WorkItem { get; set; } = null!;

    public string Status { get; set; } = "Pending"; // "Pending" 或 "Confirmed" [cite: 470]
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}