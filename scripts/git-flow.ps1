# ============================================================================
# Git Workflow Automation Helper for SSSW
# Supported actions:
#   1. pull        : Kéo code mới nhất từ nhánh main về nhánh hiện tại
#   2. commit-push : Stage tất cả thay đổi, commit và push lên nhánh hiện tại
#   3. merge-to    : Tạo Pull Request (PR) merge nhánh hiện tại vào nhánh đích (mặc định main)
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $false)]
    [ValidateSet("pull", "commit-push", "cp", "merge-to", "pr", "status", "help")]
    [string]$Action,

    [Parameter(Position = 1, ValueFromRemainingArguments = $true)]
    [string[]]$ArgsRemaining
)

$ErrorActionPreference = "Stop"

function Get-CurrentBranch {
    $branch = (git branch --show-current 2>$null)
    if (-not $branch) {
        Write-Error "Không thể xác định nhánh Git hiện tại hoặc thư mục không phải là git repository."
    }
    return $branch.Trim()
}

function Get-RepoWebUrl {
    $remote = (git config --get remote.origin.url 2>$null)
    if (-not $remote) {
        return $null
    }
    $remote = $remote.Trim()
    # Chuyển đổi SSH git@github.com:owner/repo.git -> https://github.com/owner/repo
    if ($remote -match "^git@github\.com:(.+?)(\.git)?$") {
        return "https://github.com/$($Matches[1])"
    }
    # Chuyển đổi HTTPS https://github.com/owner/repo.git -> https://github.com/owner/repo
    if ($remote -match "^https?://github\.com/(.+?)(\.git)?$") {
        return "https://github.com/$($Matches[1])"
    }
    return $remote -replace '\.git$', ''
}

# Nếu không truyền action, hiển thị menu lựa chọn
if (-not $Action) {
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "       SSSW GIT WORKFLOW AUTOMATION       " -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
    $cur = Get-CurrentBranch
    Write-Host "Nhánh hiện tại: " -NoNewline
    Write-Host $cur -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1) pull        - Kéo code từ main về $cur"
    Write-Host "2) commit-push - Commit và push code lên $cur"
    Write-Host "3) merge-to    - Tạo PR merge $cur vào nhánh khác (mặc định: main)"
    Write-Host "4) status      - Xem trạng thái git status & branch"
    Write-Host "0) Thoát"
    Write-Host ""
    $choice = Read-Host "Chọn thao tác (1/2/3/4/0)"
    switch ($choice) {
        "1" { $Action = "pull" }
        "2" { $Action = "commit-push" }
        "3" { $Action = "merge-to" }
        "4" { $Action = "status" }
        default {
            Write-Host "Hủy thao tác." -ForegroundColor Gray
            exit 0
        }
    }
}

