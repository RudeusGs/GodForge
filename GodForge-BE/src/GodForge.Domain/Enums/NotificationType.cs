namespace GodForge.Domain.Enums;

/// <summary>
/// Loại thông báo gửi đến người dùng (In-app Notification).
/// Dùng để phân loại mức độ quan trọng hoặc bối cảnh của thông báo để hiển thị UI phù hợp.
/// </summary>
public enum NotificationType
{
    /// <summary>Thông tin chung</summary>
    Info,
    /// <summary>Cảnh báo</summary>
    Warning,
    /// <summary>Lỗi</summary>
    Error,
    /// <summary>Thành công</summary>
    Success,
    /// <summary>Công việc nền đã hoàn thành</summary>
    JobCompleted,
    /// <summary>Công việc nền bị lỗi</summary>
    JobFailed
}
