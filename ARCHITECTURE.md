# IFC Property Set DTO Library Architecture

> **For practical development guidance**, see [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Goals and Non-Goals

### Goals
- Provide an authoritative, machine-readable catalog of IFC property sets (Psets) derived from the maintained Excel workbook.
- Offer a lightweight C# DTO library so .NET projects can consume property sets as strongly typed objects.
- Publish neutral JSON and XML payloads so any programming language can reuse the same definitions.
- Automate validation so additions in the workbook propagate safely to every artifact.

### Non-Goals
- Implement complete IFC parsing/rendering or authoring tools; the scope is limited to property-set definitions and metadata.

## Design Principles

### Single Source of Truth
Domain experts maintain the Excel workbook as the authoritative catalog. The extraction pipeline reads from Excel, normalizes the data to a canonical schema, and generates all downstream artifacts (JSON, XML, DTO library). This prevents duplication and ensures consistency.

### Language Agnostic
By publishing canonical JSON/XML, any programming language can consume property sets without depending on .NET binaries. The schema is intentionally flat (no inheritance trees) to facilitate cross-language code generation.

### Maintainability via Automation
The canonical JSON is bundled as an embedded resource in the DTO assembly and synchronized with the source workbook via CI validation. Drift detection ensures the generated artifact stays synchronized with the workbook.

### Scalability
Adding new property sets only requires worksheet updates; the extraction pipeline regenerates all downstream artifacts automatically without code changes.

## Data Normalization Pipeline

```
Excel Workbook
    ↓
ClosedXML Extraction (CLI tool)
    ↓
Canonical In-Memory Model (PropertySetDto, PropertyDto)
    ↓
├─ JSON/XML Serialization → snacks.json
│      ↓
│  Embedded Resource in DTO Assembly
│
└─ Revit Shared Parameter Generator (--revit flag)
       ↓
   Revit Shared Parameter File → artifacts/Revit/snacksSharedParameters.txt
       +
   GUID Persistence → CLI/Data/revit-guid-mappings.json (version-controlled)
```

### Excel Workbook Structure
- **Source:** One worksheet per property set (e.g., `BIM_Modellinfo`)
- **Rows 1-3:** Metadata and column headers (see `.github/copilot-instructions.md` for column mapping)
- **Rows 4+:** Property data rows

### Canonical Schema
The in-memory model (POCOs like `PropertySetDto`, `PropertyDto`) serves as the single contract for all exporters (JSON, XML, DTO library). Validation ensures:
- Duplicate property codes are rejected
- Mandatory columns are present
- Datatype normalization to IFC primitives
- Delimiter normalization (see copilot-instructions.md for detailed rules)

### Extraction & Validation Steps
1. Parse Excel sheet with ClosedXML
2. Extract metadata from rows 1-2
3. Validate column headers in row 3
4. For each data row: create PropertyDto, split Code/DisplayName, normalize delimiters
5. Validate: duplicate codes, required fields, enum values
6. Serialize to canonical JSON
7. Deserialize and re-validate with System.Text.Json (ensures round-trip compatibility)

## DTO Library (C#)

**Location:** `SnacksDto/src/`

**Key Types:**
- `PropertySetDto` – Immutable record representing a property set
- `PropertyDto` – Immutable record representing a single property definition
- `PropertySetRepository` – Loads bundled JSON and exposes lookup APIs

**Serialization Strategy:**
- Canonical JSON embedded as a resource in the assembly
- Lazy deserialization via `System.Text.Json` (built-in; no external JSON library needed)
- Case-insensitive JSON property matching

## Build and Release Flow

### CLI Pipeline
The CLI extraction tool (`CLI/CLI.csproj`) supports multiple output formats:

1. **JSON Output (default):** `snacks.json` - Canonical property set definitions
2. **Revit Shared Parameters (--revit flag):**
   - Generates `artifacts/Revit/snacksSharedParameters.txt` in Revit's tab-delimited format
   - Maps IFC datatypes to Revit parameter types (TEXT, YESNO, INTEGER, NUMBER, LENGTH, etc.)
   - Maintains stable GUIDs via `CLI/Data/revit-guid-mappings.json` (version-controlled) to prevent breaking existing Revit projects
   - Groups parameters by Excel sheet names for organizational clarity

### CI Pipeline
1. Extract from Excel workbook → canonical JSON
2. Validate: does new JSON match repo copy? (fail if drift detected)
3. Run unit & integration tests
4. Build DTO library NuGet package (includes embedded JSON)
5. Generate JSON/XML artifacts for release

### Versioning
- Semantic versioning driven by workbook changes
- Minor: additive property sets or properties
- Major: breaking schema changes (renamed fields, deleted entities, type changes)
- Patch: data corrections, documentation updates

### Release Artifacts
- NuGet package (SnacksDto) containing the DTO library with embedded JSON
- JSON and XML files published as GitHub Release assets
- Changelog documenting property-set changes
