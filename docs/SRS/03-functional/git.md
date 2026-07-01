# Functional Requirements: Git Integration

## FR-04: Kết nối Repository
**Mức ưu tiên:** Must  
**Mô tả:** Kết nối dự án với repository Git qua URL và credential.

### Luồng chính
1. Project Admin nhập Git remote URL (HTTPS) và Personal Access Token (PAT).
2. Hệ thống validate URL format, mã hóa PAT (AES-256-GCM), lưu `credential_ref` vào DB.
3. Tạo **async job** (type: `repo.clone`) để clone repository vào workspace trên server.
4. Trả về HTTP 202 + `jobId`.
5. Worker clone repo, cập nhật job status.
6. Sau clone thành công → tự động trigger job `parse` (nếu setting `auto_parse_on_push = true`).
7. Ghi Activity Log: `repo.connected`.

### Luồng phụ — Cập nhật credential
1. Project Admin cập nhật PAT mới.
2. Mã hóa và thay thế credential cũ.
3. Ghi Activity Log: `repo.credential_updated`.

### Luồng phụ — Ngắt kết nối
1. Project Admin ngắt kết nối repository.
2. Hệ thống xóa workspace trên server (async job).
3. Metadata của repo vẫn giữ trong DB (có thể xóa thủ công).
4. Ghi Activity Log: `repo.disconnected`.

### Xử lý lỗi
| Tình huống | HTTP Status | Error Code |
|------------|-------------|------------|
| URL format sai | 400 | `INVALID_REPO_URL` |
| Clone thất bại (auth fail) | Job failed | `CLONE_AUTH_FAILED` |
| Clone thất bại (network) | Job failed | `CLONE_NETWORK_ERROR` |
| Repo quá lớn (> 2GB) | Job failed | `REPO_SIZE_EXCEEDED` |
| Project đã có repo | 409 | `REPO_ALREADY_CONNECTED` |

### Business Rules
- **BR-27:** Mỗi project chỉ được kết nối 1 repository.
- **BR-28:** Credential (PAT) phải mã hóa bằng AES-256-GCM trước khi lưu DB.
- **BR-29:** Chỉ hỗ trợ HTTPS URL (không hỗ trợ SSH cho MVP).
- **BR-30:** Repo size limit: 2GB. Cảnh báo nếu > 1GB.

### Acceptance Criteria
- [x] AC-34: Nhập URL + PAT hợp lệ → job clone được tạo, trả 202.
- [x] AC-35: Clone thành công → repo workspace có trên server, status = success.
- [x] AC-36: Clone thất bại → job status = failed, error message rõ ràng.
- [x] AC-37: PAT được mã hóa trong DB, không thể đọc plaintext.
- [x] AC-38: Kết nối repo khi project đã có repo → 409.

---

## FR-05: Giao diện Git UI
**Mức ưu tiên:** Must  
**Mô tả:** Cung cấp giao diện cho các thao tác Git cơ bản.

### Thao tác hỗ trợ

#### Status
- Hiển thị danh sách file: modified, added, deleted, untracked.
- Phân biệt staged vs unstaged.
- Hiển thị branch hiện tại, ahead/behind remote.

#### Stage / Unstage
- Stage file cụ thể hoặc stage all.
- Unstage file cụ thể hoặc unstage all.

#### Commit
1. User nhập commit message (bắt buộc, ≥ 5 ký tự).
2. Hệ thống kiểm tra có file staged.
3. Thực hiện commit trên server workspace.
4. Ghi Activity Log: `git.commit`.

#### Push
1. User trigger push.
2. Hệ thống acquire **distributed lock** (Redis, TTL: 5 phút).
3. Push lên remote. Nếu reject (non-fast-forward) → thông báo cần pull trước.
4. Release lock.
5. Ghi Activity Log: `git.push`.

#### Pull
1. User trigger pull.
2. Hệ thống acquire distributed lock.
3. Pull từ remote. Nếu có conflict → đánh dấu conflict, KHÔNG auto-resolve.
4. Release lock.
5. Ghi Activity Log: `git.pull`.

