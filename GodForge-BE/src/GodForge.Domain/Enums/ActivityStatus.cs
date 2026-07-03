namespace GodForge.Domain.Enums;

/// <summary>
/// Trạng thái của một hoạt động (Activity) trong dự án.
/// Được sử dụng để ghi log (activity log) các sự kiện xảy ra nhằm mục đích theo dõi (audit/traceability).
/// </summary>
public enum ActivityStatus
{
    /// <summary>Thành công</summary>
    Succeeded,
    /// <summary>Thất bại</summary>
    Failed,
    /// <summary>Bị từ chối</summary>
    Denied
}
