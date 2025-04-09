# SDK-style SQL Database Project Referenced Database Downloader Tool

[![Licence](https://img.shields.io/badge/licence-MIT-blue)](LICENCE)
![GitHub Actions Build Status](https://img.shields.io/github/actions/workflow/status/bit-by-bit-solutions/ref-db-downloader/ci.yml)
![NuGet Version](https://img.shields.io/nuget/v/BitByBit.SqlSdkProject.ReferencedDbDownloader)

## Table of contents

- [Overview](#overview)
- [Usage](#usage)
  - [Global install](#global-install)
  - [Local install](#local-install)

## Overview

This tool is intended for use in deployment pipelines that make use of [SqlPackage](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage) for deploying SQL Server databases as data tier applications (DAC, `.dacpac` files), in particular those that include database references in the form of package references.

The tool will read the `.sqlproj` project file supplied as input and download and extract the `.dacpac` files from the referenced packages into the output directory specified (also supplied as an input parameter). If a `NuGet.config` file is present then it will be used for any package sources and credentials.

The tool assumes that the project is defined as part of a solution, which follows a typical .NET layout with the solution at the highest level.

```bash
└── solution-dir
    ├── project-1
    └── project-2
```

If the tool can't find a `.sln` solution file it will throw an error.

## Usage

To install the tool run `dotnet tool install --global BitByBit.SqlSdkProject.ReferencedDbDownloader` (omit `--global` for a local install).

### Global install

Run `ref-db-download --project "/path/to/your.sqlproj" --outputDirectory "/tmp/outdir"`.

### Local install

Run `dotnet tool ref-db-download --project "/path/to/your.sqlproj" --outputDirectory "/tmp/outdir"` or `dotnet ref-db-download --project "/path/to/your.sqlproj" --outputDirectory "/tmp/outdir"`.
