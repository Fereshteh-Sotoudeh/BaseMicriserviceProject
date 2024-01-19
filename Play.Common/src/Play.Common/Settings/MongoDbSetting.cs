namespace Play.Common.Settings;
public class MongoDbSetting
{
    public string Host { get; init; }
    public int Port { get; init; }
    public string ConnectionString => $"mongodb://{Host}:{Port}";
}