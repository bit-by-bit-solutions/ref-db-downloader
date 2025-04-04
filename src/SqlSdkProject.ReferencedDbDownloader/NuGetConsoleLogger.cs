using NuGet.Common;

namespace SqlSdkProject.ReferencedDbDownloader;

internal sealed class NuGetConsoleLogger : LoggerBase
{
	internal static ILogger Instance { get; } = new NuGetConsoleLogger();

	public override void Log(ILogMessage message) => WriteLogMessage(message).GetAwaiter().GetResult();

	public override Task LogAsync(ILogMessage message) => WriteLogMessage(message);

	private static async Task WriteLogMessage(ILogMessage message)
	{
		ConsoleColor previousColor = Console.ForegroundColor;
		try
		{
			Console.ForegroundColor = GetColour(message.Level);
			await GetOutput(message.Level).WriteLineAsync($"[{message.Level}] {message.Message}");
		}
		finally
		{
			Console.ForegroundColor = previousColor;
		}
	}

	private static ConsoleColor GetColour(LogLevel level) => level switch
	{
		LogLevel.Error => ConsoleColor.Red,
		LogLevel.Warning => ConsoleColor.Yellow,
		LogLevel.Information => ConsoleColor.White,
		LogLevel.Debug => ConsoleColor.Gray,
		LogLevel.Minimal => ConsoleColor.White,
		LogLevel.Verbose => ConsoleColor.Gray,
		_ => ConsoleColor.White,
	};

	private static TextWriter GetOutput(LogLevel level) => level switch
	{
		LogLevel.Error => Console.Error,
		_ => Console.Out,
	};
}
