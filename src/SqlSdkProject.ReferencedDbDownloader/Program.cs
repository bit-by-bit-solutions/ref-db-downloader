using System.CommandLine;

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

		var rootCommand = new RootCommand(
			"""
			Downloads database DACPAC files referenced in an SDK-style SQL Server Database project.
			Will use any NuGet package sources and credentials configured in your NuGet.Config.
			""")
		{
			projectOption,
			outputDirectoryOption,
		};

		rootCommand.SetHandler(
			async (project, outputDirectory) =>
			{
				try
				{
					var progress = new Progress<string>(Console.WriteLine);
					using var downloader = new DacPacDownloader(project.FullName, outputDirectory.FullName, progress);
					await downloader.DownloadFiles(cancellationToken);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					await Console.Error.WriteLineAsync(ex.ToString());
					throw;
				}
			},
			projectOption,
			outputDirectoryOption);

		return rootCommand;
	}
}
