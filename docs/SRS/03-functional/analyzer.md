# Functional Requirements: Analyzer & Diff

## FR-11: Dependency Graph
**Mức ưu tiên:** Must  
**Mô tả:** Xây dựng và trực quan hóa graph phụ thuộc giữa các resource trong project Godot.

### Loại dependency (Edge)
| From | To | Relation |
|------|----|----------|
| Scene | Scene | `instances` (sub-scene) |
| Scene | Script | `attaches` (script gắn vào node) |
| Scene | Resource | `uses` (ext_resource) |
| Scene | Asset | `references` (image, audio...) |
| Script | Script | `extends` / `preload` / `load` |
| Script | Scene | `preload` / `load` |
| Script | Asset | `preload` / `load` |
| Resource | Asset | `references` |

### Giao diện Graph
- **Interactive Graph:** Zoom, pan, drag nodes.
- **Node:** Mỗi file là 1 node, icon theo type (scene/script/asset).
- **Edge:** Đường nối có mũi tên, label kiểu relation.
- **Filter:** Theo type (chỉ scene, chỉ script), theo depth (1-hop, 2-hop, all).
- **Focus mode:** Click 1 node → highlight tất cả incoming + outgoing dependencies.
- **Cyclic Detection:** Đường vòng phụ thuộc (A→B→C→A) highlight bằng màu đỏ.

### Thuật toán
- Xây dựng DAG từ `dependencies` table.
- Phát hiện cycle: DFS-based cycle detection (Tarjan's hoặc tương đương).
- Lưu cycle info trong `health_issues` (severity: `warning`).

### Business Rules
- **BR-50:** Graph data đọc từ `dependencies` table (Metadata Schema).
- **BR-51:** Graph chỉ hiển thị data của project hiện tại.
- **BR-52:** Nếu project chưa analyze → hiển thị "Chưa có dữ liệu" + button trigger analyze.
- **BR-53:** Graph layout sử dụng force-directed layout (client-side rendering).

### Acceptance Criteria
- [x] AC-65: Mở dependency graph → hiển thị graph interactive với nodes và edges.
- [x] AC-66: Filter chỉ scene → chỉ hiện scene nodes và liên kết giữa chúng.
- [x] AC-67: Có cyclic dependency A→B→A → đường vòng highlight đỏ.
- [x] AC-68: Click node → focus mode highlight incoming/outgoing.
- [x] AC-69: Project chưa analyze → thông báo + nút trigger.

---

## FR-12: Project Health Analyzer
**Mức ưu tiên:** Must  
**Mô tả:** Phân tích sức khỏe dự án Godot, tính health score, phát hiện vấn đề.

### Health Score Formula
```
Health Score = 100 - Σ(penalty_per_issue)

Penalty per issue:
  - Critical: -10 điểm/issue (cap: -50)
  - Warning:  -3 điểm/issue  (cap: -30)
  - Info:     -1 điểm/issue  (cap: -10)

Minimum score: 0
Maximum score: 100
```

### Danh sách Health Issues
| Issue Type | Severity | Mô tả |
|------------|----------|-------|
| `missing_resource` | Critical | Scene reference file không tồn tại |
| `missing_script` | Critical | Node gắn script nhưng script không tồn tại |
| `cyclic_dependency` | Warning | Vòng phụ thuộc giữa các file |
| `unused_asset` | Info | Asset không được reference |
| `oversized_scene` | Warning | Scene có > 500 nodes |
| `deep_nesting` | Warning | Node tree sâu > 10 level |
| `duplicate_resource` | Info | Resource trùng lặp (cùng content, khác path) |
| `missing_import` | Warning | File `.import` tồn tại nhưng source file bị xóa |

### Luồng xử lý
1. Job `analyze` nhận project_id.
2. Đọc Metadata Schema (scenes, assets, scripts, dependencies).
3. Chạy từng rule → sinh danh sách issues.
4. Tính health score.
5. Lưu vào `health_reports` + `health_issues`.
6. Cập nhật Redis Cache cho dashboard.
7. Nếu có issue Critical → gửi notification.

### Business Rules
- **BR-54:** Analyze job chạy async qua RabbitMQ Worker.
- **BR-55:** Health report lưu history — không ghi đè report cũ (để so sánh trend).
- **BR-56:** Health score hiển thị trên dashboard và project detail.
- **BR-57:** Analyze yêu cầu parse hoàn thành trước. Nếu chưa parse → reject job.

### Acceptance Criteria
- [x] AC-70: Chạy analyze → sinh health report với score 0-100.
- [x] AC-71: Project có 2 missing resource → 2 issues Critical, score giảm 20 điểm.
- [x] AC-72: Có issue Critical → user nhận notification.
- [x] AC-73: Health history giữ report cũ → xem trend qua thời gian.
- [x] AC-74: Chạy analyze khi chưa parse → lỗi rõ ràng, yêu cầu parse trước.

---

## FR-13: Scene Diff Viewer
**Mức ưu tiên:** Must  
**Mô tả:** So sánh scene theo cấu trúc (scene-aware diff) thay vì text-based diff.

### Loại Diff
| Loại | Mô tả |
|------|-------|
| **Commit Diff** | So sánh scene giữa 2 commit |
| **Branch Diff** | So sánh scene giữa 2 branch (tip commit) |

### Hiển thị Diff
#### Node-level Diff
- **Added nodes:** Highlight xanh lá, hiển thị full node info.
- **Removed nodes:** Highlight đỏ, hiển thị node info trước khi xóa.
- **Modified nodes:** Highlight vàng, hiển thị property changes (old → new).
- **Moved nodes:** Hiển thị parent path cũ → mới.
- **Unchanged nodes:** Hiện mờ (collapsed by default).

#### Property-level Diff
- Cho mỗi modified node, hiển thị danh sách property thay đổi:
  - Property name, old value, new value.
  - Highlight inline diff cho string values.

#### Summary Statistics
- Tổng số nodes: added / removed / modified / unchanged.
- Tổng số properties changed.

### Luồng xử lý
1. User chọn 2 commit hoặc 2 branch để so sánh.
2. Nếu cả 2 version đã parse → so sánh từ Metadata.
3. Nếu chưa parse → tạo job parse tạm (parse 2 version của file `.tscn`).
4. Sinh diff artifact, lưu MinIO (cache).
5. Trả về diff data cho frontend render.

### Business Rules
- **BR-58:** Diff được cache trong MinIO (key: `{scene_path}:{commit_a}:{commit_b}`).
- **BR-59:** Cache diff expire sau 7 ngày hoặc khi re-analyze.
- **BR-60:** Scene diff chỉ áp dụng cho file `.tscn`. File khác dùng text diff thông thường.
- **BR-61:** Diff job chạy async, trả 202 nếu cần compute.

### Acceptance Criteria
- [x] AC-75: So sánh 2 commit của cùng scene → hiển thị node-level diff (added/removed/modified).
- [x] AC-76: Node được thêm → highlight xanh lá, hiển thị full info.
- [x] AC-77: Property changed → hiển thị old value → new value.
- [x] AC-78: Diff đã cache → response nhanh (< 500ms).
- [x] AC-79: File non-tscn → fallback text diff.
