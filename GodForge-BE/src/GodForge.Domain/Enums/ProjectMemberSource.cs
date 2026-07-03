namespace GodForge.Domain.Enums;

/// <summary>
/// Xác định nguồn gốc quyền hạn của thành viên trong dự án.
/// Dùng để quản lý phân quyền (RBAC) và xác định xem quyền này có thể gỡ bỏ trực tiếp hay không.
/// </summary>
public enum ProjectMemberSource
{
    /// <summary>Được thêm trực tiếp vào dự án</summary>
    Direct,
    /// <summary>Thông qua nhóm/đội (Team)</summary>
    Team,
    /// <summary>Được kế thừa từ quyền cấp cao hơn</summary>
    Inherited
}
