# ADR 0005: Use Forgejo as the hosted Git engine

## Status

Accepted.

## Context

GodForge cần cho phép người dùng clone, push và pull repository nhưng mục tiêu sản phẩm là phân tích dự án Godot, không phải tự xây Git server. Việc tự triển khai Git Smart HTTP/SSH, object storage, refs, hooks và permission model tạo scope lớn và rủi ro bảo mật cao.

## Decision

- External linked repository tiếp tục dùng clone/fetch chuẩn.
- Internal hosted repository được provision qua Forgejo API.
- Forgejo chịu trách nhiệm Git protocol, object database, refs, SSH/HTTP authentication và webhook.
- GodForge chịu trách nhiệm project/member mapping, analysis pipeline, metadata, report và product UI.
- Application phụ thuộc vào `IHostedGitService`; Forgejo adapter nằm trong Infrastructure.

## Rejected alternative

Tự viết Git protocol server trong ASP.NET Core. Phương án này bị từ chối vì không tạo lợi thế sản phẩm và vượt scope MVP.

## Consequences

- Thêm một dependency triển khai nhưng giảm mạnh lượng code bảo mật nhạy cảm.
- Cần outbox/permission synchronization trước khi bật hosted repository cho production.
- Linked repository pipeline phải hoạt động độc lập với Forgejo.
