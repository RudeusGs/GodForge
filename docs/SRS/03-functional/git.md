# Git / Repository

## Mục tiêu

Module Git giúp project kết nối repository Git, thực hiện các thao tác Git phổ biến qua giao diện web và đảm bảo thao tác nguy hiểm được khóa, audit và xử lý lỗi an toàn.

## Tác nhân

- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- Kết nối repository HTTPS bằng Personal Access Token hoặc credential tương đương.
- Clone/fetch repository vào server-side workspace.
- Git status, stage, unstage, commit, push, pull.
- Branch list, create, checkout, delete, merge.
- Commit history và commit detail.
- Conflict handling không tự động resolve.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-04 | Kết nối repository | Kết nối project với repository Git qua HTTPS URL và credential được mã hóa. | Must | Project Admin |
| FR-05 | Git UI | Cung cấp status, stage, unstage, commit, push, pull và conflict visibility. | Must | Developer |
| FR-06 | Lịch sử commit | Hiển thị commit list, commit detail, file diff và link scene diff cho `.tscn`. | Must | Viewer+ |
| FR-07 | Quản lý branch | Tạo, chuyển, xóa và merge branch theo quyền. | Should | Developer, Project Admin |
| BR-27 | Một repo mỗi project | Mỗi project chỉ có một repository active trong MVP. | Must | System |
| BR-31 | Repository lock | Thao tác mutate Git phải acquire distributed lock. | Must | System |
| BR-34 | Commit message | Commit message bắt buộc và tối thiểu 5 ký tự. | Must | Developer |

## Luồng xử lý chính

### Kết nối repository

1. Project Admin nhập HTTPS remote URL và PAT.
2. Backend validate URL, kiểm tra project chưa có repository active và mã hóa credential.
3. Hệ thống lưu `repositories` với `credential_ref`.
4. API tạo job clone và trả `202 Accepted`.
5. Clone Worker acquire lock, clone/fetch workspace và cập nhật trạng thái repository/job.
6. Nếu clone thành công và `auto_parse_on_push = true`, hệ thống trigger parse/analyze ban đầu.
7. Hệ thống ghi activity `repo.connected`.

### Commit và push

1. Developer mở Git status.
2. User chọn file để stage hoặc stage all.
3. User nhập commit message hợp lệ.
4. Backend kiểm tra có staged files, workspace không conflict và actor có quyền.
5. Git Service acquire lock khi mutate workspace, tạo commit và ghi activity.
6. User chọn push; hệ thống acquire lock, push remote và xử lý non-fast-forward nếu có.

### Pull/merge conflict

1. User chọn pull hoặc merge.
2. Backend kiểm tra quyền, repository state và acquire lock.
3. Git operation chạy trong workspace.
4. Nếu conflict, hệ thống lưu/hiển thị danh sách conflict files và không tự resolve.
5. User xử lý conflict theo UI; sau đó stage và commit merge.

### Commit history

1. User mở commit history theo branch/filter.
2. Backend đọc Git log từ workspace, phân trang và trả commit summary.
3. Khi mở commit detail, hệ thống trả file changed và diff.
4. File `.tscn` có link sang Scene Diff Viewer.

## Ngoại lệ / lỗi

| Tình huống | HTTP Status / Job State | Error Code | Hành vi |
| --- | --- | --- | --- |
| URL repository sai | 400 | `INVALID_REPO_URL` | Không lưu credential. |
| Project đã có repo | 409 | `REPO_ALREADY_CONNECTED` | Không tạo repo mới. |
| Credential Git sai | Job failed / 401 | `CLONE_AUTH_FAILED` | Không log PAT; yêu cầu cập nhật credential. |
| Network clone/fetch lỗi | Job retry/failed | `CLONE_NETWORK_ERROR` | Retry theo policy. |
| Repo lớn hơn 2 GB | Job failed | `REPO_SIZE_EXCEEDED` | Dừng clone và thông báo. |
| Repository đang lock | 423 | `REPO_LOCKED` | Trả thời điểm lock hết hạn nếu an toàn. |
| Commit không có staged file | 400 | `NO_STAGED_FILES` | Không tạo commit. |
| Push non-fast-forward | 409 | `NON_FAST_FORWARD` | Yêu cầu pull trước. |
| Pull/merge conflict | 409 | `GIT_CONFLICT` | Hiển thị conflict files, không auto-resolve. |

## Acceptance Criteria

