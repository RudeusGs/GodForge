# 4. Kiến trúc Dữ liệu (Database)

## 4.1 Cơ sở dữ liệu chính: PostgreSQL

### Core Schema (Dữ liệu cốt lõi)
Lưu trữ thông tin nghiệp vụ quan trọng, yêu cầu backup thường xuyên.
- **`users`**: Tài khoản người dùng.
- **`roles`**: Quyền hạn (RBAC).
- **`projects`**: Thông tin dự án Atlas.
- **`project_members`**: Liên kết user và project kèm role.
- **`repositories`**: Cấu hình Git repo, tham chiếu credential mã hóa.
- **`activities`**: Audit log cho các thao tác quan trọng.
- **`jobs`**: Trạng thái tác vụ bất đồng bộ của Worker.

### Metadata Schema (Dữ liệu phân tích)
Dữ liệu sinh ra từ Worker, có thể tái tạo lại từ Repository.
- **`scenes`**: Metadata của `.tscn`.
- **`scene_nodes`**: Cấu trúc node tree của scene.
- **`assets`**: Thông tin file asset (hình ảnh, âm thanh...).
- **`resources`**: Thông tin file `.tres`, `.res`.
- **`scripts`**: Thông tin file `.gd`.
- **`dependencies`**: Quan hệ phụ thuộc (cạnh của graph).
- **`health_reports` & `health_issues`**: Báo cáo lỗi, cảnh báo của dự án.

## 4.2 Cache & Lock: Redis
- **Dashboard Cache:** Cache thống kê, số lượng file để API phản hồi nhanh (P95 < 2s).
- **Distributed Lock:** Lock repository khi Worker đang chạy thao tác Git (ngăn chặn conflict).
- **Rate Limit & Session:** Quản lý chống spam API.

## 4.3 Object Storage: MinIO
Lưu trữ dữ liệu lớn không phù hợp lưu trong RDBMS:
- File Diff artifact sinh ra từ Diff Worker.
- Hình ảnh Preview.
- File Report export.
- Repository archive snapshot.

## 4.4 Quy tắc thiết kế Entity
- Mọi bảng bắt buộc có khóa chính (Primary Key).
- Cột Audit: `created_at`, `updated_at`.
- Đánh Index hợp lý cho các trường dùng để tìm kiếm, filter.
- Ràng buộc Foreign Key chặt chẽ ở Core Schema. Hạn chế FK quá phức tạp ở Metadata Schema nếu ảnh hưởng hiệu năng Worker lúc bulk insert.