### Xử lý xung đột (Conflict)
- Khi pull/merge gây conflict:
  - Hiển thị danh sách file conflict.
  - Cho phép user chọn: accept theirs / accept ours / manual resolve.
  - Manual resolve: user tải file, sửa, upload lại.
  - Sau khi resolve → stage và commit merge.

### Business Rules
- **BR-31:** Mọi thao tác thay đổi state (commit, push, pull, merge) phải acquire distributed lock.
- **BR-32:** Lock TTL: 5 phút. Nếu quá hạn → tự động release (tránh deadlock).
- **BR-33:** Không cho phép push nếu có conflict chưa resolve.
- **BR-34:** Commit message bắt buộc, tối thiểu 5 ký tự.

### Acceptance Criteria
- [x] AC-39: Xem status → hiển thị đúng danh sách file changed, staged, untracked.
- [x] AC-40: Stage file → file chuyển từ unstaged sang staged.
- [x] AC-41: Commit với message → commit thành công, file trở về clean.
- [x] AC-42: Push thành công → remote cập nhật.
- [x] AC-43: Push khi có user khác đang lock → 423 Locked, thông báo chờ.
- [x] AC-44: Pull có conflict → hiển thị danh sách conflict file, không auto-resolve.

---

## FR-06: Lịch sử Commit
**Mức ưu tiên:** Must  
**Mô tả:** Hiển thị commit history của repository.

### Chi tiết hiển thị
- **Commit list:** Hash (short), author, date, message. Phân trang (20 commits/page).
- **Commit detail:** Full message, danh sách file changed (added/modified/deleted), diff cho từng file.
- **Filter:** Theo branch, author, date range.
- **Liên kết:** Nếu file changed là `.tscn` → link sang Scene Diff Viewer (FR-13).

### Business Rules
- **BR-35:** Commit history đọc từ Git repo trên server, không lưu DB (trừ cache ngắn hạn).
- **BR-36:** Chỉ user có quyền ≥ viewer mới xem được commit history.

### Acceptance Criteria
- [x] AC-45: Mở commit history → hiển thị danh sách commit với hash, author, date, message.
- [x] AC-46: Click commit → xem danh sách file changed và diff.
- [x] AC-47: Filter theo branch → chỉ hiển thị commit của branch đó.
- [x] AC-48: File `.tscn` changed → có link sang Scene Diff Viewer.

---

## FR-07: Quản lý Branch
**Mức ưu tiên:** Should  
**Mô tả:** Tạo, chuyển, xóa branch và merge branch.

### Thao tác hỗ trợ

#### Xem danh sách branch
- Local branches và remote branches.
- Hiển thị branch hiện tại, last commit, ahead/behind.

#### Tạo branch
1. User nhập tên branch mới, chọn source branch.
2. Validate tên: chỉ `[a-zA-Z0-9/_-]`, không trùng existing branch.
3. Tạo branch.

#### Checkout (Chuyển branch)
1. User chọn branch muốn chuyển.
2. Hệ thống kiểm tra working tree.
3. Nếu có uncommitted changes → **cảnh báo** và yêu cầu commit/stash trước.
4. Nếu clean → checkout.

#### Xóa branch
- Xóa local branch: cho phép nếu không phải branch hiện tại.
- Xóa remote branch: chỉ Project Admin+.

#### Merge
1. User chọn source branch merge vào target branch.
2. Hệ thống acquire lock, thực hiện merge.
3. Nếu conflict → xử lý như FR-05.
4. Ghi Activity Log: `git.merge`.

### Business Rules
- **BR-37:** Không cho phép xóa branch đang checkout.
- **BR-38:** Checkout khi có dirty working tree → block, cảnh báo.
- **BR-39:** Merge yêu cầu quyền ≥ developer.
- **BR-40:** Xóa remote branch yêu cầu quyền ≥ project_admin.

### Acceptance Criteria
- [x] AC-49: Tạo branch → branch mới xuất hiện trong danh sách.
- [x] AC-50: Checkout khi có uncommitted changes → cảnh báo, không cho checkout.
- [x] AC-51: Merge thành công → commit merge xuất hiện, branch source vẫn tồn tại.
- [x] AC-52: Viewer cố merge → 403 Forbidden.
