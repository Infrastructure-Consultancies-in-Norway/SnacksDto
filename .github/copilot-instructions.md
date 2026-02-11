# Copilot Instructions for SnacksDto

## Overview
SnacksDto is a .NET 8 library that provides Data Transfer Objects for IFC Property Sets. The project extracts property definitions from an Excel workbook, normalizes them to a canonical schema, and publishes them as strongly-typed C# DTOs plus JSON/XML exports for language-agnostic consumption.

## Project Structure
```
SnacksDto/                           # Repository root
├─ .github/copilot-instructions.md   # This file
├─ ARCHITECTURE.md                   # Detailed design documentation
├─ README.md                         # Project overview
└─ SnacksDto/                        # Solution root
   ├─ SnacksDto.sln                  # Visual Studio solution file
   ├─ SnacksDto/                     # DTO library (net8.0)
   │  ├─ artifacts/                  # Generated snacks.json (bundled in assembly)
   │  ├─ data/                       # Excel workbook source files
   │  └─ src/                        # DTOs and repository (PropertySetDto, PropertyDto, etc.)
   ├─ CLI/                           # Extraction tool (net8.0 console app)
   │  ├─ Services/                   # WorkbookExtractor, GitHubFileDownloader
   │  ├─ Models/                     # CLI DTOs and data structures
   │  ├─ Program.cs                  # Entry point with CLI argument parsing
   │  └─ CliOptions.cs               # Command-line option definitions
   ├─ CLI.Tests/                     # xUnit tests for extraction logic
   └─ SnacksDtop.Tests/              # xUnit tests for JSON serialization
```

## Build and Test Commands

**From the repository root or `SnacksDto/` directory**, run:

### Build the entire solution
```
dotnet build SnacksDto.sln
```

### Run all tests
```
dotnet test SnacksDto.sln
```

### Run a specific test project
```
dotnet test SnacksDtop.Tests/SnacksDtop.Tests.csproj
dotnet test CLI.Tests/CLI.Tests.csproj
```

### Run the CLI extraction tool
```
dotnet run --project CLI/CLI.csproj -- --workbook <path> --output <path>
```
Defaults: workbook at `SnacksDto/data/Egenskapsstruktur.xlsx`, output at `artifacts/psets.json`

### View CLI help and available options
```
dotnet run --project CLI/CLI.csproj -- --help
```
Key options: `--workbook`, `--output`, `--sheet` (filter by sheet names), `--skip-update`, `--force-download`


## Key Architecture Concepts

### The Data Pipeline
The codebase implements a three-stage workflow:
1. **Excel Workbook** → Input source of truth (one worksheet per property set)
2. **CLI Extraction** → `WorkbookExtractor` (using ClosedXML) parses sheets and enforces schema rules
3. **Canonical JSON** → Normalized in-memory model serialized to `snacks.json`
4. **DTO Library** → `snacks.json` bundled as an embedded resource in the assembly for runtime lookup

**Key Point**: The canonical JSON (in `SnacksDto/artifacts/`) is the contract shared between the CLI tool and the DTO library. Regenerate it whenever the workbook changes.

### Excel Workbook Structure
Each worksheet represents one property set (e.g., `BIM_Modellinfo`):
- **Row 1** (B1): Human-readable title + scope hint → `PropertySetDto.DisplayName` + `Scope`
- **Row 2** (B2): Discipline classification → `PropertySetDto.Discipline`
- **Row 3** (B3:K3): Column headers (`Egenskapsnavn`, `Datatype`, `Nivå`, etc.)
- **Rows 4+**: Property data rows, each becoming a `PropertyDto` instance

