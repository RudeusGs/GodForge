namespace GodForge.Domain.Enums;

/// <summary>
/// Phân loại các tiến trình chạy ngầm (Background Job).
/// Dùng để xác định Handler/Consumer nào sẽ xử lý thông điệp từ Message Queue (như xử lý clone, parse dữ liệu).
/// </summary>
public enum JobType
{
    /// <summary>Tải mã nguồn về (clone)</summary>
    CloneRepository,
    /// <summary>Cập nhật mã nguồn mới nhất (fetch)</summary>
    FetchRepository,
    /// <summary>Phân tích cú pháp dự án</summary>
    ParseProject,
    /// <summary>Phân tích dự án (chuyên sâu)</summary>
    AnalyzeProject,
    /// <summary>So sánh sự thay đổi giữa các scene</summary>
    DiffScene,
    /// <summary>Tạo ảnh/video xem trước</summary>
    GeneratePreview,
    /// <summary>Gửi thông báo</summary>
    NotificationDispatch
}
