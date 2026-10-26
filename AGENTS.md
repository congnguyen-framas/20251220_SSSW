# SSSW Project Agent Rules

## Core Operational Directives

1. **Zero Logic Mutation**:
   - Maintain 100% numerical and logical equivalence with existing business rules in `ShotWeightViewModel.cs` and `ShotWeightFGViewModel.cs`.
   - Never alter the formulas for `C021`, `C022`, `C023`, `C024`.
   - Preserve edge case handlers (`REX`, `Cleat_Ring` cascade subtraction, `Inlay`, Multi-size mold allocation).

2. **Database Integrity**:
   - Entities under `models/ef/entities/` (`FT600` through `FT609`, `FT029`, `FT031`) map directly to Framas SQL Server MES schema. Do NOT rename `C0xx` properties or alter `HasQueryFilter(p => p.Actived == true)`.

3. **MVVM & Hardware Architecture**:
   - Keep UI code-behind free of business logic.
   - Do NOT instantiate new driver instances; use the singleton `DeviceConnectionService`.
   - Marshal all driver callbacks from background threads to the UI thread via `Dispatcher.InvokeAsync`.
   - Explicitly alias conflicting WinForms/WPF types (`Brush`, `Color`, `MessageBox`).

4. **Multi-site Deployment**:
   - Switch plant targets exclusively in `SSSW/DeployConfig.props` and `SSSW/appsettings.json`. Never modify `<DeployDir>` directly in `SSSW.csproj`.

5. **Vibe Coding & Documentation**:
   - Consult [`SSSW/docs/INDEX.md`](SSSW/docs/INDEX.md) and [`SSSW/docs/sessions/INDEX.md`](SSSW/docs/sessions/INDEX.md) before implementing changes.
   - Log all non-trivial sessions in `SSSW/docs/sessions/YYYY-MM-DD_slug.md`.
