using STFU.UI;

WriteLog("Starting STFU host.");

try
{
    StfuUiHost.Run(args, WriteLog);
    WriteLog("STFU UI stopped.");
}
catch (Exception exception)
{
    WriteLog($"Fatal error: {exception}");
    Environment.ExitCode = 1;
}

static void WriteLog(string message)
{
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}");
}
