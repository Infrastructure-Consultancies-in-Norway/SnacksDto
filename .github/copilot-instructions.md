# Copilot Instructions for SnacksDto

## Overview
SnacksDto is a .NET 8 library that provides Data Transfer Objects for IFC Property Sets. The project extracts property definitions from an Excel workbook, normalizes them to a canonical schema, and publishes them as strongly-typed C# DTOs plus JSON/XML exports for language-agnostic consumption.

## Project Structure
```
SnacksDto/
├─ SnacksDto/                  # DTO library (net8.0)
│  ├─ artifacts/               # Generated snacks.json (embedded resource)
│  └─ data/                    # Placeholder for future Excel source
├─ CLI/                        # Extraction tool (net8.0 console app)
│  ├─ Services/
│  │  └─ WorkbookExtractor.cs  # Reads Excel workbook via ClosedXML
│  ├─ Models/                  # DTOs for property sets and definitions
│  └─ Program.cs               # Entry point with command-line argument parsing
├─ CLI.Tests/                  # xUnit tests for CLI tool
├─ SnacksDtop.Tests/           # xUnit tests for JSON serialization
└─ SnacksDto.sln              # Solution file
```

## Build and Test Commands

### Build the entire solution
```
dotnet build SnacksDto/SnacksDto.sln
```

### Run tests
```
dotnet test SnacksDto/SnacksDto.sln
```

### Run a specific test file
```
dotnet test SnacksDto/SnacksDtop.Tests/SnacksDtop.Tests.csproj
dotnet test SnacksDto/CLI.Tests/CLI.Tests.csproj
```

### Run the CLI extraction tool
```
dotnet run --project SnacksDto/CLI/CLI.csproj -- --workbook <path> --output <path>
```

### View CLI help
```
dotnet run --project SnacksDto/CLI/CLI.csproj -- --help
```

## Key Architecture Concepts

### The Data Pipeline
1. **Excel Workbook** → Input source of truth, one worksheet per property set
2. **CLI Extraction** → `WorkbookExtractor` uses ClosedXML to parse sheets and enforce schema rules
3. **Canonical Model** → In-memory DTOs representing normalized property sets/definitions
4. **JSON Serialization** → Canonical model serialized as `snacks.json`
5. **DTO Library** → `SnacksDto` project embeds the generated JSON as an embedded resource

### Property Set Structure
- Each worksheet in the Excel file represents one property set (e.g., `BIM_Modellinfo`)
- Metadata row 1: Human-readable name and scope (e.g., "BIM_Modellinfo (gjelder alle fag)")
- Metadata row 2: Discipline classification (e.g., "Felles")
- Header row 3: Column names (`Egenskapsnavn`, `Datatype`, `Nivå`, `Eksempelverdi`, etc.)
- Data rows 4+: Individual property definitions

### Column Mappings (Excel → DTO)
- **B (Egenskapsnavn)** → Code + DisplayName (split on " - ")
- **C (Datatype)** → IFC primitive type (IfcText, IfcInteger, etc.)
- **D (Nivå)** → ApplicableEntities (IFC entity names or "Obj" or "*" for all)
- **E (Eksempelverdi)** → SampleValue
- **F (Anbefalte verdier)** → RecommendedValues (comma-separated)
- **G (Tillatte verdier)** → AllowedValues ("*" = unrestricted)
- **H (Tillatte verdier - Regex)** → AllowedPattern (regex or reference)
- **I (Forklaring)** → Description (Markdown-safe)
- **J (Erfaringer)** → Notes (optional lessons learned)
- **K (Farge)** → StatusColor (enum: Green/Gray/Yellow)

## Conventions

### Naming
- DTO records use `Dto` suffix (e.g., `PropertySetDto`, `PropertyDto`)
- Services use `*Service` or `*Extractor` pattern
- Test classes: `[TestedClass]Tests` or `[Feature]Tests`

### Null Handling
- Codebase uses nullable reference types (`<Nullable>enable</Nullable>`)
- Optional properties are marked `string?` or `List<T>?`
- Tests use `Assert.NotNull()` for required properties

### Data Validation
- Duplicate property codes within a set are rejected (see `SnacksJson.cs` test)
- `Requirement` field: 0=optional, 1=recommended, 2=mandatory (see `SnacksDtop.Tests`)
- `snacks.json` must be deserializable by standard `System.Text.Json` with case-insensitive options

### Testing Framework
- xUnit (Fact/Theory attributes)
- Assertions: Assert class methods
- No global setup; tests are self-contained
- Test discovery: Files named `*Tests.cs` or `*Test.cs`

## Dependencies
- **ClosedXML 0.104.0** – Excel workbook parsing (CLI only)
- **xunit 2.9.3** – Testing framework
- **Microsoft.NET.Test.Sdk 17.14.1** – Test runtime
- **System.Text.Json** – JSON serialization (built-in)

## Common Tasks

### Adding a new property set
1. Add a new worksheet to the Excel workbook with the standard header structure
2. Run the CLI extraction tool to regenerate `snacks.json`
3. Update `snacks.json` in `SnacksDto/artifacts/`
4. Run tests to verify deserialization

### Modifying column mappings
- Edit `WorkbookExtractor.cs` in the CLI project
- Update the enum or constant that defines expected column headers
- Ensure backward compatibility with existing workbooks
- Add corresponding DTO field if introducing new data

### Changing the DTO schema
- Modify record definitions in the CLI Models folder
- Regenerate `snacks.json`
- Update test expectations in `SnacksDtop.Tests`
- Versioning implications: breaking changes warrant major version bump

## Testing Best Practices
- **Unit tests** verify individual extractions (CLI.Tests)
- **Integration tests** validate the entire JSON artifact (SnacksDtop.Tests)
- Always test for empty/null inputs and duplicate detection
- Requirement field validation is critical: tests check for valid codes (0/1/2)
