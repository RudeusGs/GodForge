# GodForge SRS

`docs/SRS` là nguồn tài liệu chính cho Software Requirements Specification của GodForge. Luồng duy trì SRS dạng Word đã được thay thế bằng Markdown trong repo để dễ review, versioning, traceability và cập nhật cùng mã nguồn.

File Word `SRS_GodForge.docx`, nếu vẫn còn trong repo, chỉ được xem là tài liệu tham chiếu/archive. Không cập nhật nội dung chính trong Word.

## Cấu trúc tài liệu

| Đường dẫn | Nội dung |
| --- | --- |
| `00-overview.md` | Tổng quan sản phẩm, mục tiêu, vai trò người dùng. |
| `01-scope.md` | Phạm vi bao gồm, loại trừ, ranh giới và giới hạn MVP. |
| `02-architecture.md` | Kiến trúc web nhiều lớp, async workers và nguyên tắc thiết kế. |
| `03-functional/` | Yêu cầu chức năng theo module. |
| `04-database.md` | PostgreSQL schema, Redis, MinIO và quy tắc dữ liệu. |
| `05-api.md` | REST API `/api/v1`, response/error format và endpoint catalog. |
| `06-security.md` | Authentication, RBAC, secret handling, threat model. |
| `07-non-functional.md` | Performance, reliability, scalability, observability, maintainability. |
| `08-workflows.md` | Workflow nghiệp vụ chính. |
| `09-ui-ux.md` | Màn hình, navigation, states và role-based visibility. |
| `10-traceability.md` | Mapping requirement với API, database, UI và test case. |
| `11-testing-acceptance.md` | Test strategy, acceptance, DoD và test case format. |
| `12-worker-processing.md` | Queue, job lifecycle, retry, DLQ, idempotency và worker metrics. |
| `13-deployment-operations.md` | Local/dev/prod deployment, monitoring, backup, incident và DR. |

## Quy định cập nhật

- Viết bằng tiếng Việt, rõ ràng, kỹ thuật và nhất quán.
- Không thêm tính năng ngoài phạm vi SRS nếu chưa có quyết định sản phẩm.
- Khi thêm hoặc sửa requirement, phải cập nhật:
  - file module tương ứng trong `03-functional/`;
  - `10-traceability.md`;
  - `11-testing-acceptance.md` hoặc test case liên quan.
- Giữ ID yêu cầu ổn định nếu requirement vẫn cùng ý nghĩa: `FR-xx`, `NFR-xx`, `BR-xx`, `AC-xx`.
- Nếu đổi API contract, cập nhật `05-api.md` và traceability.
- Nếu đổi schema, cập nhật `04-database.md`, traceability và testing.
- Nếu đổi worker/job behavior, cập nhật `12-worker-processing.md` và các workflow liên quan.
- Nếu thông tin chưa đủ chắc chắn, ghi một mục xác nhận ngắn gọn ở file liên quan.

## Quy tắc review

- Mỗi PR thay đổi docs SRS phải kiểm tra tên sản phẩm là `GodForge`.
- Không dùng placeholder cho nội dung có thể suy ra từ SRS; ghi rõ các quyết định còn thiếu trong mục xác nhận.
- Không xóa nội dung có giá trị từ SRS gốc; nếu nội dung trùng thì gộp lại gọn hơn.
