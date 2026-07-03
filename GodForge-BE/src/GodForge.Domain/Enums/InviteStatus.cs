namespace GodForge.Domain.Enums;

/// <summary>
/// Trạng thái lời mời tham gia vào hệ thống hoặc dự án.
/// Dùng trong quy trình mời người dùng mới (Invitation flow) để quản lý tính hợp lệ của lời mời.
/// </summary>
public enum InviteStatus
{
    /// <summary>Đang chờ phản hồi</summary>
    Pending,
    /// <summary>Đã chấp nhận</summary>
    Accepted,
    /// <summary>Đã hết hạn</summary>
    Expired,
    /// <summary>Đã bị thu hồi</summary>
    Revoked
}
