# 🧰 TCSTools – Tweaking Cat Studios Internal Tooling

Studio-maintained Unity tools packaged as independent **UPM packages** for use across all TCS projects.

---

## 📦 Repository Layout
```
TCSTools/
├─ packages/
│  ├─ com.tcs.tools.adminwindow/
│  ├─ com.tcs.tools.scriptableobjectinspector/
│  └─ com.tcs.tools.projectplus/
├─ build/                  # pack & verify scripts
├─ dist/                   # output .tgz bundles
└─ README.md               # (this file)
```

Each subfolder under `packages/` is a fully-formed **Unity package** with its own  
`package.json`, `Runtime/`, `Editor/`, `Samples~/`, and `Documentation~/` folders.

---

## 🧑‍💻 Local Development Workflow

### Prerequisites
- Unity 6000.x LTS or newer
- “Visible Meta Files” enabled in Unity (for GUID stability)
- OPTIONAL: Node.js 18+ (for `npm pack`)
- OPTIONAL: PowerShell 7+ or Bash (for build scripts)

### Folder Relationship
Every developer should clone both repos as **siblings**. For example:

```
Documents/
  TCS/
    MainProject/      ← Unity project root
    TCSTools/         ← this repo
```

---

## 🔨 Building & Packaging
From the **TCSTools** root:

```bash
# macOS / Linux
bash build/pack-all.sh

# Windows
powershell build/pack-all.ps1
```

Each package is packed using `npm pack` and placed under:

```
dist/<package-name>/<version>/<package-name>-<version>.tgz
```

Example:
```
dist/com.tcs.tools.adminwindow/1.1.0/com.tcs.tools.adminwindow-1.1.0.tgz
```

---

## 🚀 Using the Packages in a Unity Project

### Option 1 – Active Development (Recommended)
Reference the packages **relatively** in the Unity project’s `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.tcs.tools.adminwindow": "file:../../TCSTools/packages/com.tcs.tools.adminwindow",
    "com.tcs.tools.scriptableobjectinspector": "file:../../TCSTools/packages/com.tcs.tools.scriptableobjectinspector",
    "com.tcs.tools.projectplus": "file:../../TCSTools/packages/com.tcs.tools.projectplus"
  }
}
```

> `file:` paths are resolved **relative to the `Packages/` folder**,  
> so `../TCSTools/...` points to the sibling repo.

After editing, delete `Packages/packages-lock.json` and reopen Unity.  
Each tool should appear in **Package Manager → In Project** as *Local*.

---

### Option 2 – Frozen Release (Immutable Build)
Use the packed `.tgz` files from `dist/` and reference them relatively:

```json
{
  "dependencies": {
    "com.tcs.tools.adminwindow": "file:ThirdParty/tcstools/com.tcs.tools.adminwindow-1.1.0.tgz"
  }
}
```

Locks the version — ideal for CI or milestone branches.

---

### Option 3 – Embedded Copy
Copy the entire package folder into the Unity project’s `Packages/` directory:

```
BellumQuest/Packages/com.tcs.tools.adminwindow/
```

Unity treats it as **Embedded** and ignores any `file:` or registry references.

---

## 🧱 Versioning & Changelog Policy
Each package follows **Semantic Versioning**:

| Type | Meaning |
|------|----------|
| **MAJOR** | Breaking API change |
| **MINOR** | Additive features (back-compatible) |
| **PATCH** | Bug fixes |

Steps for release:
1. Update `version` in the package’s `package.json`.
2. Add an entry to its `CHANGELOG.md`.
3. Run `build/pack-all` to produce `.tgz` files.
4. Commit + tag (e.g. `com.tcs.tools.adminwindow@1.1.0`).

---

## 🧪 Verification Checklist
- [ ] Package compiles in a blank Unity project
- [ ] No `UnityEditor.*` in Runtime assemblies
- [ ] Samples import correctly
- [ ] Docs (`Documentation~/index.md`) updated
- [ ] Version & changelog bumped

---

## 🧰 Build Automation (Optional CI)
You can run the same scripts in UVCS/Unity DevOps to:
1. Run tests for each package
2. Generate `.tgz` bundles
3. Upload artifacts or attach to a release

---

## 🪄 Common Fixes
| Problem | Fix |
|----------|-----|
| “Could not find package.json” | Check the relative path in `manifest.json` starts with `../TCSTools/` |
| “Access denied” during resolve | Close Unity, delete `Library/PackageCache`, reopen Unity (pause OneDrive/AV if needed) |
| Unity still using old paths | Delete `Packages/packages-lock.json` and `Library/PackageCache`, reopen Unity |
| Broken sample GUIDs | Commit `.meta` files for all assets under `Samples~/` |

---

## 🤝 Contributing New Tools
1. Create a new folder under `packages/` named `com.tcs.tools.<toolname>`
2. Add `package.json`, `Runtime/`, `Editor/`, `Samples~/`, `Documentation~/`
3. Run `build/pack-all` to validate
4. Commit & verify in a consumer project

---

**© 2025 Tweaking Cat Studios**  
_Internal use only – not for external distribution_
