---
name: scale-calc
description: >-
  Reference and audit guide for Shot Weight calculation formulas and weighing logic in SSSW.
  Use this skill whenever working on ShotWeightViewModel, C021-C024 calculations, REX items,
  Cleat_Ring cascade subtraction, Inlays, or tolerance verification.
---

# ⚖️ SSSW Shot Weight Calculation & Audit Guide

This skill guides you through auditing, modifying, or testing the weight calculation logic in `ShotWeightViewModel.cs`.

## Critical Rule: Zero Logic Mutation
Any optimization or refactoring must produce **100% identical numeric output** to the existing business rules.

## Core Fields & Formula Reference

| Field | Meaning | Standard Injection Item | REX Item (`C002.StartsWith("REX")`) |
|-------|---------|-------------------------|------------------------------------|
| `C024` | Gross / Total Weight (g) | Weighed in 1st Scale (Part + Runner) | Weighed in single scale |
| `C022` | Runner Weight (g) | Weighed in 2nd Scale (Runner only) | Always `0` (No runner) |
| `C023` | Part Weight per shot (g) | `= C024 - C022` | `= C024` |
| `C021` | Part Weight per pair (g/prs)| `= C023 / (PairsPerShot * %Usage)` | `= C023 / (PairsPerShot * %Usage)` |

## Special Handlings

### 1. `Cleat_Ring` Cascade Subtraction
- When a `Cleat_Ring` row is saved, its net plastic weight must subtract the weight of the pre-inserted REX cleat studs weighed in the prior step of the same sequence:
  ```csharp
  // Must match StartsWith("Cleat_Ring")
  item.C021 = item.C023 - rexStep.C023;
  ```
- **Do NOT** use `StartsWith("Ring")` (historical bug that caused negative weights).

### 2. Multi-size Molds (Shared Shot)
- When multiple sizes share one mold, group by `GlobalVariable.PrefixUpToThirdHyphen(item.C002)` and distribute weight according to BOM usage percentages (`_percentOfUsage`).

### 3. Sequential Step Check
- Steps must be weighed in order (`C015`).
- If an earlier step has `C021 == 0`, alert the user and reject saving later steps.
