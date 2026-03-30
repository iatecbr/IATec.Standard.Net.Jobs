namespace Application.Configurations.Options;

public class UrlsServiceClientOption
{
    public const string Key = "IATec:Services";

    public AccountsOption Accounts { get; init; } = new();
    public InventoryOption Inventory { get; init; } = new();
}