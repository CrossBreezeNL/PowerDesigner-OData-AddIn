# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A PowerDesigner 16 ActiveX (COM) Add-In, written in C# (.NET Framework 4.7.2, x64), that reverse engineers an OData `$metadata` document into a PowerDesigner Physical Data Model (PDM), and later updates that PDM from the same metadata.

## Build

Windows only. There are no automated tests and no linter.

```
msbuild -t:restore
msbuild PowerDesigner_OData_AddIn.sln -property:Configuration=Release
```

- **Debug build**: a post-build event runs `reg import Register_Debug_AddIn.reg /reg:64`, which registers the add-in under `HKLM\SOFTWARE\Sybase\PowerDesigner 16\Addins\OData`. This needs an elevated (Administrator) Visual Studio/shell. The `.reg` file hardcodes the DLL path `C:\GIT\GitHub\PowerDesigner-OData-AddIn\...\bin\Debug\...`, so adjust it if your clone lives elsewhere.
- Debug also sets `RegisterForComInterop=true`, which needs admin rights too.
- **Installer (MSI)**: `PowerDesigner_OData_AddIn_Setup/*.vdproj` is a Visual Studio Installer Project. It can only be built with `devenv.com ... /Build Release /project PowerDesigner_OData_AddIn_Setup`, not with msbuild.
- CI (`.github/actions/build-addin/action.yml`) uses GitVersion to stamp `AssemblyInfo.cs` and the vdproj `ProductVersion`. The `main` branch builds the MSI by default, and `feature/*` branches build only the assembly. Do not bump versions by hand.

## Manual testing / debugging

Testing means running it in PowerDesigner. Build Debug, enable the "OData" add-in under Tools > General Options > Add-Ins (you may need to run PowerDesigner as admin), and define a PowerDesigner variable `ODATA_DEBUG_MODE=True` (General Options > Variables) so that `PdLogger.Debug` output appears in the Output window. Debug mode is evaluated against the *active model* (`EvaluateText("%$ODATA_DEBUG_MODE%")`), so no debug output is logged when no model is open.

## Architecture

All source is in `PowerDesigner_OData_AddIn/` (namespace `CrossBreeze.Tools.PowerDesigner.AddIn.OData`).

- **`PdODataAddIn`**: the COM entry point (`IPdAddIn`, fixed `Guid`, so don't change it). PowerDesigner calls `ProvideMenuItems` / `IsCommandSupported` / `DoCommand`:
  - Menu `Reverse` with `pObject == null` → "OData..." → `PdODataModelUpdater.CreatePdmModel()`.
  - Menu `Object` on a PDM model that has the OData metadata file → "Update model from OData metadata" → `UpdatePdmModel()`.
  - Every interface method wraps its body in try/catch and logs to the PowerDesigner output window, because exceptions that cross the COM boundary are not handled and disable the add-in. Keep this pattern for any new entry point.
- **`PdODataModelUpdater`**: orchestration.
  - The OData source is persisted *in the model* as a `FileObject` with Code `OData_Metadata` (Location = metadata URL, LocationType = 2/URL, Generated = false). Its **`Comment` field stores the auth type** (`None`/`Basic`, matching the `ODataAutenticationType` enum). Credentials are never stored and are prompted for through `PdODataBasicAuthenticationForm`.
  - Fetches metadata with `WebRequest` (forced TLS 1.2), validates the `Edmx` root and `Version` attribute, then dispatches by version: **v4 → `PdODataV4Helper`** (uses `Microsoft.OData.Edm` 7.x), **v1–3 → `PdODataV3Helper`** (uses `Microsoft.Data.Edm` 5.x). The two helpers mirror each other, so a mapping change usually has to be made in both.
  - **Update flow**: creates a hidden temporary model, copies `ModelOptionsText` and the DBMS from the target model (the DBMS is set only when it differs, because setting the same DBMS breaks the merge), runs the same import into it, then calls `pdmModel.Merge(...)` with all changes except deletes/removes. After that it deletes the temporary model.
- **V3/V4 helper mapping** (`CreatePdmObjects`):
  - Enum types → PDM physical domains (int, list of values).
  - Entity/Complex types → tables named `Namespace.TypeName`, placed in a package per namespace and owned by a user per namespace. They get Stereotype = EDM type kind and `Generated = false`.
  - Navigation properties → references between the type tables.
  - Entity sets → **replica tables** of their entity-type table (Stereotype `EntitySet`, with some attributes and collections excluded from replication). Navigation property bindings → references between the replica tables.
- **`PdHelper`**: get-or-create/find helpers for packages, users, tables and views, plus `UpdateDiagramRecursively`. **`PdAppHelper.GetPDWindow`**: the owner window for modal WinForms dialogs. **`PdLogger`**: Info/Error/Debug to the PowerDesigner output window.
- PowerDesigner interop assemblies (`Interop.PdAddInTypLib`, `PdCommon`, `PdPDM`) are checked into `PowerDesigner_OData_AddIn/lib/` with `EmbedInteropTypes=True`.
