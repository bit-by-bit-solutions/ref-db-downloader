using Assembly = System.Reflection.Assembly;

namespace SqlSdkProject.ReferencedDbDownloader.Tests;

internal sealed class OutputDirectoryTests
{
	[Test]
	public async Task ImplicitConversion_ToString_ReturnsFilePath()
	{
		const string directoryPath = "/tmp/test";
		OutputDirectory outputDirectory = directoryPath;

		string result = outputDirectory;

		await Assert.That(result).IsEqualTo(directoryPath);
	}

	[Test]
	public async Task ImplicitConversion_FromString_CreatesOutputDirectory()
	{
		const string directoryPath = "/tmp/test";

		OutputDirectory outputDirectory = directoryPath;

		await Assert.That(outputDirectory).IsNotNull();
		await Assert.That((string)outputDirectory).IsEqualTo(directoryPath);
	}

	[Test]
	public async Task Create_PathToExistingFile_ThrowsArgumentException()
	{
		string fileName = Guid.NewGuid().ToString();
		string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, fileName);

		try
		{
			await File.WriteAllTextAsync(path, nameof(Create_PathToExistingFile_ThrowsArgumentException));

			var exception = Assert.Throws<ArgumentException>(() => _ = (OutputDirectory)path);
			await Assert.That(exception.Message).StartsWith("An existing file was specified as the output directory.");
		}
		finally
		{
			File.Delete(path);
		}
	}
}
