using Captacao.Api.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Captacao.Api.Infra;

// Hub do painel em tempo real (RF09, RNF05). Somente atendentes e
// administradores autenticados se conectam. O servidor só envia; o cliente
// não chama métodos no hub.
//
// Eventos enviados a todos os conectados (apenas o id, sem dado pessoal;
// o front busca os detalhes pelos endpoints protegidos):
//   "SolicitacaoAtualizada"       { solicitacaoId }
//   "SolicitacaoProntaParaAnalise" { solicitacaoId }
[Authorize(Roles = $"{Perfis.Administrador},{Perfis.Atendente}")]
public class PainelHub : Hub
{
}

public class PainelNotificadorSignalR : IPainelNotificador
{
    private readonly IHubContext<PainelHub> _hub;
    private readonly ILogger<PainelNotificadorSignalR> _logger;

    public PainelNotificadorSignalR(
        IHubContext<PainelHub> hub,
        ILogger<PainelNotificadorSignalR> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task SolicitacaoAtualizadaAsync(int solicitacaoId, CancellationToken ct = default) =>
        EnviarAsync("SolicitacaoAtualizada", solicitacaoId, ct);

    public Task SolicitacaoProntaParaAnaliseAsync(int solicitacaoId, CancellationToken ct = default) =>
        EnviarAsync("SolicitacaoProntaParaAnalise", solicitacaoId, ct);

    // Falha de notificação nunca pode derrubar a operação que a originou.
    private async Task EnviarAsync(string evento, int solicitacaoId, CancellationToken ct)
    {
        try
        {
            await _hub.Clients.All.SendAsync(evento, new { solicitacaoId }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao notificar o painel ({Evento}, solicitação {Id}).", evento, solicitacaoId);
        }
    }
}
