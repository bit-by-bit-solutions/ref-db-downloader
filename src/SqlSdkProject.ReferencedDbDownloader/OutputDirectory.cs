namespace SqlSdkProject.ReferencedDbDownloader;

internal sealed class OutputDirectory
{
	private readonly string directoryPath;

	private OutputDirectory(string directoryPath) => this.directoryPath = directoryPath;

	public static implicit operator string(OutputDirectory outputDirectory) => outputDirectory.directoryPath;

	public static implicit operator OutputDirectory(string value) => Create(value);

	private static OutputDirectory Create(string directoryPath)
	{
		if (File.Exists(directoryPath))
			throw new ArgumentException("An existing file was specified as the output directory.", nameof(directoryPath));

		return new OutputDirectory(directoryPath);
	}
}
