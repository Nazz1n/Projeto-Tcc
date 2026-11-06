
namespace Captacao.Domain.Entidades;

public class Atendente
{
    public int AtendenteId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string SenhaHash { get; set; } = string.Empty;

    public string Perfil { get; set; } = "Atendente";

    public bool Ativo { get; set; } = true;

    // Relacionamentos
    public ICollection<Carga> Cargas { get; set; }
        = new List<Carga>();

    public ICollection<Solicitacao> Solicitacoes { get; set; }
        = new List<Solicitacao>();

    public ICollection<HistoricoStatus> HistoricosStatus { get; set; }
        = new List<HistoricoStatus>();

    public ICollection<RegistroAuditoria> RegistrosAuditoria { get; set; }
        = new List<RegistroAuditoria>();
}
