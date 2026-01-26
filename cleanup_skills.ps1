# Skills Cleanup Script - Simplified
# This script removes unnecessary skills for FraudGuard-AI project

$skillsPath = "e:\.agent - Copy (2)\skills"

# Skills to KEEP (Essential for FraudGuard-AI)
$keepSkills = @(
    "bash-linux",
    "docker-expert",
    "postgres-best-practices",
    "api-patterns",
    "api-security-best-practices",
    "database-design",
    "clean-code",
    "code-review-checklist",
    "systematic-debugging",
    "test-driven-development",
    "testing-patterns",
    "performance-profiling",
    "mobile-design",
    "architecture",
    "software-architecture",
    "deployment-procedures",
    "server-management",
    "environment-setup-guide",
    "ethical-hacking-methodology",
    "rag-implementation",
    "prompt-engineering",
    "git-pushing",
    "documentation-templates"
)

Write-Host "Scanning skills directory..." -ForegroundColor Cyan
$allSkills = Get-ChildItem -Path $skillsPath -Directory

$toDelete = $allSkills | Where-Object { $_.Name -notin $keepSkills }

Write-Host "Total skills: $($allSkills.Count)" -ForegroundColor White
Write-Host "Skills to keep: $($keepSkills.Count)" -ForegroundColor Green
Write-Host "Skills to delete: $($toDelete.Count)" -ForegroundColor Red

Write-Host "`nDeleting unnecessary skills..." -ForegroundColor Red
$deletedCount = 0
foreach ($skill in $toDelete) {
    try {
        Remove-Item -Path $skill.FullName -Recurse -Force -ErrorAction Stop
        $deletedCount++
        if ($deletedCount % 20 -eq 0) {
            Write-Host "Deleted $deletedCount skills..." -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "Failed to delete: $($skill.Name)" -ForegroundColor Yellow
    }
}

Write-Host "`nCleanup completed!" -ForegroundColor Green
Write-Host "Deleted: $deletedCount skills" -ForegroundColor White

$remaining = Get-ChildItem -Path $skillsPath -Directory
Write-Host "Remaining: $($remaining.Count) skills" -ForegroundColor Cyan
