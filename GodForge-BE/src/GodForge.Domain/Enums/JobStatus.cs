namespace GodForge.Domain.Enums;

/// <summary>
/// Trạng thái của một tiến trình chạy ngầm (Background Job) thực thi qua RabbitMQ/Worker.
/// Dùng để theo dõi tiến độ, xử lý lỗi (retry/DLQ) và thông báo cho người dùng khi công việc hoàn tất.
/// </summary>
public enum JobStatus
{
    /// <summary>Đang xếp hàng chờ xử lý</summary>
    Queued,
    /// <summary>Đang được chạy</summary>
    Running,
    /// <summary>Đang thử lại sau khi lỗi</summary>
    Retrying,
    /// <summary>Đã hoàn thành</summary>
    Completed,
    /// <summary>Bị lỗi</summary>
    Failed,
    /// <summary>Đã bị hủy</summary>
    Cancelled,
    /// <summary>Quá thời gian xử lý</summary>
    Timeout,
    /// <summary>Thất bại hoàn toàn và bị đưa vào hàng đợi lỗi</summary>
    DeadLettered
}
