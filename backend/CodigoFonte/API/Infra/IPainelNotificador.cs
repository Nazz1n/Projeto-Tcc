namespace Captacao.Api.Infra;

// Painel em tempo real (RF08, RF09, RNF05). Os serviços chamam esta
// interface quando uma solicitação muda. A implementação em uso é
// PainelNotificadorSignalR (Infra/PainelHub.cs); PainelNotificadorNulo
// existe para testes e para desligar o envio.
public interface IPainelNotificador
{
    Task SolicitacaoAtualizadaAsync(int solicitacaoId, CancellationToken ct = default);

    // A solicitação passou a "Pronto para análise" (RF08).
    Task SolicitacaoProntaParaAnaliseAsync(int solicitacaoId, CancellationToken ct = default);
}

public class PainelNotificadorNulo : IPainelNotificador
{
    private readonly ILogger<PainelNotificadorNulo> _logger;

    public PainelNotificadorNulo(ILogger<PainelNotificadorNulo> logger) => _logger = logger;

    public Task SolicitacaoAtualizadaAsync(int solicitacaoId, CancellationToken ct = default)
    {
        _logger.LogDebug("Painel: solicitação {Id} atualizada (tempo real não implementado).", solicitacaoId);
        return Task.CompletedTask;
    }

    public Task SolicitacaoProntaParaAnaliseAsync(int solicitacaoId, CancellationToken ct = default)
    {
        _logger.LogDebug("Painel: solicitação {Id} pronta para análise (tempo real não implementado).", solicitacaoId);
        return Task.CompletedTask;
    }
}
