namespace GodForge.Domain.Enums;

/// <summary>
/// Trạng thái kết nối và xử lý mã nguồn của kho Git (Repository) liên kết với dự án.
/// Dùng để quản lý vòng đời của mã nguồn từ lúc mới cấu hình đến lúc clone và sẵn sàng phân tích.
/// </summary>
public enum GitRepositoryStatus
{
    /// <summary>Chưa được cấu hình</summary>
    Unconfigured,
    /// <summary>Đã cấu hình</summary>
    Configured,
    /// <summary>Đang tải mã nguồn (clone)</summary>
    Cloning,
    /// <summary>Sẵn sàng hoạt động</summary>
    Ready,
    /// <summary>Gặp lỗi</summary>
    Error,
    /// <summary>Bị vô hiệu hóa</summary>
    Disabled
}
