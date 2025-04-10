namespace SqlSdkProject.ReferencedDbDownloader.Tests;

internal sealed class DatabasePackageReferenceTests
{
	[Test]
	public async Task Parse_ValidPackageReference_ReturnsPackageReference()
	{
		const string input = "package:1.0.0";

		var (reference, error) = DatabasePackageReference.Parse(input);

		await Assert.That(reference).IsNotNull();
		await Assert.That(reference!.Id).IsEqualTo("package");
		await Assert.That(reference.Version).IsEqualTo("1.0.0");
		await Assert.That(error).IsEmpty();
	}

	[Test]
	[Arguments("package1.0.0")]
	[Arguments("package:1.0:0")]
	public async Task Parse_InvalidFormat_ReturnsError(string input)
	{
		var (reference, error) = DatabasePackageReference.Parse(input);

		await Assert.That(reference).IsNull();
		await Assert.That(error).IsEqualTo("The package ID must be in the format <package>:<version>");
	}

	[Test]
	public async Task Parse_EmptyPackageName_ReturnsError()
	{
		const string input = ":1.0.0";

		var (reference, error) = DatabasePackageReference.Parse(input);

		await Assert.That(reference).IsNull();
		await Assert.That(error).IsEqualTo("The package name cannot be empty");
	}

	[Test]
	public async Task Parse_InvalidVersion_ReturnsError()
	{
		const string input = "package:invalid";

		var (reference, error) = DatabasePackageReference.Parse(input);

		await Assert.That(reference).IsNull();
		await Assert.That(error).IsEqualTo("The version 'invalid' is not a valid NuGet version");
	}
}
