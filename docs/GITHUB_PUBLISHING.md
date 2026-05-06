# GitHub Publishing

This environment can publish files through the GitHub connector once the repository exists.

## Option A: Create Empty Repo Manually

1. Create an empty GitHub repository named `tia-portal-agentic-toolkit`.
2. Do not add README/license/gitignore in GitHub; this repo already has them.
3. Tell Codex the repository full name, for example:

```text
javit/tia-portal-agentic-toolkit
```

Codex can then upload the source files through the GitHub contents API.

## Option B: Install Git Locally

Install Git for Windows, then from this directory:

```powershell
git init
git add .
git commit -m "Initial TIA Portal Agentic Toolkit"
git branch -M main
git remote add origin https://github.com/<owner>/tia-portal-agentic-toolkit.git
git push -u origin main
```

Do not commit `bin/` or `obj/`; `.gitignore` excludes them.

## Option C: Upload With A Local Token

Do not paste GitHub tokens into Codex or chat. Set a fine-grained token only in your local PowerShell session:

```powershell
$env:GITHUB_TOKEN = "YOUR_FINE_GRAINED_TOKEN"
powershell -ExecutionPolicy Bypass -File .\scripts\upload-github.ps1 -Repository "owner/tia-portal-agentic-toolkit"
Remove-Item Env:\GITHUB_TOKEN
```

The token needs Contents read/write access to the target repository.
