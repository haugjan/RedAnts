# RedAnts.Domain.Tests

Unit tests for the pure domain layer (`Domain/`).

## Why the Domain sources are linked, not project-referenced

The domain layer has zero framework or Umbraco dependencies by architectural
rule (see `CLAUDE.md`). The whole app lives in a single `Microsoft.NET.Sdk.Web`
project (`RedAnts.csproj`), so a `ProjectReference` would drag Umbraco, the
Azure Blob provider, uSync and the ICU runtime into the test build just to
exercise a handful of value objects and aggregates.

Instead this project links only the domain sources:

```xml
<Compile Include="..\..\Domain\**\*.cs" />
```

That keeps the suite hermetic and fast (~0.1 s, no web host, no database) and
compiles the exact same source the app ships. If the domain ever gains an
external dependency, this project stops compiling, which is the signal that the
"pure domain" rule was broken.

The root web project excludes this folder from its own globbing via
`<DefaultItemExcludes>...;tests/**</DefaultItemExcludes>` in `RedAnts.csproj`,
so the test files are never pulled into the app build.

## Running

```
dotnet test tests/RedAnts.Domain.Tests
```