switch ($Action.ToLower()) {
    "pull" {
        $curBranch = Get-CurrentBranch
        Write-Host ">> [PULL] Kéo code từ 'origin/main' về nhánh '$curBranch'..." -ForegroundColor Cyan
        
        # Kiểm tra xem có uncommitted changes hay không
        $status = git status --porcelain
        if ($status) {
            Write-Warning "Cảnh báo: Bạn đang có file chưa commit trong thư mục làm việc."
            $confirm = Read-Host "Bạn có muốn tiếp tục git pull không? (y/n)"
            if ($confirm -ne 'y' -and $confirm -ne 'Y') {
                Write-Host "Đã hủy thao tác pull." -ForegroundColor Yellow
                exit 0
            }
        }

        git pull origin main
        if ($LASTEXITCODE -eq 0) {
            Write-Host ">> Đã kéo code từ main về '$curBranch' thành công!" -ForegroundColor Green
        } else {
            Write-Error "Lỗi khi kéo code từ main. Vui lòng kiểm tra xung đột (conflict) nếu có."
        }
    }

    { $_ -in "commit-push", "cp" } {
        $curBranch = Get-CurrentBranch
        Write-Host ">> [COMMIT-PUSH] Đang xử lý cho nhánh '$curBranch'..." -ForegroundColor Cyan

        # Ghép các tham số sau thành message
        $msg = if ($ArgsRemaining) { $ArgsRemaining -join " " } else { "" }
        if ([string]::IsNullOrWhiteSpace($msg)) {
            $msg = Read-Host "Nhập nội dung commit message"
        }

        if ([string]::IsNullOrWhiteSpace($msg)) {
            Write-Host "Commit message không được để trống. Đã hủy." -ForegroundColor Red
            exit 1
        }

        Write-Host ">> Đang stage toàn bộ thay đổi (git add -A)..." -ForegroundColor Gray
        git add -A

        $status = git status --porcelain
        if (-not $status) {
            Write-Host ">> Không có thay đổi nào để commit." -ForegroundColor Yellow
            exit 0
        }

        Write-Host ">> Đang commit với thông điệp: '$msg'..." -ForegroundColor Gray
        git commit -m $msg
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Lỗi khi thực hiện git commit."
        }

        Write-Host ">> Đang push lên remote branch '$curBranch'..." -ForegroundColor Gray
        git push origin $curBranch
        if ($LASTEXITCODE -eq 0) {
            Write-Host ">> Đã commit và push thành công lên origin/$curBranch!" -ForegroundColor Green
        } else {
            Write-Error "Lỗi khi push lên remote origin/$curBranch."
        }
    }

    { $_ -in "merge-to", "pr" } {
        $curBranch = Get-CurrentBranch
        $targetBranch = if ($ArgsRemaining -and $ArgsRemaining.Length -gt 0) { $ArgsRemaining[0] } else { "" }
        
        if ([string]::IsNullOrWhiteSpace($targetBranch)) {
            $inputTarget = Read-Host "Nhập nhánh đích muốn merge vào [mặc định: main]"
            $targetBranch = if ([string]::IsNullOrWhiteSpace($inputTarget)) { "main" } else { $inputTarget.Trim() }
        }

        if ($curBranch -eq $targetBranch) {
            Write-Warning "Nhánh hiện tại trùng với nhánh đích ('$curBranch'). Không thể tạo PR vào chính nó."
            exit 1
        }

        Write-Host ">> [MERGE-TO / PR] Tạo Pull Request: '$curBranch' -> '$targetBranch'..." -ForegroundColor Cyan

        $webUrl = Get-RepoWebUrl
        if (-not $webUrl) {
            Write-Error "Không tìm thấy URL remote repository để tạo PR."
        }

        $prUrl = "$webUrl/compare/$targetBranch...$curBranch?expand=1"
        
        # Kiểm tra nếu GitHub CLI (gh) đã được cài đặt
        $ghCmd = Get-Command gh -ErrorAction SilentlyContinue
        if ($ghCmd) {
            Write-Host ">> Tìm thấy GitHub CLI (gh). Đang mở giao diện tạo PR..." -ForegroundColor Gray
            gh pr create --base $targetBranch --head $curBranch --web
        } else {
            Write-Host ">> Đang mở trang tạo PR trên trình duyệt:" -ForegroundColor Yellow
            Write-Host "   $prUrl" -ForegroundColor Green
            Start-Process $prUrl
        }

        Write-Host ">> Link tạo PR:" -ForegroundColor Cyan
        Write-Host "   $prUrl" -ForegroundColor Yellow
    }

    "status" {
        $curBranch = Get-CurrentBranch
        Write-Host ">> Nhánh hiện tại: $curBranch" -ForegroundColor Yellow
        git status
    }

    "help" {
        Write-Host "Cách dùng git-flow.ps1:" -ForegroundColor Cyan
        Write-Host "  .\scripts\git-flow.ps1 pull" -ForegroundColor White
        Write-Host "    -> Kéo code mới nhất từ main về nhánh hiện tại."
        Write-Host "  .\scripts\git-flow.ps1 commit-push `"<nội dung commit>`"" -ForegroundColor White
        Write-Host "    -> Stage toàn bộ file, commit và push lên nhánh hiện tại."
        Write-Host "  .\scripts\git-flow.ps1 merge-to [<nhánh_đích>]" -ForegroundColor White
        Write-Host "    -> Mở trang tạo Pull Request merge nhánh hiện tại vào nhánh đích (mặc định main)."
    }
}
