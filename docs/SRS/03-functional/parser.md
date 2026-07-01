# Functional Requirements: Parser & Metadata

## FR-08: Scene parser
**Mức ưu tiên:** Must
**Mô tả:** Worker ngầm Parse tệp `.tscn` để trích xuất node tree, external resource, sub-resource, connection và script reference.
- **Ràng buộc:** Chạy bất đồng bộ. Lỗi ở 1 tệp scene không được làm hỏng tiến trình quét của toàn dự án.

## FR-09: Scene viewer
**Mức ưu tiên:** Must
**Mô tả:** Hiển thị cấu trúc scene dưới dạng cây node, property chính, script gắn kèm. Hỗ trợ tìm kiếm, lọc node.

## FR-10: Asset manager
**Mức ưu tiên:** Must
**Mô tả:** Quản lý asset (.png, .wav...): loại, kích thước, đường dẫn. Xem asset đang được sử dụng ở scene hay script nào. Cảnh báo asset không sử dụng hoặc asset bị thiếu.
