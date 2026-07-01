# Functional Requirements: Parser & Metadata

## FR-08: Scene Parser
**Mức ưu tiên:** Must  
**Mô tả:** Worker ngầm parse tệp `.tscn` để trích xuất metadata cấu trúc scene.

### Dữ liệu trích xuất từ `.tscn`
| Field | Mô tả | Ví dụ |
|-------|-------|-------|
| `scene_name` | Tên file scene | `MainMenu.tscn` |
| `scene_path` | Đường dẫn trong project | `scenes/ui/MainMenu.tscn` |
| `format_version` | Godot format version | `3` (Godot 4.x) |
| `node_count` | Tổng số node | `42` |

### Node Tree trích xuất
Mỗi node trong scene được parse ra:
| Field | Mô tả |
|-------|-------|
| `name` | Tên node |
| `type` | Loại node (Node2D, Sprite2D, Control...) |
| `parent` | Parent path (`.` = root) |
| `script` | Script gắn kèm (nếu có) — path tới `.gd` file |
| `properties` | Key-value property quan trọng (groups, signals, etc.) |

### External Resources
- Trích xuất danh sách `[ext_resource]`: type, path, id.
- Mapping resource → file thực tế trong project.

### Sub-resources
- Trích xuất `[sub_resource]`: type, id, properties.

### Signal Connections
- Trích xuất `[connection]`: signal, from, to, method.

### Luồng xử lý
1. Job `parse` nhận danh sách file từ repo workspace.
2. Lọc file `.tscn`, `.tres`, `.gd`, asset files.
3. Parse tuần tự từng file, ghi kết quả vào Metadata Schema.
4. Nếu 1 file lỗi → log error, skip, tiếp tục file tiếp theo.
5. Cập nhật job progress (% hoàn thành) qua SignalR.
6. Hoàn thành → cập nhật job status = `completed`, gửi notification.

### Xử lý lỗi
| Tình huống | Hành vi |
|------------|---------|
| File `.tscn` bị corrupt/invalid format | Log warning, skip file, tiếp tục |
| File quá lớn (> 10MB) | Log warning, skip file |
| Encoding không phải UTF-8 | Thử detect encoding, fallback skip |

### Business Rules
- **BR-41:** Lỗi parse 1 file KHÔNG được crash toàn bộ job. Phải isolate và tiếp tục.
- **BR-42:** Parse job chạy hoàn toàn async qua RabbitMQ Worker.
- **BR-43:** Nếu scene đã parse trước đó → so sánh file hash, chỉ re-parse nếu thay đổi (incremental).
- **BR-44:** Parse kết quả ghi vào Metadata Schema, có thể tái tạo bất cứ lúc nào.

### Acceptance Criteria
- [x] AC-53: Parse project 100 scenes → tất cả scenes được ghi vào DB với node tree đúng.
- [x] AC-54: 1 file `.tscn` bị corrupt → file đó bị skip, 99 scenes còn lại parse thành công.
- [x] AC-55: Re-parse project → chỉ parse file thay đổi (incremental), thời gian nhanh hơn lần đầu.
- [x] AC-56: Parse job hiển thị progress realtime.

---

## FR-09: Scene Viewer
**Mức ưu tiên:** Must  
**Mô tả:** Hiển thị cấu trúc scene dưới dạng cây node tương tác.

### Giao diện
- **Tree View:** Hiển thị node tree giống Godot Editor (tên + icon theo type).
- **Node Detail Panel:** Click node → hiển thị properties, script, signals, groups.
- **Search & Filter:**
  - Tìm kiếm theo tên node, type.
  - Filter theo type (chỉ hiện Control, Node2D, v.v.).
- **Breadcrumb:** Hiển thị đường dẫn từ root đến node đang chọn.

### Dữ liệu hiển thị cho mỗi Node
| Mục | Mô tả |
|-----|-------|
| Name | Tên node |
| Type | Loại node (icon tương ứng) |
| Script | Link tới script file (nếu có) |
| Properties | Key-value properties quan trọng |
| Signals | Danh sách signal connections |
| Groups | Danh sách groups |
| Children | Số lượng child nodes |

### Business Rules
- **BR-45:** Dữ liệu hiển thị đọc từ Metadata Schema (đã parse), không parse realtime.
- **BR-46:** Scene chưa parse → hiển thị message "Scene chưa được phân tích" + button trigger parse.

### Acceptance Criteria
- [x] AC-57: Mở scene viewer → hiển thị node tree đúng cấu trúc.
- [x] AC-58: Click node → hiển thị properties, script, signals.
- [x] AC-59: Tìm kiếm "Button" → highlight tất cả node type Button.
- [x] AC-60: Scene chưa parse → thông báo + nút trigger parse.

---

## FR-10: Asset Manager
**Mức ưu tiên:** Must  
**Mô tả:** Quản lý và trực quan hóa asset trong project Godot.

### Loại asset hỗ trợ
| Loại | Extension | Hiển thị |
|------|-----------|----------|
| Image | `.png`, `.jpg`, `.svg`, `.webp` | Thumbnail preview |
| Audio | `.wav`, `.ogg`, `.mp3` | File info + player |
| Font | `.ttf`, `.otf` | Font name + sample text |
| Shader | `.gdshader` | Code preview |
| Other | `.import`, `.cfg` | File info |

### Chức năng
- **Asset List:** Hiển thị tất cả asset với grid/list view, sortable, filterable.
- **Asset Detail:** Click asset → type, size, path, format, dimensions (nếu image).
- **Usage Tracking:** Asset đang được dùng ở scene/script nào (reverse lookup từ dependencies).
- **Cảnh báo:**
  - **Unused assets:** Asset không được reference bởi scene/script nào.
  - **Missing assets:** Scene/script reference file không tồn tại.

### Business Rules
- **BR-47:** Asset data đọc từ Metadata Schema, kết hợp dependencies table.
- **BR-48:** Thumbnail cho images được generate bởi Worker, lưu MinIO.
- **BR-49:** "Unused" detection dựa trên dependency graph — có thể false positive nếu asset load dynamic.

### Acceptance Criteria
- [x] AC-61: Mở asset manager → hiển thị danh sách asset với thumbnail.
- [x] AC-62: Xem asset detail → hiển thị đúng type, size, path, usage.
- [x] AC-63: Asset không được reference → hiện cảnh báo "Unused".
- [x] AC-64: Scene reference file không tồn tại → hiện cảnh báo "Missing".
