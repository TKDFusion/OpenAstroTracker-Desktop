---
name: release
description: Use when the user wants to create a release, make a release, ship, cut a release, or publish a new version of OATControl. Triggers on phrases like "make a release", "let's ship", "let's release", "create a release", "ship it", "cut a release", "new release", "/release".
---

# OATControl Release

Create a new OATControl release. Follow these steps in order. Stop and ask the user before proceeding if any step fails.

## Pre-flight

1. Verify `gh` CLI is available and authenticated (`gh auth status`)
2. Verify `curl` is available (`which curl`)
3. Verify git working tree is clean (`git status`)
4. If either fails, stop and tell the user what to fix

## Step 0: Verify Master Branch

Releases must always be cut from `master`. Check and handle the current branch:

1. Check current branch: `git branch --show-current`
2. If already on `master`, proceed to Step 1
3. If on a feature branch:
   - Check if an open PR exists: `gh pr list --head <branch> --state open`
   - If no PR exists, offer to create one: `gh pr create --base master --head <branch> --title "<title>" --body "<body>"`
   - If PR is open, tell the user to merge it first and stop
   - If PR exists but is not merged, tell the user to merge it first and stop
4. After confirming the PR is merged, switch to master and pull:
   ```
   git checkout master && git pull
   ```
5. Proceed to Step 1

## Step 1: Version

1. Read current version from `OATControl/Properties/AssemblyInfo.cs` line 54: `[assembly: AssemblyVersion("X.Y.Z.W")]`
2. Show the current version to the user
3. Propose the next version by bumping the minor segment (e.g. `1.2.0.0` → `1.3.0.0`)
4. Ask the user to confirm or provide a different version
5. Validate the new version is strictly higher than the current version
6. Store as `$VERSION` (e.g. `1.3.0.0`) and `$TAG` (e.g. `V1.3.0.0`)

## Step 2: Version Bump

Update these two files:

**`OATControl/Properties/AssemblyInfo.cs`** — Update both lines:
```
[assembly: AssemblyVersion("$VERSION")]
[assembly: AssemblyFileVersion("$VERSION")]
```

**`OATControl/OATControl Setup.iss`** — Update line 5:
```
#define MyAppVersion "$VERSION"
```

## Step 3: Changelog + README

1. Get the last release tag: `git tag --sort=-v:refname | grep -E '^V1\.' | head -1`
2. Get commits since that tag: `git log $LAST_TAG..HEAD --oneline`
3. Generate bullet points from the commit messages (combine related commits, make them concise)
4. Present the bullet list to the user for editing
5. Once approved, prepend to both files:

**`OATControl/CHANGELOG.md`** — Markdown format:
```markdown
## OATControl V$VERSION ($DATE)

- Change one.
- Change two.
```

**`OATControl/README.txt`** — Plain text format matching existing entries. Use the exact column alignment pattern:
```
OATControl V$VERSION                                         $DATE
- One-line change.
- Another change.
```
The date format is `%d %b %Y` (e.g. `24 May 2026`). The header line is 65 characters wide — pad with spaces so the date aligns to the right edge.

## Step 4: Commit & Tag

1. Stage only the changed files: `AssemblyInfo.cs`, `OATControl Setup.iss`, `CHANGELOG.md`, `README.txt`
2. Commit: `git commit -m "Release $TAG"`
3. Tag: `git tag -a $TAG -m "Release $TAG"`

## Step 5: Build Pause

Tell the user:

> **Build required.** Please do the following in Visual Studio:
> 1. Build OATControl in **Release** mode
> 2. Run **InnoSetup** on `OATControl/OATControl Setup.iss`
> 3. Confirm when done

Wait for the user to confirm, then verify the installer exists at `OATControl/bin/SetupOutput/OATControlSetup.exe`. If not found, ask the user to check.

## Step 6: Push & GitHub Draft Release

1. Push: `git push && git push --tags`
2. Create a temporary file with the changelog bullet points (just the bullets, no header) for release notes
3. Create draft release:
   ```
   gh release create $TAG --draft --title "OATControl $TAG Release" --notes-file <temp-file> "OATControl/bin/SetupOutput/OATControlSetup.exe"
   ```
4. Report the draft release URL to the user
5. Clean up the temporary notes file

## Step 7: Discord Announcement

1. Read the webhook URL from `.claude/settings.local.json` key `discordReleaseWebhook`
2. If the key is missing, ask the user for the webhook URL and save it to `.claude/settings.local.json`
3. Build the announcement message:
   ```
   @everyone A new OATControl version ($TAG) has been released. These are the main changes:

   - Bullet one.
   - Bullet two.

   Let us know if there are any issues.

   OpenAstroTech Team
   $RELEASE_URL
   ```
   Use the same user-facing bullet points from Step 3. The `$RELEASE_URL` is the GitHub release URL from Step 6.
4. POST to the webhook:
   ```bash
   curl -X POST "$WEBHOOK_URL" \
     -H "Content-Type: application/json" \
     -d '{"content": "<message with \\n for line breaks>"}'
   ```
5. Report success to the user

## Error Handling

- If any step fails, report the error clearly and stop
- Do not attempt partial recovery or skip steps
- The user can fix the issue and re-run `/release` from the beginning, or continue manually
