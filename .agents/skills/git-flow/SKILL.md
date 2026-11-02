---
name: git-flow
description: >-
  Automate common Git operations in SSSW: pull main into current branch,
  commit and push to current branch, and create Pull Requests (merge-to).
---

# 🚀 Git Workflow Automation Skill

This skill documents and executes Git automation workflows for the SSSW repository.

## Commands Reference

### 1. Pull latest `main` into current branch
- **Batch / CMD**:
  ```cmd
  .\git-flow pull
  ```
- **PowerShell**:
  ```powershell
  .\scripts\git-flow.ps1 pull
  ```
- **Git Alias**:
  ```bash
  git pull-main
  ```

### 2. Commit & Push to current branch
- **Batch / CMD**:
  ```cmd
  .\git-flow commit-push "feat: your commit message"
  ```
- **PowerShell**:
  ```powershell
  .\scripts\git-flow.ps1 commit-push "feat: your commit message"
  ```
- **Git Alias**:
  ```bash
  git cp "feat: your commit message"
  ```

### 3. Create Pull Request (merge to target branch, default `main`)
- **Batch / CMD**:
  ```cmd
  .\git-flow merge-to main
  ```
- **PowerShell**:
  ```powershell
  .\scripts\git-flow.ps1 merge-to main
  ```
- **Git Alias**:
  ```bash
  git pr main
  ```
  *Note*: If GitHub CLI (`gh`) is installed, it opens interactive PR creation. If not, it automatically opens the GitHub Compare & PR URL in your default web browser.

### 4. Interactive Menu
Running without arguments launches an interactive selection menu:
```cmd
.\git-flow
```
