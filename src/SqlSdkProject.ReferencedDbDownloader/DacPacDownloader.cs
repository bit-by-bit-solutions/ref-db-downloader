using System.Collections.Immutable;
using Microsoft.Build.Construction;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace SqlSdkProject.ReferencedDbDownloader;

internal sealed class DacPacDownloader : IDisposable
{
	private readonly SourceCacheContext cacheContext = new();
	private readonly SqlProjectFile projectFile;
	private readonly OutputDirectory outputDirectory;
	private readonly IProgress<string> progress;

	internal DacPacDownloader(SqlProjectFile projectFile, OutputDirectory outputDirectory, IProgress<string> progress)
	{
		this.projectFile = projectFile;
		this.outputDirectory = outputDirectory;
		this.progress = progress;
	}

	public void Dispose() => cacheContext.Dispose();

	internal async Task DownloadFiles(CancellationToken cancellationToken)
	{
		IImmutableList<DatabasePackageReference> packageRefs = GetDatabasePackageReferences(projectFile);
		if (packageRefs.Count == 0)
		{
			progress.Report("No package references found. Exiting.");
			return;
		}

		string solutionDirectory = GetSolutionDirectory(projectFile).FullName;
		ISettings settings = Settings.LoadDefaultSettings(solutionDirectory);
		IImmutableList<SourceRepository> repositories = GetSourceRepositories(settings);

		progress.Report($"Found {packageRefs.Count} package references to process");
		progress.Report($"Using {repositories.Count} package sources");

		Directory.CreateDirectory(outputDirectory);

		foreach (var (packageRef, index) in packageRefs.Select((dpr, i) => (dpr, i)))
		{
			progress.Report($"Processing package {index + 1} of {packageRefs.Count}...");
			await DownloadPackageAndExtractDacPacFiles(packageRef, repositories, cancellationToken);
		}

		progress.Report("All packages processed successfully");
	}

	private static IImmutableList<SourceRepository> GetSourceRepositories(ISettings settings)
	{
		var sourceRepositoryProvider = new SourceRepositoryProvider(
			new PackageSourceProvider(settings),
			Repository.Provider.GetCoreV3());

		return [..sourceRepositoryProvider.GetRepositories()];
	}

	private static ImmutableList<DatabasePackageReference> GetDatabasePackageReferences(string projectFile)
	{
		ProjectRootElement project = ProjectRootElement.Open(projectFile)
			?? throw new ArgumentException("Unable to open the project file.", nameof(projectFile));

		return project.Items
			.Where(IsPackageReference)
			.Where(IsReferencedDatabase)
			.Where(HasVersionElement)
			.Select(CreateDatabasePackageReference)
			.ToImmutableList();
	}

	private static DatabasePackageReference CreateDatabasePackageReference(ProjectItemElement projectItemElement) => new(
		projectItemElement.Include,
		projectItemElement.Metadata.First(projectMetadataElement => projectMetadataElement.Name == "Version").Value);

	private static bool HasVersionElement(ProjectItemElement projectItemElement) =>
		projectItemElement.Metadata.Any(projectMetadataElement => projectMetadataElement.Name == "Version");

	private static bool IsPackageReference(ProjectItemElement projectItemElement) =>
		projectItemElement.ItemType == "PackageReference";

	private static bool IsReferencedDatabase(ProjectItemElement projectItemElement) =>
		projectItemElement.Metadata.Any(projectMetadataElement => projectMetadataElement.Name == "DatabaseSqlCmdVariable");

	private static DirectoryInfo GetSolutionDirectory(string projectFile)
	{
		string directoryName = Path.GetDirectoryName(projectFile) ??
			throw new ArgumentException("Unable to get directory from project file path.", nameof(projectFile));

		var directory = new DirectoryInfo(directoryName);
		while (directory is not null && DirectoryDoesNotContainSolutionFile(directory))
			directory = directory.Parent;

		if (directory is null)
			throw new DirectoryNotFoundException($"Could not find a solution file (.sln) in any parent directory of '{projectFile}'.");

		return directory;
	}

	private static bool DirectoryDoesNotContainSolutionFile(DirectoryInfo directory) =>
		directory.GetFiles("*.sln", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).Length == 0;

	private async Task DownloadPackageAndExtractDacPacFiles(
		DatabasePackageReference packageRef,
		IImmutableList<SourceRepository> repositories,
		CancellationToken cancellationToken)
	{
		foreach (SourceRepository repo in repositories)
		{
			if (await DownloadPackageAndExtractDacPacFiles(packageRef, repo, cancellationToken))
				break;
		}
	}

	private async Task<bool> DownloadPackageAndExtractDacPacFiles(
		DatabasePackageReference packageRef,
		SourceRepository repo,
		CancellationToken cancellationToken)
	{
		using var packageStream = new MemoryStream();
		var resource = await repo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

		if (!await resource.CopyNupkgToStreamAsync(
				packageRef.Id,
				new NuGetVersion(packageRef.Version),
				packageStream,
				cacheContext,
				NuGetConsoleLogger.Instance,
				cancellationToken))
		{
			progress.Report($"Package {packageRef.Id} {packageRef.Version} not found in {repo.PackageSource.Source}");
			return false;
		}

		packageStream.Position = 0;
		await ExtractDacPacFiles(packageStream, cancellationToken);

		return true;
	}

	private async Task ExtractDacPacFiles(Stream packageStream, CancellationToken cancellationToken)
	{
		using var packageReader = new PackageArchiveReader(packageStream);
		IEnumerable<string> files = await packageReader.GetFilesAsync("tools", cancellationToken);

		foreach (string file in files.Where(f => Path.GetExtension(f).Equals(".dacpac", StringComparison.OrdinalIgnoreCase)))
		{
			string fileName = Path.GetFileName(file);
			packageReader.ExtractFile(
				file,
				Path.Combine(outputDirectory, fileName),
				NuGetConsoleLogger.Instance);

			progress.Report($"Downloaded {fileName}");
		}
	}

	private sealed record DatabasePackageReference(string Id, string Version);
}
