# IFC Property Set DTO Library Architecture

## Goals and Non-Goals
- Provide an authoritative, machine-readable catalog of IFC property sets (Psets) derived from the maintained Excel workbook.
- Offer a lightweight C# DTO library so .NET projects can consume property sets as strongly typed objects.
- Publish neutral JSON and XML payloads so any programming language can reuse the same definitions.
- Automate validation so additions in the workbook propagate safely to every artifact.
- **Non-goals:** Implement complete IFC parsing/rendering or authoring tools; the scope is limited to property-set definitions and metadata.

## Source of Truth
1. **Excel Workbook (one worksheet per Pset):** Authoring surface for domain experts.
2. **Workbook Conventions:** Each sheet should follow a common header schema (e.g., `PropertyName`, `Description`, `DataType`, `Units`, `Constraints`).
3. **Version Tags:** Store workbook version/date in a dedicated "Metadata" sheet so downstream artifacts can include provenance information.

## Workbook Example (`SnacksDto\SnacksDto\data\EgenskapsstrukturV10.xlsx`)
### Sheet anatomy (BIM_Modellinfo & BIM_Tverrfaglig)
| Cell/Column | Example | Purpose | DTO target |
|-------------|---------|---------|-----------|
| B1 | `BIM_Modellinfo (gjelder alle fag)` | Human title + scope hint | `PropertySetDefinition.DisplayName` + `ScopeNote` |
| B2 | `Felles` | Discipline/usage scope | `PropertySetDefinition.Discipline` |
| Row 3 (B→K) | `Egenskapsnavn`, `Datatype`, `Nivå`, `Eksempelverdi`, `Anbefalte verdier`, `Tillatte verdier`, `Tillatte verdier (Regex)`, `Forklaring`, `Erfaringer`, `Farge` | Column headers for every property row | Maps to `PropertyDefinition` fields |
| Rows ≥4 | Property data (e.g., `MOD.01 - SNACks-versjon`) | Actual property definitions | Serialized DTO instances |

### Column mapping → DTO fields
| Column | Header | DTO field | Notes |
|--------|--------|-----------|-------|
| B | Egenskapsnavn | `PropertyDefinition.DisplayName` and `Code` (split on `-`) | Keep both human text and identifier prefix (`MOD.01`). |
| C | Datatype | `PropertyDefinition.Datatype` | Normalize to IFC primitive enum (`IfcText`, `IfcInteger`, …). |
| D | Nivå | `PropertyDefinition.ApplicableEntities` | Split on `/` or `,` (`IfcBuilding/IfcBridge`, `Obj`). `*` = applies to all. |
| E | Eksempelverdi | `PropertyDefinition.SampleValue` | Optional string. |
| F | Anbefalte verdier | `PropertyDefinition.RecommendedValues` | Split on comma; trim. |
| G | Tillatte verdier | `PropertyDefinition.AllowedValues` | `*` indicates unrestricted. |
| H | Tillatte verdier (Regex) | `PropertyDefinition.AllowedPattern` | Preserve literal regex; treat `[Se ark …]` as reference metadata. |
| I | Forklaring | `PropertyDefinition.Description` | Markdown-friendly text. |
| J | Erfaringer | `PropertyDefinition.Notes` | Optional lessons learned. |
| K | Farge | `PropertyDefinition.StatusColor` | Map to enum (Green/Gray/Yellow) if desired. |

### Extraction steps for each property set
1. Load the sheet with `ClosedXML.Workbook`. Skip preface rows until the header row containing `Egenskapsnavn` is found.
2. Capture sheet-level metadata: `Identifier = sheet.Name`, `DisplayName = value in B1 stripped of scope suffix`, `ScopeNote = text within parentheses in B1`, and `Discipline = value in B2`.
3. Iterate rows starting at headerRow + 1 until column B is empty for N consecutive rows.
4. For each row, instantiate `PropertyDefinition` using the column mapping above. Split combined fields (`MOD.01 - SNACks-versjon`) into `Code` (`MOD.01`) and `DisplayName` (`SNACks-versjon`).
5. Normalize delimiters:
   - `/` separates IFC entities (`IfcBuilding/IfcBridge`).
   - `,` separates enumerations in columns F/G.
   - `*` means "any value" and should result in an empty restriction list plus `AllowAny=true` flag.
6. Persist the resulting `PropertySetDefinition` → add to canonical collection → serialize to JSON/XML → embed in the DTO assembly.

### Sample canonical payload (trimmed)
```json
{
  "name": "BIM_Modellinfo",
  "discipline": "Felles",
  "scope": "gjelder alle fag",
  "properties": [
    {
      "code": "MOD.01",
      "displayName": "SNACks-versjon",
      "datatype": "IfcText",
      "applicableTo": ["IfcBuilding", "IfcBridge"],
      "sampleValue": "v1.0",
      "recommendedValues": ["v1.0"],
      "allowedValues": ["v1.0"],
      "allowedPattern": "v1.0",
      "description": "Versjonsnummer for gjeldende versjon brukt i modellen",
      "color": "Grønn"
    },
    {
      "code": "BIM.04",
      "displayName": "MMI",
      "datatype": "IfcText",
      "applicableTo": ["Obj"],
      "sampleValue": "350",
      "recommendedValues": ["000", "100", "200", "300", "350", "375", "400", "500"],
      "allowedValues": ["Verdier iht. MMI 2.0 veileder"],
      "allowedPattern": "^\\d{3}$",
      "description": "Objektets MMI status, definisjonen bør følge RIFs MMI definisjon",
      "color": "Grønn"
    }
  ]
}
```

