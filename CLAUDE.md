# TicketRenamer

## Overview
Windows desktop app (.NET 8) that automates renaming purchase receipt photos from Spanish supermarkets. Uses Groq Vision API (LLM) to extract vendor name and date from ticket images, then renames files to `Proveedor-AA-MM-DD[-N].ext`.

## Architecture

### Projects
- `src/TicketRenamer.Core` - Class Library: business logic, OCR, parsing, file operations
- `src/TicketRenamer.Console` - Console App: CLI interface with System.CommandLine
- `tests/TicketRenamer.Core.Tests` - xUnit tests

### Key Directories
- `Models/` - Record types (ReceiptData, ProcessingResult, ProcessingOptions, ProviderDictionary)
- `Parsers/` - DateParser (static, regex), ProviderMatcher, FileNameBuilder
- `Services/` - IOcrService/GroqVisionService, IBackupService, ILogService, IProcessingPipeline

### Processing Pipeline (sequential, fail-fast)
1. Scan `entrada/` for JPG/PNG images
2. Filter out already-processed files (via registro.txt HashSet)
3. Backup ALL new files to `backup/` (fail-fast: if one fails, stop everything)
4. For each file: Groq Vision OCR -> parse provider+date -> generate name -> move to `procesados/`
5. Log each operation to registro.txt
6. Validate: entrada/ empty, backup count == procesados count

## Commands
```bash
dotnet build                    # Build solution
dotnet test                     # Run all tests (51 tests)
dotnet run --project src/TicketRenamer.Console -- --input ./entrada --output ./procesados --backup ./backup --verbose
```

## Configuration
- **GROQ_API_KEY**: Set as environment variable (never commit to git)
- **proveedores.json**: Provider dictionary at project root, maps OCR variations to canonical names

## Conventions
- Language: Code in English, data/config in Spanish
- Records for immutable models, sealed classes for services
- Interfaces for all services (testable with mocks)
- Central Package Management (Directory.Packages.props)
- TreatWarningsAsErrors enabled
- FluentAssertions + xUnit + Moq for tests

## Exit Codes
- 0: All files processed successfully
- 1: Some files failed (OCR errors)
- 2: Critical error (backup failure, missing API key)
