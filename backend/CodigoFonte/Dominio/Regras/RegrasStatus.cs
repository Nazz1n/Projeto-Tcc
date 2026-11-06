using Captacao.Domain.Constantes;

namespace Captacao.Domain.Regras;

// Máquina de estados da solicitação (RF14).
// Automáticas: NovoContato -> AguardandoPreenchimento (formulário acessado);
//   envio -> ComPendencias | ProntoParaAnalise; ComPendencias ->
//   ProntoParaAnalise (pendências resolvidas); ProntoParaAnalise/EmAnalise ->
//   ComPendencias (documento reprovado).
// Manuais (atendente): -> EmAnalise, -> Contratado (só de EmAnalise),
//   -> Encerrado (de qualquer estado aberto).
public static class RegrasStatus
{
    public static string StatusAposEnvio(AvaliacaoSolicitacao a) =>
        a.PossuiDocumentoObrigatorioAusente || a.PossuiDocumentoObrigatorioReprovado
            ? StatusSolicitacao.ComPendencias
            : StatusSolicitacao.ProntoParaAnalise;

    // Um reenvio não devolve à fila uma solicitação já assumida.
    public static bool EnvioAlteraStatus(string atual) =>
        atual == StatusSolicitacao.NovoContato ||
        atual == StatusSolicitacao.AguardandoPreenchimento ||
        atual == StatusSolicitacao.ComPendencias;

    public static string? ProximoStatusAutomatico(string atual, AvaliacaoSolicitacao a)
    {
        switch (atual)
        {
            case StatusSolicitacao.ComPendencias:
                var semPendencias = a.CamposObrigatoriosPreenchidos
                    && !a.PossuiDocumentoObrigatorioAusente
                    && !a.PossuiDocumentoObrigatorioReprovado;
                return semPendencias ? StatusSolicitacao.ProntoParaAnalise : null;

            case StatusSolicitacao.ProntoParaAnalise:
            case StatusSolicitacao.EmAnalise:
                return a.PossuiDocumentoObrigatorioReprovado
                    ? StatusSolicitacao.ComPendencias : null;

            default:
                return null;
        }
    }

    public static bool TransicaoManualPermitida(string atual, string novo, out string? motivo)
    {
        motivo = null;

        if (atual == novo) { motivo = "A solicitação já está nesse status."; return false; }

        if (StatusSolicitacao.EhFinal(atual))
        {
            motivo = "A solicitação já foi finalizada e não pode mais ser alterada.";
            return false;
        }

        switch (novo)
        {
            case StatusSolicitacao.EmAnalise:
                if (atual == StatusSolicitacao.ComPendencias || atual == StatusSolicitacao.ProntoParaAnalise)
                    return true;
                motivo = "Só é possível assumir solicitações cujo formulário já foi enviado.";
                return false;

            case StatusSolicitacao.Contratado:
                if (atual == StatusSolicitacao.EmAnalise) return true;
                motivo = "Assuma a solicitação (Em análise) antes de marcá-la como contratada.";
                return false;

            case StatusSolicitacao.Encerrado:
                return true;

            default:
                motivo = "Esse status é atualizado automaticamente e não pode ser definido manualmente.";
                return false;
        }
    }
}
