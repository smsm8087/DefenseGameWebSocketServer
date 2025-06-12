public static class HandlerFactory
{
    private static readonly Dictionary<string, IMessageHandler> _handlers = new()
    {
        //여기에 메시지 타입과 핸들러를 추가
        { "move", new MoveHandler() },
    };

    public static IMessageHandler? GetHandler(string type)
    {
        _handlers.TryGetValue(type, out var handler);
        return handler;
    }
}