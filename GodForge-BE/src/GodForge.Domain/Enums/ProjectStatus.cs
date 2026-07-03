namespace GodForge.Domain.Enums;

/// <summary>
/// Trạng thái vòng đời của dự án (Project) trong hệ thống.
/// Dùng để kiểm soát quyền truy cập và xác định các hành động hợp lệ (ví dụ: dự án Archived thì không thể sửa đổi).
/// </summary>
public enum ProjectStatus
{
    /// <summary>Bản nháp</summary>
    Draft,
    /// <summary>Đã kết nối với kho lưu trữ mã nguồn</summary>
    RepositoryConnected,
    /// <summary>Đang tiến hành phân tích</summary>
    Analyzing,
    /// <summary>Đang hoạt động</summary>
    Active,
    /// <summary>Đã được lưu trữ/đóng băng (Archive)</summary>
    Archived,
    /// <summary>Đã bị xóa (Soft delete)</summary>
    Deleted
}
