---
name: vibe-code
description: >-
  Standard workflow to start, conduct, and wrap up a Vibe Coding session on the SSSW project.
  Use this skill whenever the user asks to vibe code, implement a new feature, fix a bug,
  refactor code, or log a development session.
---

# ⚡ SSSW Vibe Coding Workflow

This skill guides you through the standard 5-step Vibe Coding process for the **SSSW** (.NET 8 WPF/WinForms) project.

## Workflow Steps

### Step 1: Context Loading & Review
1. Read the latest session entry listed in [`SSSW/docs/sessions/INDEX.md`](../../SSSW/docs/sessions/INDEX.md).
2. Check [`SSSW/docs/VIBE_CODING_GUIDELINES.md`](../../SSSW/docs/VIBE_CODING_GUIDELINES.md) and [`CLAUDE.md`](../../CLAUDE.md).
3. If touching database or calculation logic, consult:
   - [`SSSW/docs/tech/DATABASE_SCHEMA.md`](../../SSSW/docs/tech/DATABASE_SCHEMA.md)
   - [`SSSW/docs/tech/BUSINESS_LOGIC.md`](../../SSSW/docs/tech/BUSINESS_LOGIC.md)

### Step 2: Impact Analysis & Safety Check
- **Zero Logic Mutation Rule**: Any calculation optimization or refactoring must produce 100% identical results to the existing business rules (`C021`, `C022`, `C023`, `C024`, `REX`, `Cleat_Ring`, `Inlay`, Multi-size).
- Check if WPF and WinForms namespace collisions exist (e.g., `Brush`, `Color`, `MessageBox`).
- Ensure hardware driver interaction uses the singleton `DeviceConnectionService`.

### Step 3: Implementation
- Implement changes cleanly following MVVM.
- Do not add business logic to UI code-behind files.
- Preserve existing comments and legacy explanations.

### Step 4: Verification
- Run a build to verify there are no compilation errors:
  `dotnet build SSSW/SSSW.sln`
- Verify that no warnings were introduced.

### Step 5: Session Logging & Git Commit
1. Create a new session file: `SSSW/docs/sessions/YYYY-MM-DD_slug.md`.
2. Add the session entry to the top of the table in `SSSW/docs/sessions/INDEX.md`.
3. Provide a clear commit message conforming to the semantic commit format:
   `feat(...)`, `fix(...)`, `refactor(...)`, `docs(...)`, or `chore(...)`.
