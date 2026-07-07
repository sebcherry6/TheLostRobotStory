using System;

public static class GameEvents
{
    public static Action<string> Message;

    public static void ShowMessage(string text)
    {
        Message?.Invoke(text);
    }
}