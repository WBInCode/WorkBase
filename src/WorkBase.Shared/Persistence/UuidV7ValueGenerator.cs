namespace WorkBase.Shared.Persistence;

public static class UuidV7
{
    public static Guid Create() => Guid.CreateVersion7();
}
