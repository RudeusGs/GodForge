# GodForge Backend (GodForge-BE)

**GodForge** là nền tảng quản trị dành cho doanh nghiệp để quản lý, phân tích và trực quan hóa các dự án Godot. Repository này chứa mã nguồn Backend được xây dựng trên nền tảng **ASP.NET Core (.NET 9)**.

## 🏗 Kiến trúc (Architecture)

Backend tuân thủ nghiêm ngặt các nguyên tắc **Clean Architecture**, **Vertical Slice Architecture**, và **Domain-Driven Design (DDD)**. Hệ thống được thiết kế theo mô hình **Modular Monolith** kết hợp với xử lý bất đồng bộ ngầm.

### Công nghệ sử dụng (Tech Stack)
- **Framework:** ASP.NET Core (.NET 9)
- **Cơ sở dữ liệu:** PostgreSQL (Lưu trữ Core & Metadata)
- **Cache & Khóa (Lock):** Redis
- **Message Broker:** RabbitMQ (Ưu tiên xử lý bất đồng bộ cho các tác vụ nặng như Parse, Analyze, Diff)
- **Lưu trữ file:** MinIO
- **Realtime:** SignalR
- **Observability (Giám sát):** Prometheus, Grafana, Loki, Serilog

### Cấu trúc dự án
```text
GodForge-BE/
├── src/                # Mã nguồn dự án (API, Application, Domain, Infrastructure, Worker)
├── tests/              # Mã nguồn kiểm thử (Unit, Integration, và E2E Tests)
├── docs/               # Tài liệu Đặc tả hệ thống (SRS) và Kiến trúc
├── docker-compose.yml  # File cấu hình hạ tầng môi trường dev (Local infrastructure)
└── scaffold.ps1        # Script PowerShell hỗ trợ tự động sinh dự án .NET
```

---

## 🚀 Hướng dẫn cài đặt (Getting Started)

### 1. Yêu cầu hệ thống (Prerequisites)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js](https://nodejs.org/) (Dùng để chạy Git hooks & Commitlint)

### 2. Thiết lập hạ tầng
Để khởi động các dịch vụ cơ sở dữ liệu và message broker cần thiết tại máy cá nhân:
```bash
# Khởi động PostgreSQL, Redis, RabbitMQ, và MinIO
docker-compose up -d

# (Tùy chọn) Khởi động kèm các công cụ giám sát (Prometheus, Grafana, Loki)
docker-compose --profile monitoring up -d
```

### 3. Khởi tạo mã nguồn dự án (Project Scaffolding)
Nếu các project C# chưa được tạo, bạn có thể chạy script sau để tự động sinh toàn bộ cấu trúc Modular Monolith:
```powershell
.\scaffold.ps1
```

### 4. Thiết lập Git Hooks & Commitlint (Husky)
Dự án này bắt buộc tuân thủ chuẩn **Conventional Commits**. Bạn phải cài đặt các dependency của Node để bật Git hooks (Pre-commit, Commit-msg):
```bash
npm install -D husky lint-staged @commitlint/cli @commitlint/config-conventional
npm run prepare
```
Đảm bảo mọi nội dung commit đều tuân thủ cấu trúc:
`type(scope): subject` (Ví dụ: `feat(api): add auth endpoints`).

---

## 📖 Tài liệu dự án (Documentation)
Toàn bộ tài liệu chính thức về kiến trúc, chức năng và thiết kế cơ sở dữ liệu đều nằm trong thư mục `docs/` ở gốc dự án. **Vui lòng đọc kỹ tài liệu trước khi thực hiện các thay đổi về cấu trúc hệ thống.**

## 🤝 Nguyên tắc phát triển (Development Principles)
- **Async-first (Ưu tiên bất đồng bộ):** Không bao giờ khóa (block) API Controllers bằng các luồng xử lý nặng. Luôn đẩy các tác vụ này vào RabbitMQ để Worker xử lý.
- **Không truy cập trực tiếp DB từ API:** Mọi logic nghiệp vụ và truy vấn phải đi qua tầng Application (Use Cases/CQRS).
- **Mở rộng & Hiệu năng:** Đảm bảo mã nguồn dễ đọc, dễ bảo trì, dễ viết test và có thể nâng cấp mượt mà trong nhiều năm tới.
