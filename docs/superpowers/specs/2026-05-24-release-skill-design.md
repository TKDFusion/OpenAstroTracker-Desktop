# Release Skill Design

## Overview

A Claude Code skill (`/release`) that creates an OATControl release end-to-end. The skill automates version bumping, changelog generation, git operations, and GitHub draft release creation, while pausing for the manual build step that must be done in Visual Studio.

## Skill Metadata

- **Skill name:** `release`
- **Location:** `.claude/skills/release.md` (project-level skill)
- **Trigger:** User types `/release`, or uses phrases like "make a release", "let's ship", "let's release", "create a release", "ship it", "cut a release"

## Flow

### Step 1: Version Prompt

- Read current version from `OATControl/Properties/AssemblyInfo.cs` (AssemblyVersion)
- Show current version to user
- Propose next version by bumping minor number (e.g. `1.2.0.0` → `1.3.0.0`)
- User confirms or provides a different version
- Validate new version is higher than current

### Step 2: Version Bump

Update version in these files:

1. **`OATControl/Properties/AssemblyInfo.cs`** — Update `AssemblyVersion` and `AssemblyFileVersion` attributes
2. **`OATControl/OATControl Setup.iss`** — Update the `AppVersion` line

### Step 3: Changelog + README

- Run `git log <last-tag>..HEAD --oneline` to get commits since last release tag
- Generate bullet points from commit messages
- Present to user for editing (both files use the same bullet text)
- Prepend to **`OATControl/CHANGELOG.md`** — fuller descriptions with markdown formatting
- Prepend to **`OATControl/README.txt`** — terse one-liners in existing format:
  ```
  OATControl V1.3.0.0                                        24 May 2026
  - One-line change summary.
  - Another change.
  ```

### Step 4: Commit & Tag

- Stage changed files (AssemblyInfo.cs, Setup.iss, CHANGELOG.md, README.txt)
- Commit with message: `Release V<version>`
- Create annotated tag: `V<version>` (e.g. `V1.3.0.0`)

### Step 5: Pause for Build

- Tell user to build OATControl in Release mode in Visual Studio
- Tell user to run InnoSetup to produce the installer
- Wait for user confirmation that `OATControl\bin\SetupOutput\OATControlSetup.exe` exists
- Verify the file exists before proceeding

### Step 6: Push & Create GitHub Draft Release

- Push commit and tag to remote (`git push && git push --tags`)
- Create GitHub draft release:
  ```
  gh release create V<version> --draft --title "V<version>" --notes-file <temp-changelog> "OATControl/bin/SetupOutput/OATControlSetup.exe"
  ```
- Report the draft release URL to user

## Files Modified

| File | What changes |
|------|-------------|
| `OATControl/Properties/AssemblyInfo.cs` | AssemblyVersion, AssemblyFileVersion |
| `OATControl/OATControl Setup.iss` | AppVersion |
| `OATControl/CHANGELOG.md` | Prepend new version section |
| `OATControl/README.txt` | Prepend new version block with one-liners |

## Error Handling

- **Pre-flight checks:** Verify `gh` CLI is available and authenticated. Verify git working tree is clean.
- **Version validation:** New version must be higher than current.
- **Build verification:** Check that installer .exe exists before creating release.
- **Stop on failure:** If any step fails, report the error and stop. Do not attempt partial recovery.

## Scope

This skill handles **OATControl only**. ASCOM Driver and OATSimulation have separate release processes.
