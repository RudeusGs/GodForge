---
name: debug-issue
description: Skill for systematically debugging and fixing issues in the GodForge backend. Covers reading logs, tracing correlation IDs, identifying root causes in the correct architectural layer, and writing regression tests.
---

# Debug Issue

Skill hướng dẫn quy trình debug và fix bug trong GodForge Backend.

## Quy trình debug

### 1. Thu thập thông tin
- Đọc error message và correlation ID.
- Xác định layer nào gây lỗi: Domain, Application, Infrastructure, API.
- Kiểm tra logs (Serilog structured logs) nếu có.

### 2. Xác định phạm vi ảnh hưởng
- Bug ở Domain logic → fix trong `GodForge.Domain`.
- Bug ở business flow → fix trong `GodForge.Application`.
- Bug ở DB query, external service → fix trong `GodForge.Infrastructure`.
- Bug ở request/response mapping → fix trong `GodForge.Api`.

### 3. Kiểm tra trước khi sửa
- Đọc lại SRS functional requirement liên quan (acceptance criteria).
- Xem có unit test nào đang cover case này không.
- Nếu có test đang pass sai → fix test trước.

### 4. Fix bug
- Sửa ở đúng layer (KHÔNG sửa ở layer sai cho tiện).
- Thêm hoặc cập nhật unit test để cover bug đã fix.
- Đảm bảo fix không break acceptance criteria khác.

### 5. Verify
- Chạy `dotnet build` — không có warning.
- Chạy `dotnet test` — tất cả tests pass.
- Test thủ công với API nếu cần.

### 6. Commit
- Dùng type `fix(scope): mô tả ngắn gọn`.
- Ví dụ: `fix(auth): prevent token reuse after refresh rotation`

## Lưu ý quan trọng
- KHÔNG fix bug bằng cách thêm try-catch để swallow exception.
- KHÔNG fix bug ở Controller nếu root cause ở Application/Domain.
- KHÔNG disable warning để "fix" compiler error.
- KHÔNG sửa test để test pass thay vì sửa code.
