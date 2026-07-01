# Functional Requirements: Analyzer & Diff

## FR-11: Dependency graph
**Mức ưu tiên:** Must
**Mô tả:** Xây dựng graph phụ thuộc giữa scene, resource, script và asset. Cho phép zoom, pan, filter, phát hiện "vòng phụ thuộc" (cyclic dependencies).

## FR-12: Project health analyzer
**Mức ưu tiên:** Must
**Mô tả:** Phân tích sức khỏe dự án. Tính health score. Hiển thị danh sách cảnh báo (missing asset, scene quá phức tạp) với độ nghiêm trọng.

## FR-13: Scene diff viewer
**Mức ưu tiên:** Must
**Mô tả:** So sánh scene-aware (scene struct) để hiển thị node thêm/xóa/sửa, property thay đổi giữa 2 commit hoặc nhánh. Trực quan hơn so với line-by-line text diff truyền thống.
