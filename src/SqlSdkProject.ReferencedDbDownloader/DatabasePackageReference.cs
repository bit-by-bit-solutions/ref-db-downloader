using NuGet.Versioning;

namespace SqlSdkProject.ReferencedDbDownloader;

internal sealed record DatabasePackageReference(string Id, string Version)
{
	internal static (DatabasePackageReference? PackageReference, string ErrorMessage) Parse(string packageId)
	{
		string[] values = packageId.Split(':');
		if (values.Length != 2)
			return (null, "The package ID must be in the format <package>:<version>");

		if (string.IsNullOrWhiteSpace(values[0]))
			return (null, "The package name cannot be empty");

		return NuGetVersion.TryParse(values[1], out NuGetVersion? version)
			? (new DatabasePackageReference(values[0], version.ToString()), string.Empty)
			: (null, $"The version '{values[1]}' is not a valid NuGet version");
	}
}
