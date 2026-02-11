# SnacksDto

Data Transfer Objects to make it easy to implement IFC Property Sets using any programming language.

SnacksDto extracts property definitions from an Excel workbook, normalizes them to a canonical schema, and publishes strongly-typed C# DTOs plus JSON/XML exports for language-agnostic consumption.

## Quick Start

### Using the .NET DTO Library

```csharp
using SnacksDto;

// Load property sets from the bundled JSON
var repository = new PropertySetRepository();
var modellinfo = repository.GetByName("BIM_Modellinfo");

foreach (var property in modellinfo.Properties)
{
    Console.WriteLine($"{property.Code}: {property.DisplayName}");
}
```

### Using JSON/XML Files

Download `snacks.json` or `snacks.xml` from the [latest release](../../releases/latest) and deserialize using your language's JSON/XML libraries:

```python
import json

with open('snacks.json', 'r', encoding='utf-8') as f:
    property_sets = json.load(f)

for pset in property_sets:
    print(f"{pset['name']}: {pset['discipline']}")
    for prop in pset['properties']:
        print(f"  - {prop['code']}: {prop['displayName']}")
```

## Documentation

- **[.github/copilot-instructions.md](.github/copilot-instructions.md)** – Development guide for developers working in this codebase (build commands, testing, conventions)
- **[ARCHITECTURE.md](ARCHITECTURE.md)** – Design rationale, data pipeline, CI/release flow
- **[LICENSE](LICENSE)** – License terms

## Project Structure

```
SnacksDto/
├─ SnacksDto/                  # Solution root
│  ├─ SnacksDto/               # DTO library (net8.0)
│  ├─ CLI/                     # Extraction tool (net8.0 console app)
│  ├─ CLI.Tests/               # Unit tests
│  ├─ SnacksDtop.Tests/        # Integration tests
│  └─ SnacksDto.sln
├─ .github/copilot-instructions.md
├─ ARCHITECTURE.md
└─ README.md
```

## Building Locally

From the `SnacksDto/` directory:

```bash
# Build
dotnet build SnacksDto.sln

# Run tests
dotnet test SnacksDto.sln

# Run CLI extraction tool
dotnet run --project CLI/CLI.csproj -- --help
```

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for detailed build and development instructions.

## Workflow

1. Domain experts maintain the Excel workbook in `SnacksDto/data/`
2. Run the CLI extraction tool to parse the workbook → generates canonical `snacks.json`
3. Commit the updated JSON artifact
4. CI validates the artifact and builds the NuGet package
5. Release includes: NuGet package, JSON/XML files, and changelog

## License

See [LICENSE](LICENSE) for terms.
