namespace STFU.UI.Bridge.Session;

public sealed record UiCommandLogEntry(DateTimeOffset Time, string Text, int HandledCount);
