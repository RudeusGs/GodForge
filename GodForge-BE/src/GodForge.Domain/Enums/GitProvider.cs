namespace GodForge.Domain.Enums;

/// <summary>
/// Danh sách các nền tảng lưu trữ mã nguồn Git được hỗ trợ.
/// Dùng để xác định API hoặc phương thức kết nối cụ thể khi hệ thống tương tác với Git (webhook, fetch, clone).
/// </summary>
public enum GitProvider
{
    /// <summary>Nhà cung cấp GitHub</summary>
    GitHub,
    /// <summary>Nhà cung cấp GitLab</summary>
    GitLab,
    /// <summary>Nhà cung cấp Bitbucket</summary>
    Bitbucket,
    /// <summary>Nhà cung cấp Git chung (tự lưu trữ hoặc nền tảng khác)</summary>
    Generic
}
