namespace GodForge.Domain.Enums;

/// <summary>
/// Trạng thái tài khoản người dùng trong hệ thống.
/// Dùng cho luồng xác thực (Authentication) và quản lý tài khoản (kiểm tra có được phép gọi API không).
/// </summary>
public enum UserStatus
{
    /// <summary>Đang chờ kích hoạt tài khoản</summary>
    PendingActivation,
    /// <summary>Tài khoản đang hoạt động</summary>
    Active,
    /// <summary>Tài khoản bị khóa (do nhập sai mật khẩu nhiều lần, v.v.)</summary>
    Locked,
    /// <summary>Tài khoản bị vô hiệu hóa (bởi Admin)</summary>
    Disabled,
    /// <summary>Tài khoản đã bị xóa (Soft delete)</summary>
    Deleted
}
