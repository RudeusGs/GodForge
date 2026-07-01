# 8. Workflows

## Mục tiêu

Tài liệu này mô tả các workflow nghiệp vụ chính của GodForge để thống nhất hành vi giữa UI, API, worker, database, notification và activity log.

## WF-01 Login / Refresh Session

| Bước | Mô tả |
| --- | --- |
| 1 | User nhập email/password tại màn hình Login. |
| 2 | API validate input, rate limit và account status. |
| 3 | Auth Service kiểm tra bcrypt password hash. |
| 4 | Nếu hợp lệ, API phát access token và refresh token. |
| 5 | Refresh token được lưu dạng hash trong `refresh_tokens`. |
| 6 | API ghi `user.login.success`; lỗi ghi `user.login.failed`. |
| 7 | Khi access token hết hạn, client gọi refresh để lấy token pair mới. |
| 8 | Refresh token cũ bị revoke theo rotation policy. |

Điểm kiểm soát: không tiết lộ user tồn tại, không log token/password, có correlation id.

## WF-02 Create Project

| Bước | Mô tả |
| --- | --- |
| 1 | User chọn tạo project. |
| 2 | Nhập name, description, Godot version và visibility. |
| 3 | API validate tên unique, role và input. |
| 4 | Project Service tạo `projects`, `project_settings` và membership `project_owner`. |
| 5 | Activity Log ghi `project.created`. |
| 6 | UI chuyển đến Project Dashboard với empty state yêu cầu kết nối repository. |

Điểm kiểm soát: creator luôn là owner; project mới chưa có repository/metadata.

## WF-03 Connect Repository

| Bước | Mô tả |
| --- | --- |
| 1 | Project Admin mở Repository Settings. |
| 2 | Nhập HTTPS remote URL, default branch và PAT. |
| 3 | API validate URL, quyền và project chưa có repository active. |
| 4 | Credential được mã hóa và lưu dưới dạng `credential_ref`. |
| 5 | API tạo clone job và trả 202. |
| 6 | Notification/Activity ghi `repo.connected` hoặc error nếu thất bại. |

Điểm kiểm soát: không lưu/log PAT plaintext; URL phải chống SSRF theo policy.

## WF-04 Initial Clone And Analyze

| Bước | Mô tả |
| --- | --- |
| 1 | Clone Worker nhận `RepositoryCloneRequested`. |
| 2 | Worker acquire repository lock. |
| 3 | Worker clone/fetch repository vào workspace. |
| 4 | Worker cập nhật `repositories.clone_status` và job progress. |
| 5 | Nếu clone thành công, hệ thống trigger parse/analyze ban đầu theo settings. |
| 6 | Parser Worker tạo metadata; Analyze Worker tạo health report. |
| 7 | Dashboard cache invalidated/rebuilt; user nhận notification. |

Điểm kiểm soát: clone lỗi auth không retry vô hạn; network lỗi có thể retry.

## WF-05 Git Commit And Push

| Bước | Mô tả |
| --- | --- |
| 1 | Developer mở Git UI và xem status. |
| 2 | Chọn files để stage. |
| 3 | Nhập commit message tối thiểu 5 ký tự. |
| 4 | API kiểm tra staged files và acquire repository lock. |
| 5 | Git Service tạo commit, release lock và ghi `git.commit`. |
| 6 | User chọn push. |
| 7 | API acquire lock, push remote và xử lý non-fast-forward nếu có. |
| 8 | Sau push thành công, hệ thống có thể trigger parse/analyze theo settings. |

Điểm kiểm soát: không push khi có conflict chưa resolve; lock TTL tránh deadlock.

## WF-06 Pull With Conflict

| Bước | Mô tả |
| --- | --- |
| 1 | Developer chọn pull. |
| 2 | API kiểm tra quyền, repository ready và acquire lock. |
| 3 | Git Service chạy pull. |
| 4 | Nếu conflict, API trả danh sách conflict files và trạng thái conflict. |
| 5 | UI cho user chọn accept ours/theirs hoặc manual resolve theo phạm vi Git UI. |
| 6 | Sau resolve, user stage và commit merge. |
| 7 | Activity ghi `git.pull` và `git.merge` nếu có. |

