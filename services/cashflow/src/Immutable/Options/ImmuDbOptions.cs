namespace ArchChallenge.CashFlow.Infrastructure.Data.Immutable.Options;

public sealed class ImmuDbOptions
{
    public const string SectionName = "ImmuDb";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 3322;

    public string Username { get; set; } = "immudb";

    public string Password { get; set; } = "immudb";

    public string Database { get; set; } = "defaultdb";

    /// <summary>
    /// Verifica se o UUID do servidor immudb corresponde ao registrado anteriormente.
    /// Desabilite em ambientes locais onde o container é recriado com frequência.
    /// </summary>
    public bool CheckDeploymentInfo { get; set; } = true;
}
