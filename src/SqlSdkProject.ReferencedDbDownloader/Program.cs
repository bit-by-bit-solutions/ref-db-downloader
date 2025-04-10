using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace SqlSdkProject.ReferencedDbDownloader;

internal static class Program
{
	private static async Task<int> Main(string[] args)
	{
		using var cts = new CancellationTokenSource();
		Console.CancelKeyPress += (_, e) =>
		{
			e.Cancel = true;
			cts.Cancel();
		};

		try
		{
			RootCommand rootCommand = CreateRootCommand(cts.Token);
			return await rootCommand.InvokeAsync(args);
		}
		catch (OperationCanceledException)
		{
			Console.WriteLine("Cancelled");
			return 1;
		}
	}

	private static RootCommand CreateRootCommand(CancellationToken cancellationToken)
	{
		Option<FileInfo> projectOption = new Option<FileInfo>(
			"--project",
			"The .sqlproj file containing the NuGet database references to download")
		{
			IsRequired = true,
		}.ExistingOnly();

		Option<DirectoryInfo> outputDirectoryOption = new Option<DirectoryInfo>(
			"--outputDirectory",
			"The output directory for the downloaded DACPAC files")
		{
			IsRequired = true,
		}.LegalFilePathsOnly();

		const string includePackagesOptionDescription = """
			An optional list of additional NuGet packages to download and extract DACPAC files from. The
			format of each package is ID <package>:<version>, for example: --include Microsoft.SqlServer.Dacpacs.Master:160.2.3.
			You can specify multiple packages by separating them with a space, or by repeating the option.
			""";

		var includePackagesOption = new Option<ImmutableList<DatabasePackageReference>>(
			"--include",
			ParseIncludePackagesArgument,
			false,
			includePackagesOptionDescription)
		{
			AllowMultipleArgumentsPerToken = true,
		};

		var rootCommand = new RootCommand(
			"""
			Downloads database DACPAC files referenced in an SDK-style SQL Server Database project.
			Will use any NuGet package sources and credentials configured the project's NuGet.Config.
			""")
		{
			projectOption,
			outputDirectoryOption,
			includePackagesOption,
		};

		rootCommand.SetHandler(
			async (project, outputDirectory, additionalPackages) =>
			{
				try
				{
					var progress = new Progress<string>(Console.WriteLine);
					using var downloader = new DacPacDownloader(project.FullName, outputDirectory.FullName, progress)
					{
						AdditionalPackagesToDownload = additionalPackages,
					};

					await downloader.DownloadFiles(cancellationToken);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					await Console.Error.WriteLineAsync(ex.ToString());
					throw;
				}
			},
			projectOption,
			outputDirectoryOption,
			includePackagesOption);

		return rootCommand;

		static ImmutableList<DatabasePackageReference> ParseIncludePackagesArgument(ArgumentResult result)
		{
			var packages = new List<DatabasePackageReference>();

			foreach (Token token in result.Tokens)
			{
				var (packageReference, errorMessage) = DatabasePackageReference.Parse(token.Value);
				if (packageReference is null)
				{
					result.ErrorMessage = errorMessage;
					return [];
				}

				packages.Add(packageReference);
			}

			return packages.ToImmutableList();
		}
	}
}
