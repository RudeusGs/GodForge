namespace GodForge.Domain.Enums;

/// <summary>
/// Quyền hạn toàn cục của người dùng trên toàn hệ thống (System-level RBAC).
/// Dùng để phân quyền đăng nhập vào trang quản trị (Admin panel) hoặc các chức năng đặc quyền của server.
/// </summary>
public enum SystemRole
{
    /// <summary>Quản trị viên toàn hệ thống</summary>
    SystemAdmin,
    /// <summary>Quản trị viên hỗ trợ khách hàng</summary>
    SupportAdmin,
    /// <summary>Người dùng bình thường</summary>
    User
}
