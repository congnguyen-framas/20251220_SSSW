# 🤖 SSSW Subagents

Tài liệu định nghĩa các Subagents chuyên biệt phục vụ cho dự án **SSSW**:

| Tên Subagent | Vai trò | Công cụ | Mục đích sử dụng |
|--------------|---------|---------|------------------|
| **`sssw-code-reviewer`** | Code Reviewer & Safety Auditor | Read-only | Kiểm tra diff code xem có vi phạm nguyên tắc Zero Logic Mutation, xung đột thread WinForms/WPF, hoặc sai sót trong entity EF Core không. |
| **`sssw-scale-tester`** | Calculation & Formula Auditor | Read-only | Mô phỏng và kiểm thử thuật toán tính toán `C021`–`C024` trên các kịch bản khuôn mẫu (Standard, REX, Cleat_Ring, Inlay, Multi-size). |

## Cách gọi Subagent trong code / prompt
Khi cần kiểm tra code hoặc test công thức, AI chính có thể gọi trực tiếp thông qua công cụ `invoke_subagent` với `TypeName: "sssw-code-reviewer"` hoặc `"sssw-scale-tester"`.
