# 5. Thiết kế API

## 5.1 Tiêu chuẩn API
Hệ thống sử dụng chuẩn RESTful API, triển khai bằng ASP.NET Core .NET 9.
- **Stateless:** API không lưu session, xác thực qua JWT.
- **Routing:** `/api/v1/[resource]`
- **Request/Response Format:** `application/json`

## 5.2 Lớp API Gateway / Controller
Chịu trách nhiệm:
- Nhận request.
- Xác thực Token (Auth).
- Kiểm tra quyền (RBAC).
- Validate Input Payload.
- Gọi Use Case / Service layer (CQRS).
- Chuẩn hóa Response & Error.

*Lưu ý:* **Không truy cập trực tiếp Database từ Controller.**

## 5.3 Xử lý tác vụ nặng (Async Pattern)
Các endpoint kích hoạt thao tác Git, Parse, Analyze, Diff sẽ **KHÔNG** chờ kết quả ngay.
- Trả về HTTP 202 Accepted.
- Kèm theo `jobId`.
- Frontend dùng `jobId` để poll trạng thái hoặc nhận update qua WebSocket (SignalR).

## 5.4 Phản hồi chuẩn (Standard Response)
**Thành công (200 OK / 201 Created):**
```json
{
  "data": { ... },
  "meta": { "pagination": ... }
}
```

**Lỗi (4xx / 5xx):**
```json
{
  "error": {
    "code": "PROJECT_NOT_FOUND",
    "message": "Project không tồn tại hoặc bạn không có quyền truy cập.",
    "correlationId": "abc-123",
    "details": [...]
  }
}
```
