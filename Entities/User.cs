namespace MyWorkItemBackend.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // 儲存雜湊後的密碼
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 關聯屬性
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserWorkItemStatus> WorkItemStatuses { get; set; } = new List<UserWorkItemStatus>();
}