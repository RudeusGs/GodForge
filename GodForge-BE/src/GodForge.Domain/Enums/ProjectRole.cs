namespace GodForge.Domain.Enums;

/// <summary>
/// Vai trò của người dùng bên trong một dự án cụ thể.
/// Dùng cho hệ thống phân quyền (Project-level RBAC) để kiểm soát quyền thao tác trên tài nguyên dự án.
/// </summary>
public enum ProjectRole
{
    /// <summary>Chủ sở hữu dự án</summary>
    ProjectOwner,
    /// <summary>Quản trị viên dự án</summary>
    ProjectAdmin,
    /// <summary>Lập trình viên</summary>
    Developer,
    /// <summary>Người đánh giá (Reviewer)</summary>
    Reviewer,
    /// <summary>Người chỉ xem (Viewer)</summary>
    Viewer
}
