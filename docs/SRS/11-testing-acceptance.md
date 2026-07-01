# 11. Testing & Acceptance

## Test strategy

GodForge áp dụng test strategy theo risk-based testing: các luồng bảo mật, RBAC, Git operation, parser/analyzer worker và data integrity được ưu tiên cao hơn UI phụ trợ.

## Test levels

| Loại test | Phạm vi | Ví dụ |
| --- | --- | --- |
| Unit test | Domain logic, validators, permission checker, parser/analyzer rule thuần. | Password policy, project name validation, health score formula. |
| Integration test | API + database, Redis lock, RabbitMQ message, MinIO artifact, Git repository giả lập. | Connect repository tạo job, parse ghi metadata, lock Git operation. |
| API test | Contract `/api/v1`, response/error/pagination/job format. | Login error format, project pagination, async job response. |
| Worker test | Parser, Analyzer, Diff, Preview worker lifecycle. | Retry transient DB error, DLQ poison message, idempotent parse. |
| Security test | Auth, RBAC, input validation, secret leakage, path traversal, SSRF. | Cross-project IDOR, malicious repo URL, path `../`. |
| Performance test | Dashboard, search, parse/analyze, scene diff, queue lag. | Dashboard p95, parse 100 scenes, diff scene vừa. |
| E2E test | Workflow chính qua UI/API. | Create project -> connect repo -> analyze -> view dashboard. |
| UAT | Nghiệm thu theo vai trò. | Reviewer xem diff; Developer commit/push; Viewer readonly. |

## Test case format chuẩn

| Trường | Mô tả |
| --- | --- |
| ID | Mã test case, ví dụ `TC-AUTH-001`. |
| Requirement | Requirement ID được kiểm thử. |
| Preconditions | Dữ liệu/trạng thái cần có trước test. |
| Steps | Các bước thực hiện. |
| Expected result | Kết quả mong đợi. |
| Priority | High/Medium/Low. |

## Test cases tối thiểu

| ID | Requirement | Preconditions | Steps | Expected result | Priority |
| --- | --- | --- | --- | --- | --- |
| TC-AUTH-001 | FR-01 | User active tồn tại. | Login đúng, refresh token, logout. | Nhận token, refresh rotation hoạt động, logout revoke token. | High |
| TC-AUTH-002 | FR-02 | Project có owner/admin. | Invite member, đổi role, thử xóa owner cuối. | Role cập nhật đúng; owner cuối không bị xóa. | High |
| TC-PROJ-001 | FR-03 | User authenticated. | Create/update/delete/restore project. | CRUD đúng quyền, soft-delete và restore đúng. | High |
| TC-REPO-001 | FR-04 | Project chưa có repo. | Connect repository URL/PAT hợp lệ. | Credential mã hóa, clone job tạo, activity ghi. | High |
| TC-GIT-001 | FR-05 | Repo ready có file thay đổi. | Status, stage, commit, push. | Git state đúng, lock hoạt động, activity ghi. | High |
| TC-GIT-002 | FR-06 | Repo có commit history. | Xem list/detail/filter. | Commit list/detail đúng, phân trang đúng. | Medium |
| TC-GIT-003 | FR-07 | Repo ready. | Tạo branch, checkout dirty tree, merge. | Branch tạo được, checkout dirty bị chặn, merge theo quyền. | Medium |
| TC-PARSE-001 | FR-08 | Repo Godot fixture. | Trigger parse. | Metadata ghi đúng, file corrupt không crash job. | High |
| TC-SCENE-001 | FR-09 | Metadata scene đã parse. | Mở scene, click node, search. | Tree/detail/search đúng. | Medium |
| TC-ASSET-001 | FR-10 | Metadata asset đã parse. | Mở asset, xem usage. | Detail/usage/warning đúng. | Medium |
| TC-GRAPH-001 | FR-11 | Analyze hoàn thành. | Mở graph, filter, focus node. | Nodes/edges/filter/cycle đúng. | Medium |
| TC-HEALTH-001 | FR-12 | Metadata có missing resource fixture. | Trigger analyze. | Report score/issues đúng, notification critical. | High |
| TC-DIFF-001 | FR-13 | Hai revision scene khác nhau. | Tạo scene diff. | Node/property diff đúng, cache hit nhanh. | High |
| TC-DASH-001 | FR-14 | Project có metadata/jobs/activity. | Mở dashboard. | Summary đúng, cache hit đạt target. | Medium |
| TC-SEARCH-001 | FR-15 | User có quyền project A, không có quyền project B. | Search keyword có ở cả hai project. | Chỉ trả result project A. | High |
| TC-NOTIF-001 | FR-16 | Job hoàn thành. | Xem notification, mark read. | Notification realtime/persisted, unread count giảm. | Medium |
| TC-SETTINGS-001 | FR-17 | Project Admin và Developer. | Admin đổi setting; Developer thử đổi setting. | Admin thành công, Developer 403. | Medium |
| TC-ACTIVITY-001 | FR-18 | Project có activity. | Xem activity theo project và system admin view. | Project member thấy log đúng scope; system admin thấy toàn cục. | High |
| TC-SEC-001 | Security NFR | Có user nhiều project. | Thử cross-project IDOR, path traversal, malicious repo URL. | Request bị reject/forbidden, không leak dữ liệu. | High |
| TC-PERF-001 | Performance NFR | Dataset đại diện. | Đo dashboard/search/parse/diff. | Đạt mục tiêu NFR hoặc ghi rõ deviation. | Medium |

## UAT checklist

- System Admin quản lý user và xem system activity.
- Project Admin tạo project, mời member, kết nối repository và cấu hình settings.
- Developer thao tác Git, trigger parse/analyze và xem metadata.
- Reviewer/QA xem commit history, scene diff và health report.
- Viewer xem readonly dashboard, scene, asset, graph và activity được phép.
- Notification và activity log phản ánh đúng các workflow chính.

## Definition of Done

- Requirement `Must` có implementation và test tương ứng hoặc exception được phê duyệt.
- API response/error format đúng `05-api.md`.
- RBAC được kiểm thử với ít nhất Viewer, Developer, Project Admin và System Admin.
- Không có secret trong log, DB plaintext hoặc API response.
- Worker job có retry/DLQ/idempotency theo `12-worker-processing.md`.
- Docs SRS, traceability và testing được cập nhật khi requirement đổi.

## Sign-off criteria

- Không còn bug critical/high chưa xử lý.
- Security tests trọng yếu đạt.
- Performance mục tiêu chính đạt hoặc có biên bản chấp nhận deviation.
- UAT checklist được xác nhận bởi stakeholder phù hợp.
- Release checklist vận hành, backup/restore và monitoring hoàn tất.
