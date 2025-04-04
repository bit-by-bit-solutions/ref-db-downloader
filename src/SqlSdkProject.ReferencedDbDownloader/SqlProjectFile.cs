namespace SqlSdkProject.ReferencedDbDownloader;

internal sealed class SqlProjectFile
{
	private readonly string filePath;

	private SqlProjectFile(string filePath) => this.filePath = filePath;

	public static implicit operator string(SqlProjectFile sqlProjectFile) => sqlProjectFile.filePath;

	public static implicit operator SqlProjectFile(string value) => Create(value);

	private static SqlProjectFile Create(string filePath)
	{
		if (!Path.GetExtension(filePath).Equals(".sqlproj", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("The project file is not a SQL project file.", nameof(filePath));

		return new SqlProjectFile(filePath);
	}
}
