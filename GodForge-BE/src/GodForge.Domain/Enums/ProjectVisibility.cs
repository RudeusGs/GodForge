namespace GodForge.Domain.Enums;

/// <summary>
/// Mức độ hiển thị của dự án.
/// Dùng để xác định ai có thể tìm thấy và xem dự án (Private: chỉ thành viên; Internal: tất cả user).
/// </summary>
public enum ProjectVisibility
{
    /// <summary>Riêng tư, chỉ những thành viên được mời mới có quyền truy cập</summary>
    Private,
    /// <summary>Nội bộ, tất cả thành viên trong tổ chức đều có quyền truy cập</summary>
    Internal
}
