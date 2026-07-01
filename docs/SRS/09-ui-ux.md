# 9. UI/UX

## Mục tiêu

UI/UX của GodForge phải phục vụ workflow kỹ thuật lặp lại: quản lý project, kiểm tra Git, xem metadata, review diff, theo dõi health và audit. Giao diện ưu tiên rõ ràng, mật độ thông tin vừa đủ, trạng thái hệ thống dễ hiểu và thao tác nguy hiểm có xác nhận.

## Navigation chính

| Khu vực | Nội dung |
| --- | --- |
| Global header | Project switcher, search, notification bell, user menu. |
| Project sidebar | Dashboard, Repository/Git, Scenes, Assets, Dependency Graph, Health, Scene Diff, Activity, Settings. |
| Admin area | User Management, System Activity, system settings nếu user là System Admin. |
| Context actions | Buttons theo màn hình: parse, analyze, commit, push, pull, invite member, connect repository. |

## Danh sách màn hình

| Màn hình | Mục đích | Thành phần chính |
| --- | --- | --- |
| Login | Đăng nhập hệ thống. | Email/password form, validation, lock/disabled messages. |
| Dashboard | Tổng quan project. | Health score, repo status, metadata summary, jobs, activity, top issues. |
| Project List | Xem/tìm project. | List/table, search, filters, create project. |
| Project Detail | Xem thông tin project. | Summary, members, settings link, repository state. |
| Repository Settings | Kết nối/cập nhật repository. | Remote URL, credential state, default branch, clone status. |
| Git UI | Thao tác Git. | Status, staged/unstaged files, commit form, push/pull, conflict panel. |
| Commit History | Xem lịch sử commit. | Commit list, filters, commit detail, file diff links. |
| Scene Explorer | Xem scene tree. | Scene list, node tree, node detail, search/filter, breadcrumb. |
| Asset Explorer | Xem asset và usage. | Asset grid/list, detail panel, usage, warnings. |
| Dependency Graph | Xem phụ thuộc. | Graph canvas, filter, legend, node detail, cycle highlight. |
| Health Report | Xem sức khỏe dự án. | Score, issue list, severity filter, trend/history. |
| Scene Diff Viewer | Review scene diff. | Revision selector, tree diff, property diff, summary. |
| Notification Center | Quản lý notification. | Notification list, unread filter, mark read/all. |
| Activity Log | Xem audit/activity. | Timeline/table, action filter, actor filter, correlation id. |
| Admin/User Management | Quản lý user. | User list, invite, status, role. |

## Role-based visibility

| UI capability | Owner/Admin | Developer | Reviewer/QA | Viewer |
| --- | :---: | :---: | :---: | :---: |
| Create/update/delete project | Có | Không | Không | Không |
| Manage members | Có | Không | Không | Không |
| Configure repository | Có | Không | Không | Không |
| Git commit/push/pull/merge | Có | Có | Không | Không |
| Trigger parse/analyze | Có | Có | Không | Không |
| View scene/asset/graph/health/diff | Có | Có | Có | Có |
| View activity | Có | Có | Có | Có |
| Admin user management | System Admin only | Không | Không | Không |

Lưu ý: role-based visibility chỉ giúp UX. Backend vẫn phải enforce permission.

## Empty state

| Trạng thái | Hiển thị |
| --- | --- |
| Chưa có project | Hướng dẫn tạo project nếu user có quyền. |
| Project chưa có repository | CTA connect repository cho Project Admin; thông báo readonly cho Viewer. |
| Repository đang clone | Job progress, trạng thái queue/running và link job detail. |
| Chưa parse metadata | CTA parse/analyze cho Developer+; thông báo readonly cho Viewer. |
| Graph chưa có dữ liệu | Giải thích cần analyze và hiển thị nút analyze nếu có quyền. |
| Chưa có notification/activity | Empty list ngắn gọn, không hiển thị lỗi. |

## Loading state

- API list/table dùng skeleton hoặc spinner nhẹ.
- Job dài hiển thị progress, status và thời điểm bắt đầu.
- Scene/graph lớn có loading riêng cho canvas/tree.
- Loading không được che mất error state đã biết.

## Error state

Error message phải trả lời:

1. Điều gì xảy ra.
2. User có thể làm gì.
3. Correlation id để báo lỗi.

Không hiển thị stack trace, server path hoặc credential.

## Confirm dialog cho thao tác nguy hiểm

| Thao tác | Xác nhận |
| --- | --- |
| Delete project | Bắt buộc confirm rõ tên project. |
| Disconnect repository | Bắt buộc xác nhận tác động tới workspace/metadata. |
| Update Git credential | Xác nhận thay thế credential cũ. |
| Push | Xác nhận branch/remote nếu có commit pending. |
| Pull/Merge | Cảnh báo conflict có thể xảy ra. |
| Delete branch | Cảnh báo không thể xóa branch đang checkout. |
| Cancel job | Xác nhận job có thể dừng ở trạng thái partial. |

## Accessibility

- Form và table hỗ trợ keyboard navigation.
- Màu không phải tín hiệu duy nhất cho trạng thái.
- Error đặt gần field liên quan.
- Icon quan trọng có label/tooltip.
- Contrast đủ đọc cho dashboard, graph và diff.

## Quyết định MVP

- UI MVP ưu tiên tiếng Việt để khớp tài liệu nghiệp vụ hiện tại. API vẫn phải trả error code ổn định để frontend có thể localize message về sau.