- AC-34: URL + PAT hợp lệ tạo clone job và trả 202.
- AC-35: Clone thành công tạo workspace và repository status `ready`.
- AC-36: Clone thất bại lưu job status `failed` và error code rõ ràng.
- AC-37: PAT được mã hóa, không đọc được plaintext trong DB/log/API response.
- AC-38: Kết nối repo khi project đã có repo trả 409.
- AC-39: Git status hiển thị đúng changed/staged/unstaged/untracked files.
- AC-40: Stage/unstage file cập nhật trạng thái đúng.
- AC-41: Commit với message hợp lệ thành công và working tree cập nhật.
- AC-42: Push thành công cập nhật remote.
- AC-43: Push khi repo đang lock trả 423.
- AC-44: Pull có conflict hiển thị conflict files và không auto-resolve.
- AC-45: Commit history hiển thị hash, author, date, message.
- AC-46: Commit detail hiển thị file changed và diff.
- AC-47: Filter branch chỉ trả commit thuộc branch đó.
- AC-48: File `.tscn` changed có link sang Scene Diff Viewer.
- AC-49: Tạo branch làm branch xuất hiện trong danh sách.
- AC-50: Checkout khi dirty working tree bị chặn và cảnh báo.
- AC-51: Merge thành công tạo merge commit.
- AC-52: Viewer cố merge nhận 403.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{id}/repository` | `project_admin+` | remoteUrl, personalAccessToken | Clone job | `INVALID_REPO_URL`, `REPO_ALREADY_CONNECTED` |
| GET | `/api/v1/projects/{id}/repository` | Project member | none | Repository detail | `REPO_NOT_CONNECTED` |
| PUT | `/api/v1/projects/{id}/repository/credential` | `project_admin+` | personalAccessToken | Credential updated | `FORBIDDEN` |
| DELETE | `/api/v1/projects/{id}/repository` | `project_admin+` | confirmation | Repository disconnected | `REPO_LOCKED` |
| POST | `/api/v1/projects/{id}/repository/sync` | `developer+` | branch/options | Fetch job/result | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{id}/git/status` | `developer+` | branch | Git status | `REPO_NOT_READY` |
| POST | `/api/v1/projects/{id}/git/stage` | `developer+` | file paths | Stage result | `PATH_NOT_ALLOWED` |
| POST | `/api/v1/projects/{id}/git/unstage` | `developer+` | file paths | Unstage result | `PATH_NOT_ALLOWED` |
| POST | `/api/v1/projects/{id}/git/commit` | `developer+` | message | Commit result | `NO_STAGED_FILES` |
| POST | `/api/v1/projects/{id}/git/push` | `developer+` | branch/remote | Push result | `NON_FAST_FORWARD`, `REPO_LOCKED` |
| POST | `/api/v1/projects/{id}/git/pull` | `developer+` | branch | Pull result | `GIT_CONFLICT`, `REPO_LOCKED` |
| GET | `/api/v1/projects/{id}/git/branches` | `viewer+` | none | Branch list | `REPO_NOT_READY` |
| POST | `/api/v1/projects/{id}/git/branches` | `developer+` | name, source | Branch created | `BRANCH_EXISTS` |
| POST | `/api/v1/projects/{id}/git/branches/checkout` | `developer+` | branch | Checkout result | `DIRTY_WORKTREE` |
| DELETE | `/api/v1/projects/{id}/git/branches/{name}` | `project_admin+` | branch name | Branch deleted | `CURRENT_BRANCH_DELETE_DENIED` |
| POST | `/api/v1/projects/{id}/git/merge` | `developer+` | source, target | Merge result | `GIT_CONFLICT` |
| GET | `/api/v1/projects/{id}/git/commits` | `viewer+` | pagination/filter | Commit list | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{id}/git/commits/{hash}` | `viewer+` | hash | Commit detail | `COMMIT_NOT_FOUND` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `repositories` | Remote URL, credential reference, default branch, workspace path, clone status. |
| `jobs` | Clone/fetch/sync jobs, progress, status, errors và correlation id. |
| `activities` | `repo.connected`, `repo.disconnected`, `git.commit`, `git.push`, `git.pull`, `git.merge`. |
| `project_settings` | Auto parse/analyze policy sau push/clone. |
| `notifications` | Job result, conflict hoặc credential issue notification. |

## Ghi chú bảo mật / phân quyền

- Chỉ hỗ trợ HTTPS repository trong MVP.
- Credential phải được mã hóa bằng AES-256-GCM hoặc cơ chế tương đương.
- Không truyền raw user input trực tiếp vào shell command; Git arguments phải được validate và truyền bằng structured API.
- File path từ user phải normalize và kiểm tra không vượt workspace.
- Không expose server internal path, remote credential, stack trace hoặc raw Git stderr chứa secret.