### Column Mapping (Excel → PropertyDto)
| Excel Column | Header | DTO Field | Notes |
|---|---|---|---|
| B | Egenskapsnavn | Code + DisplayName | Split on " - " separator |
| C | Datatype | Datatype | IFC primitive type (IfcText, IfcInteger, etc.) |
| D | Nivå | ApplicableEntities | IFC entity names, "Obj", or "*" for all |
| E | Eksempelverdi | SampleValue | Optional example value |
| F | Anbefalte verdier | RecommendedValues | Comma-separated list |
| G | Tillatte verdier | AllowedValues | "*" = unrestricted, otherwise specific values |
| H | Tillatte verdier (Regex) | AllowedPattern | Regex pattern or reference |
| I | Forklaring | Description | Markdown-safe text |
| J | Erfaringer | Notes | Optional lessons learned |
| K | Farge | StatusColor | Enum: Green/Gray/Yellow |

See ARCHITECTURE.md for detailed normalization rules (delimiter handling, null/empty logic, validation).

## Conventions

### Naming
- DTO records: `[Entity]Dto` suffix (e.g., `PropertySetDto`, `PropertyDto`)
- Services: `*Service` or `*Extractor` pattern
- Test classes: `[TestedClass]Tests` or `[Feature]Tests`

### Nullable Reference Types
- Enabled across the entire solution (`<Nullable>enable</Nullable>`)
- Optional properties are explicitly `string?`, `List<T>?`, `IReadOnlyList<T>?`
- Required properties are non-nullable

### Data Validation
- **Duplicate property codes**: Rejected within a single property set (validated in tests)
- **Requirement levels** (0=optional, 1=recommended, 2=mandatory): Enum-driven, enforced in `RequirementLevel.cs`
- **JSON deserialization**: Must work with `System.Text.Json` using case-insensitive options
- **Empty collections**: Prefer `Array.Empty<T>()` or `new List<T>()` over null

### Testing Framework
- **xUnit** with `[Fact]` and `[Theory]` attributes
- No shared test setup; tests are self-contained
- Assertions via the standard `Assert` class
- Both CLI.Tests and SnacksDtop.Tests share the same testing patterns

### Code Style
- Immutable records: `public sealed record class PropertySetDto { ... }`
- C# 11+ features: records with init-only properties, required keyword where applicable
- Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`)

## Dependencies

**CLI Project** (`CLI.csproj`):
- **ClosedXML 0.104.0** – Excel workbook parsing

**Test Projects**:
- **xunit 2.9.3** – Testing framework
- **xunit.runner.visualstudio 3.1.4** – Test explorer integration
- **Microsoft.NET.Test.Sdk 17.14.1** – Test runtime
- **coverlet.collector 6.0.4** – Code coverage

**DTO Library** (`SnacksDto.csproj`):
- No external dependencies; uses built-in `System.Text.Json` for serialization

## Common Development Tasks

### Extracting property sets from a workbook
```
cd SnacksDto
dotnet run --project CLI/CLI.csproj -- --workbook data/Egenskapsstruktur.xlsx --output artifacts/psets.json
```

### Extracting a single sheet
```
dotnet run --project CLI/CLI.csproj -- --workbook data/Egenskapsstruktur.xlsx --sheet BIM_Modellinfo --output test.json
```

### Adding a new property set
1. Add a new worksheet to the Excel workbook with the standard header structure (rows 1-3)
2. Run the CLI extraction tool to regenerate `snacks.json`
3. Commit the updated JSON artifact
4. Run tests to verify deserialization: `dotnet test SnacksDtop.Tests/`

### Modifying the Excel schema
1. Edit `WorkbookExtractor.cs` in CLI/Services/ to adjust column mapping or validation logic
2. Update corresponding DTO fields in CLI/Models/ if adding new columns
3. Regenerate `snacks.json` and commit
4. Update tests if validation rules changed

### Changing the DTO schema
1. Modify record definitions in `SnacksDto/src/` (PropertySetDto, PropertyDto, etc.)
2. Regenerate `snacks.json` via the CLI
3. Update test expectations in `SnacksDtop.Tests/`
4. Consider semantic versioning implications: breaking changes warrant major version bump
