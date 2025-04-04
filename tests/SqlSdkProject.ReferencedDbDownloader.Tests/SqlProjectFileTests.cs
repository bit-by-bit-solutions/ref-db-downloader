namespace SqlSdkProject.ReferencedDbDownloader.Tests;

internal sealed class SqlProjectFileTests
{
	[Test]
	public async Task ImplicitConversion_ToString_ReturnsFilePath()
	{
		const string filePath = "test.sqlproj";
		SqlProjectFile sqlProjectFile = filePath;

		string result = sqlProjectFile;

		await Assert.That(result).IsEqualTo(filePath);
	}

	[Test]
	public async Task ImplicitConversion_FromString_CreatesSqlProjectFile()
	{
		const string filePath = "test.sqlproj";

		SqlProjectFile sqlProjectFile = filePath;

		await Assert.That(sqlProjectFile).IsNotNull();
		await Assert.That((string)sqlProjectFile).IsEqualTo(filePath);
	}

	[Test]
	public async Task Create_InvalidFileExtension_ThrowsArgumentException()
	{
		const string invalidFilePath = "test.txt";

		var exception = Assert.Throws<ArgumentException>(() => _ = (SqlProjectFile)invalidFilePath);
		await Assert.That(exception.Message).StartsWith("The project file is not a SQL project file.");
	}
}
