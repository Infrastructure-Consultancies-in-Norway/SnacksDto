# Release Guide for SnacksDto

## One-Time Setup (First Release Only)

### 1. Create NuGet API Key
1. Go to https://www.nuget.org
2. Sign in to your account
3. Go to Account → API Keys
4. Click "Create" to generate a new key
5. Set scopes: "Push version 0.1.0-beta" and "Push new versions and version updates"
6. Copy the generated API key

### 2. Add GitHub Repository Secret
1. Go to your repository on GitHub
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. **Name:** `NUGET_API_KEY`
5. **Value:** Paste the API key from nuget.org
6. Click "Add secret"

> **Security Note:** The API key will not be visible after saving. Store it securely elsewhere as backup.

## Creating a Release

### Step 1: Commit and Tag

```bash
# Create an annotated git tag (matches pattern v*.*.*)
git tag v0.1.0-beta -m "Release 0.1.0-beta"

# Push the tag to GitHub
git push origin v0.1.0-beta
```

### Step 2: Watch the Workflow

1. Go to your repository on GitHub
2. Click the "Actions" tab
3. Find the "Release" workflow run (should appear within seconds)
4. Wait for the workflow to complete

**The workflow will:**
- ✅ Run all tests (must pass)
- ✅ Build the project in Release mode
- ✅ Generate artifacts:
  - `snacks.json` (canonical JSON)
  - `snacksSharedParameters.txt` (Revit parameters)
  - `objects_*.inp` files (Tekla objects)
- ✅ Create a GitHub Release with all artifacts
- ✅ Publish NuGet package to nuget.org

### Step 3: Verify Release

**On GitHub:**
1. Go to "Releases" page
2. Find the new release (e.g., "Release 0.1.0-beta")
3. Verify all assets are attached:
   - `snacks-artifacts.zip`
   - `snacks.json`
   - `snacksSharedParameters.txt`

**On NuGet.org:**
1. Go to https://www.nuget.org/packages/SnacksDto
2. Look for the new version (may take 1-2 minutes to index)
3. Installation command should appear:
   ```
   dotnet add package SnacksDto --version 0.1.0-beta
   ```

## Version Numbering

Follow semantic versioning: `MAJOR.MINOR.PATCH[-SUFFIX]`

### Examples:
- **Beta releases:** `v0.1.0-beta`, `v0.2.0-beta`, `v0.5.0-beta`
- **Stable releases:** `v1.0.0`, `v1.1.0`, `v2.0.0`
- **Patch releases:** `v1.0.1`, `v1.0.2`

### Guidelines:
- **MAJOR:** Breaking changes (schema restructuring, deleted properties)
- **MINOR:** New property sets or properties added (additive)
- **PATCH:** Bug fixes, data corrections, documentation updates
- **SUFFIX:** `-beta`, `-rc1`, `-alpha` for pre-release versions

## Troubleshooting

### Workflow Failed at Tests
- Check the test logs in the workflow run
- Fix the failing tests locally
- Run `dotnet test SnacksDto.sln` to verify
- Try creating the release again

### Workflow Failed at Release Creation
- Check if GitHub has permission to create releases (should be automatic)
- Verify the tag name matches pattern `v*`

### Workflow Failed at NuGet Publish
- Check if `NUGET_API_KEY` secret is configured (Settings → Secrets)
- Verify the API key has "Push" permission on nuget.org
- Check if package version already exists on nuget.org (must be unique)

### Release Created but Package Not on NuGet
- NuGet indexing can take 1-5 minutes
- Check https://www.nuget.org/packages/SnacksDto again in a few minutes
- If still missing, check the "Publish to NuGet" step in the workflow logs

## Post-Release

### Update Documentation
After releasing, consider updating:
- `README.md` with new version notes
- `CHANGELOG.md` with release details
- Any documentation referencing old versions

### Announce the Release
- Add release notes on the GitHub Releases page
- Post on relevant community channels
- Update version links in documentation

## Rollback (if needed)

If a release has issues and needs to be retracted:

```bash
# Delete the local tag
git tag -d v0.1.0-beta

# Delete the remote tag
git push origin --delete v0.1.0-beta
```

Then contact NuGet support to unlist the package if needed.
