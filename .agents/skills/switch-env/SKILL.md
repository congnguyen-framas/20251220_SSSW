---
name: switch-env
description: >-
  Switch between framas factory deployment environments (e.g. fFT vs fVN).
  Use this skill whenever the user wants to switch target site, check active site configuration,
  or prepare a build for a specific plant.
---

# 🏭 Switch Environment Workflow (fVN / fFT)

This skill provides step-by-step guidance for safely toggling deployment and database target environments between Vietnam (**fVN**) and Fast Track (**fFT**).

## 1. Environment Map

| Site | Location Code | `mesocomp` | Deploy Share Path in `DeployConfig.props` | DB Server |
|------|--------------|------------|-------------------------------------------|-----------|
| **fFT** (Fast Track) | `fFT` | `FTT1` | `\\10.40.10.9\DogeWH_Share\fFT\Update` | SQL Server (FT connection string) |
| **fVN** (Vietnam) | `fVN` | `VNT1` | `\\192.168.1.241\DogeWH_Share\fVN\Update` | SQL Server (VN connection string) |

## 2. Switching Steps

### Step 1: Update `SSSW/DeployConfig.props`
Open `SSSW/DeployConfig.props`. Comment the currently active `<DeployDir>` block and uncomment the target site's block.
> [!WARNING]
> Do NOT edit `SSSW.csproj` directly. The csproj imports `DeployConfig.props`.

### Step 2: Update `SSSW/appsettings.json`
Open `SSSW/appsettings.json`. Comment/uncomment the corresponding `"SSSW"` encrypted connection string:
- For **fFT**: Enable the line under `//fFT`.
- For **fVN**: Enable the line under `//fVN`.

### Step 3: Validate Alignment
Ensure that `DeployConfig.props` and `appsettings.json` are both targeting the same site. A mismatch (e.g., deploying to fFT with an fVN database connection) will cause cross-plant data contamination.

### Step 4: Verify Build
Run:
`dotnet build SSSW/SSSW.sln`