### From canonical JSON to `SnacksDto` objects (C# sketch)
```csharp
public sealed record PropertySetDto(
    string Name,
    string Discipline,
    string Scope,
    IReadOnlyList<PropertyDto> Properties);

public sealed record PropertyDto(
    string Code,
    string DisplayName,
    string Datatype,
    IReadOnlyCollection<string> ApplicableTo,
    string? SampleValue,
    IReadOnlyCollection<string> RecommendedValues,
    IReadOnlyCollection<string> AllowedValues,
    string? AllowedPattern,
    string? Description,
    string? Notes,
    string? StatusColor);

var modellinfo = new PropertySetDto(
    Name: "BIM_Modellinfo",
    Discipline: "Felles",
    Scope: "gjelder alle fag",
    Properties: new[]
    {
        new PropertyDto(
            Code: "MOD.01",
            DisplayName: "SNACks-versjon",
            Datatype: "IfcText",
            ApplicableTo: new[]{"IfcBuilding","IfcBridge"},
            SampleValue: "v1.0",
            RecommendedValues: new[]{"v1.0"},
            AllowedValues: new[]{"v1.0"},
            AllowedPattern: "v1.0",
            Description: "Versjonsnummer for gjeldende versjon brukt i modellen",
            Notes: null,
            StatusColor: "Grønn"),
        new PropertyDto(
            Code: "BIM.04",
            DisplayName: "MMI",
            Datatype: "IfcText",
            ApplicableTo: new[]{"Obj"},
            SampleValue: "350",
            RecommendedValues: new[]{"000","100","200","300","350","375","400","500"},
            AllowedValues: new[]{"Verdier iht. MMI 2.0 veileder"},
            AllowedPattern: "^\\d{3}$",
            Description: "Objektets MMI status, definisjonen bør følge RIFs MMI definisjon",
            Notes: null,
            StatusColor: "Grønn")
    });
```

## Data Normalization Pipeline
```
Excel workbook → Extraction service → Canonical schema → DTO generation & JSON/XML serialization
```
1. **Extraction Service (C# console tool):** Uses `ClosedXML`/`EPPlus` to read worksheets, enforce schema rules, and emit a canonical in-memory model.
2. **Canonical Schema:** Internal POCOs such as `PropertySetDefinition`, `PropertyDefinition`, `AllowedValue`, `Unit`. This schema is the single contract implemented by every exporter.
3. **Validation:** During extraction run structural validation (mandatory columns, unique property names per Pset, datatype normalization) plus optional semantic rules (unit compatibility, enum coverage).
4. **Diffing:** Persist the canonical model as `psets.canonical.json`; CI compares this file against `main` to highlight workbook changes.

## DTO Library (C#)
- **Assembly:** `SnacksDto.IfcPropertySets` with namespaces like `SnacksDto.Ifc.PropertySets`.
- **Types:**
  - Immutable records (`PropertySetDto`, `PropertyDto`, `ConstraintDto`).
  - Helper enums for IFC primitive types and unit systems.
  - `PropertySetRepository` that loads bundled JSON and exposes lookup APIs (`GetByName`, `FindByIfcClass`).
- **Serialization:** Bundle the canonical JSON as an embedded resource; lazily deserialize via `System.Text.Json` to avoid code generation churn.
- **Extensibility:** Interfaces (`IPropertySetSerializer`) so consumers can plug in alternative persistence layers.

## Neutral Data Artifacts
1. **JSON:** Primary interchange file (`psets.json`) following the canonical schema and version metadata.
2. **XML:** Generated from the same in-memory model using `System.Xml.Linq`. Retain identical field names to keep parity with JSON.
3. **Schema Definitions:** Optional JSON Schema / XSD files to give consumers contract validation.
4. **Packaging:** Publish artifacts alongside each release (NuGet package for DTO assembly, GitHub Release assets for raw JSON/XML).

## Cross-Language Consumption
- Languages without .NET can pull `psets.json`/`psets.xml` directly and hydrate their own DTOs.
- Provide concise documentation snippets (TypeScript, Python, Java) showing how to deserialize and map to local types.
- Keep the canonical schema intentionally simple (no inheritance trees) to make downstream generation straightforward.

## Build and Release Flow
1. **CI Pipeline:**
   - Step 1: Run extraction tool against the Excel workbook committed in `data/psets.xlsx`.
   - Step 2: Compare generated canonical JSON with repo copy; fail if drift is detected.
   - Step 3: Generate JSON, XML, and (optionally) Markdown tables for docs.
   - Step 4: Build & test the DTO library, then pack a NuGet artifact embedding the freshly generated JSON.
2. **Versioning:** Semantic versioning driven by workbook changes (minor for additive Psets/properties, major for breaking edits).
3. **Release Artifacts:** NuGet package + zipped JSON/XML files + changelog entry summarizing property-set changes.

## Suggested Repository Layout
```
SnacksDto/
├─ data/
│  └─ EgenskapsstrukturV10.xlsx               # Source workbook
├─ tools/
│  └─ Extractor/               # C# console app that emits canonical JSON/XML
├─ src/
│  └─ SnacksDto/               # DTO library
├─ artifacts/
│  ├─ psets.json
│  └─ psets.xml
├─ ARCHITECTURE.md             # This document
└─ README.md
```

## Why This Approach Works
- **Single source of truth:** Domain experts continue editing Excel while the codebase consumes a normalized representation, reducing duplication.
- **Language agnostic:** JSON/XML exports mean any ecosystem can consume the definitions without depending on .NET binaries.
- **Maintainability:** Embedding generated resources keeps the DTO assembly synchronized with the released data, and CI validation prevents silent drift.
- **Scalability:** Adding new Psets only requires worksheet updates; the pipeline regenerates all downstream artifacts automatically.
