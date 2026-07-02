# GodForge

GodForge là một nền tảng Web phục vụ công tác quản lý, phân tích và trực quan hóa các dự án Godot Engine. Hệ thống cung cấp các bộ công cụ chuyên sâu bao gồm Git UI, Scene Diff Viewer, Dependency Graph, Project Health Dashboard và hệ thống trích xuất Metadata tự động. 

Dự án này được phát triển dưới hình thức **Đồ án tốt nghiệp**, áp dụng kiến trúc phần mềm hiện đại (Clean Architecture) và cơ chế xử lý luồng công việc bất đồng bộ (Asynchronous Processing) nhằm đáp ứng hiệu năng cao.

## Tính năng cốt lõi (Core Features)

- **Quản lý dự án tập trung (Centralized Project Management):** Quản trị danh mục dự án Godot, tích hợp trực tiếp với Git repositories, phân quyền truy cập và vai trò thành viên dựa trên Role-Based Access Control (RBAC).
- **Trực quan hóa cấu trúc dự án (Project Structure Visibility):** Tự động phân tích (parse) các tệp tin đặc thù của Godot (`.tscn`, `.tres`, `.gd`) cùng dữ liệu tài nguyên (assets) nhằm tái tạo Cây cấu trúc cảnh (Scene Explorer), Asset Explorer và Biểu đồ phụ thuộc (Dependency Graph).
- **Kiểm duyệt thay đổi thông minh (Advanced Change Review):** Cung cấp giao diện thao tác Git tích hợp (Stage, Commit, Push, Pull, Merge) kết hợp **Scene Diff Viewer** - công cụ đối chiếu thay đổi ở cấp độ node và thuộc tính (property), khắc phục hạn chế của text diff truyền thống.
- **Giám sát sức khỏe dự án (Project Health Monitoring):** Hệ thống tự động rà soát, phát hiện các tài nguyên thất lạc (missing resources), mã nguồn lỗi, liên kết gãy (broken references), phụ thuộc vòng (cyclic dependencies), tài nguyên rác (unused assets) và cảnh (scene) vượt ngưỡng kích thước cho phép.
- **Kiểm toán và Vận hành (Audit & Operations):** Lưu trữ toàn vẹn nhật ký hoạt động (Activity Log), hệ thống thông báo (Notifications) và quản lý trạng thái các tiến trình nền (Background Jobs), đảm bảo tính minh bạch và khả năng truy vết (Traceability).

## Kiến trúc hệ thống (System Architecture)

Hệ thống tuân thủ mô hình Client-Server kết hợp Background Workers, phân tách rõ ràng tác vụ xử lý nhanh (HTTP requests) và tác vụ tính toán nặng.

### Công nghệ sử dụng (Tech Stack)
- **Frontend:** Vue 3, Vite, TailwindCSS, SignalR Client.
- **Backend API:** ASP.NET Core (.NET 9), Clean Architecture, CQRS Pattern (MediatR), FluentValidation.
- **Background Workers:** .NET 9 Worker Services kết hợp RabbitMQ để xử lý hàng đợi (Clone, Parse, Analyze, Diff, Preview).
- **Cơ sở dữ liệu chính:** PostgreSQL (Lưu trữ Core data & Metadata).
- **Bộ nhớ đệm & Khóa phân tán (Cache & Distributed Lock):** Redis.
- **Message Broker:** RabbitMQ.
- **Lưu trữ đối tượng (Object Storage):** MinIO (Lưu trữ Diff artifacts, thumbnails, snapshots).
- **Giám sát hệ thống (Observability):** Prometheus, Grafana, Loki.

## Hướng dẫn triển khai (Getting Started)

### Yêu cầu môi trường (Prerequisites)
- [Node.js](https://nodejs.org/) (phiên bản LTS).
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) kiến trúc x64.
- [Docker & Docker Compose](https://www.docker.com/) dành cho hạ tầng mạng.

### 1. Khởi chạy hạ tầng (Infrastructure)
Khởi động cơ sở dữ liệu, Redis, RabbitMQ và MinIO thông qua Docker Compose:
```bash
cd GodForge-BE
docker-compose up -d
```

### 2. Thiết lập Backend (.NET 9)
Tiến hành khôi phục các gói thư viện và chạy Entity Framework Migrations để khởi tạo cấu trúc dữ liệu trên PostgreSQL (Tham chiếu mật khẩu tại `docker-compose.yml`):
```bash
cd GodForge-BE
dotnet restore
dotnet tool restore
dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

Khởi chạy API Server:
```bash
cd src/GodForge.Api
dotnet run
```

### 3. Thiết lập Frontend (Vue 3)
Cài đặt các gói phụ thuộc và khởi động Development Server:
```bash
cd GodForge-FE
npm install
npm run dev
```

## Cấu trúc mã nguồn (Repository Structure)

```text
GodForge/
├── GodForge-BE/                         # Backend — ASP.NET Core .NET 9
│   ├── src/
│   │   ├── GodForge.Api/                # API controllers, middlewares, DI setup
│   │   ├── GodForge.Application/        # CQRS use cases, DTOs, business logic
│   │   ├── GodForge.Domain/             # Entities, value objects, domain events
│   │   ├── GodForge.Infrastructure/     # EF Core, Database, Redis, RabbitMQ, Git adapters
│   │   └── GodForge.Worker/             # Background job processors
│   └── tests/                           # UnitTests & IntegrationTests
├── GodForge-FE/                         # Frontend — Vue 3
└── docs/                                # Tài liệu đặc tả hệ thống (SRS, ADR, RBAC, v.v.)
```

## Tài liệu kỹ thuật (Documentation)

Chi tiết về đặc tả yêu cầu hệ thống và quy trình nghiệp vụ được lưu trữ tại thư mục `docs/`. Khuyến nghị xem xét các tệp tài liệu sau trước khi tham gia phát triển:
- [00-overview.md](docs/SRS/00-overview.md): Tổng quan dự án.
- [02-architecture.md](docs/SRS/02-architecture.md): Tiêu chuẩn Clean Architecture và cấu trúc Worker processing.
- [04-database.md](docs/SRS/04-database.md) & [05-api.md](docs/SRS/05-api.md): Quy chuẩn Database và thiết kế API.
- [12-worker-processing.md](docs/SRS/12-worker-processing.md): Chu trình sống của Background Job và nguyên tắc Idempotency.

## Phạm vi dự án (Disclaimer)

Dự án này được xây dựng và đóng gói hoàn toàn cho mục đích bảo vệ **Đồ án tốt nghiệp**. Phạm vi của GodForge không bao gồm việc thay thế môi trường phát triển Godot Editor hay các Git Client nâng cao. Hệ thống được định vị là một nền tảng phụ trợ, tập trung vào công tác quản trị, phân tích chuyên sâu và áp dụng quy trình DevOps cho các dự án trò chơi phát triển bằng Godot Engine.