Điểm kiểm soát: GodForge không tự động resolve conflict.

## WF-07 Parse Project

| Bước | Mô tả |
| --- | --- |
| 1 | Developer trigger parse hoặc hệ thống trigger sau clone/push. |
| 2 | API tạo parse job. |
| 3 | Parser Worker lấy snapshot theo commit/branch. |
| 4 | Worker parse `.tscn`, `.tres`, `.res`, `.gd` và asset files. |
| 5 | Worker ghi metadata và progress. |
| 6 | Lỗi từng file được isolate nếu có thể. |
| 7 | Job hoàn tất gửi notification. |

Điểm kiểm soát: parser không execute script và không đọc ngoài workspace.

## WF-08 View Scene

| Bước | Mô tả |
| --- | --- |
| 1 | User mở Scene Explorer. |
| 2 | API trả danh sách scene theo permission. |
| 3 | User chọn scene. |
| 4 | API trả scene detail và node tree từ metadata. |
| 5 | UI hiển thị tree, node detail, search/filter và breadcrumb. |

Điểm kiểm soát: nếu chưa parse, UI hiển thị empty state và action parse cho Developer+.

## WF-09 View Asset Usage

| Bước | Mô tả |
| --- | --- |
| 1 | User mở Asset Explorer. |
| 2 | API trả asset list. |
| 3 | User chọn asset. |
| 4 | API trả asset metadata và usage từ `dependencies`. |
| 5 | UI hiển thị asset detail, thumbnail/fallback, usage và warning unused/missing nếu có. |

Điểm kiểm soát: unused asset có thể false positive với dynamic load.

## WF-10 Generate Dependency Graph

| Bước | Mô tả |
| --- | --- |
| 1 | User mở Dependency Graph. |
| 2 | API đọc `dependencies` và metadata nodes theo project. |
| 3 | UI render graph bằng layout client-side. |
| 4 | User filter theo type/depth/root. |
| 5 | Cyclic dependency được highlight và liên kết health issue nếu có. |

Điểm kiểm soát: giới hạn depth/response size để tránh tải quá lớn.

## WF-11 Run Health Analysis

| Bước | Mô tả |
| --- | --- |
| 1 | Developer trigger analyze. |
| 2 | API kiểm tra parse đã hoàn thành. |
| 3 | Analyze Worker đọc metadata và chạy health rules. |
| 4 | Worker lưu `health_reports` và `health_issues`. |
| 5 | Redis dashboard cache được cập nhật/invalidate. |
| 6 | Critical issue tạo notification. |

Điểm kiểm soát: health report lưu history, không ghi đè report cũ.

## WF-12 View Scene Diff

| Bước | Mô tả |
| --- | --- |
| 1 | Reviewer chọn scene và hai revision. |
| 2 | API validate path/revision và kiểm tra diff cache. |
| 3 | Nếu cache hit, API trả diff result. |
| 4 | Nếu cache miss, API tạo diff job. |
| 5 | Diff Worker parse hai version và tạo node/property diff. |
| 6 | UI hiển thị added/removed/modified/moved nodes và summary. |

Điểm kiểm soát: scene-aware diff chỉ áp dụng `.tscn`; file khác fallback text diff.

## WF-13 Notification And Activity Log

| Bước | Mô tả |
| --- | --- |
| 1 | Domain event hoặc worker result phát sinh. |
| 2 | Notification Service xác định recipient theo membership/settings. |
| 3 | Activity Log ghi action nếu là sự kiện audit. |
| 4 | Notification lưu DB và gửi SignalR nếu user online. |
| 5 | User mở Notification Center hoặc Activity Log để xem lịch sử. |

Điểm kiểm soát: metadata log phải sanitize và không chứa credential/token.

## Quyết định MVP

- Manual conflict resolve trong MVP xử lý ngoài GodForge: user resolve trong local/Git tool khác rồi refresh repository status trong GodForge. Upload file đã sửa qua UI là hướng mở rộng sau MVP.
