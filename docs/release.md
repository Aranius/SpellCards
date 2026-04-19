# Release Guide

This is a small checklist for cutting a new `SpellCards` release.

## Automated GitHub workflow
The repository includes `.github/workflows/release.yml`.

- Push a tag like `v1.2.3` to build release packages automatically and attach them to a GitHub release.
- Or run the workflow manually from the Actions tab to produce the same zip artifacts without publishing a tagged release.

The workflow:
- restores, builds, and tests the solution
- publishes `win-x64`, `linux-x64`, and `osx-x64`
- creates both `framework` and `selfcontained` zip packages
- generates matching `*.sha256sum` files
- uploads all assets to the workflow run

## 1. Verify the repo state
```powershell
git status
dotnet restore
dotnet build
```

Run the automated tests before packaging:
```powershell
dotnet test
```

## 2. Update version metadata
Set the release version in `SpellCards/SpellCards.csproj` if needed.

Suggested git flow:
```powershell
git add -A
git commit -m "Prepare release vX.Y.Z"
git tag -a vX.Y.Z -m "vX.Y.Z"
```

## 3. Publish platform packages
Framework-dependent:
```powershell
dotnet publish SpellCards/SpellCards.csproj -c Release -r win-x64   -o artifacts/publish/win-x64/framework   --self-contained false
dotnet publish SpellCards/SpellCards.csproj -c Release -r linux-x64 -o artifacts/publish/linux-x64/framework --self-contained false
dotnet publish SpellCards/SpellCards.csproj -c Release -r osx-x64   -o artifacts/publish/osx-x64/framework   --self-contained false
```

Self-contained:
```powershell
dotnet publish SpellCards/SpellCards.csproj -c Release -r win-x64   -o artifacts/publish/win-x64/selfcontained   --self-contained true
dotnet publish SpellCards/SpellCards.csproj -c Release -r linux-x64 -o artifacts/publish/linux-x64/selfcontained --self-contained true
dotnet publish SpellCards/SpellCards.csproj -c Release -r osx-x64   -o artifacts/publish/osx-x64/selfcontained   --self-contained true
```

## 4. Zip the published folders
```powershell
Compress-Archive -Path artifacts/publish/win-x64/framework/*       -DestinationPath artifacts/publish/win-x64-framework.zip -Force
Compress-Archive -Path artifacts/publish/win-x64/selfcontained/*   -DestinationPath artifacts/publish/win-x64-selfcontained.zip -Force
Compress-Archive -Path artifacts/publish/linux-x64/framework/*     -DestinationPath artifacts/publish/linux-x64-framework.zip -Force
Compress-Archive -Path artifacts/publish/linux-x64/selfcontained/* -DestinationPath artifacts/publish/linux-x64-selfcontained.zip -Force
Compress-Archive -Path artifacts/publish/osx-x64/framework/*       -DestinationPath artifacts/publish/osx-x64-framework.zip -Force
Compress-Archive -Path artifacts/publish/osx-x64/selfcontained/*   -DestinationPath artifacts/publish/osx-x64-selfcontained.zip -Force
```

## 5. Generate SHA-256 checksum files
Use the helper script already in the repo:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File artifacts/publish/write-sha256sum.ps1
```

This creates one `*.sha256sum` file next to each zip.

## 6. Optional source archive
Tracked files only:
```powershell
git archive --format=tar.gz -o spellcards-src.tar.gz HEAD
```

Working tree snapshot:
```powershell
tar -czf spellcards-src.tar.gz --exclude=artifacts --exclude=bin --exclude=obj --exclude=.git .
```

## 7. Push and create the GitHub release
```powershell
git push origin master
git push origin --tags
```

If you push a `v*` tag, the GitHub release workflow can handle the packaging and asset upload automatically.

Then create a GitHub release and upload:
- `artifacts/publish/*-framework.zip`
- `artifacts/publish/*-selfcontained.zip`
- `artifacts/publish/*.sha256sum`
- optional `spellcards-src.tar.gz`

## Notes
- Generated PDFs now use timestamped filenames under `out/`, so an already-open PDF does not block a new export.
- `custom-spells.json` is part of the published app and can override built-in sources.
- `5e` uses Open5e `srd-2014`; `5.5` uses Open5e `srd-2024`.
