# ADR 0006: Deterministic analysis is authoritative; AI is advisory

## Status

Accepted.

## Context

Gemini có thể giải thích code và đưa ra khuyến nghị nhưng output không ổn định, có thể hallucinate và có chi phí/provider risk. GodForge cần kết quả trace được về commit, file, scene và rule.

## Decision

- Parser và health rule engine tạo dữ liệu đo được và health score.
- Gemini chỉ nhận context đã chọn lọc, giới hạn và redacted.
- AI findings được lưu riêng, có provider/model/prompt version/input hash/confidence/evidence.
- AI failure làm pipeline ở trạng thái degraded, không xóa deterministic report.
- Không gửi binary, `.env`, private key, token hoặc repository credential.
- Gemini chỉ được gọi từ worker qua `IAiAnalysisProvider`.

## Rejected alternative

Gửi toàn bộ repository TXT trực tiếp từ API và dùng Gemini làm health score. Phương án này bị từ chối vì không kiểm soát kích thước, secrets, chi phí và tính tái lập.

## Consequences

- Cần context builder, secret redactor, quota và schema validation.
- UI phải phân biệt measured finding và AI-generated recommendation.
