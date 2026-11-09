namespace Captacao.Api.DTOs.Solicitacoes;

// Cartão do painel Kanban e linha de listagem.
public class SolicitacaoRespostaDto
{
    public int SolicitacaoId { get; set; }
    public int MotoristaId { get; set; }
    public string? NomeMotorista { get; set; }
    public string Telefone { get; set; } = string.Empty;
    public int? VeiculoId { get; set; }
    public string? Placa { get; set; }
    public decimal? CapacidadeToneladas { get; set; }
    public int? CargaId { get; set; }
    public string? CargaDescricao { get; set; }
    public int? AtendenteId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusRotulo { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    // NaoAplicavel | SemRestricao | Compativel | Incompativel | NaoVerificada
    public string Compatibilidade { get; set; } = string.Empty;
    public int PendenciasDocumentais { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class DocumentoAvaliadoDto
{
    public string Chave { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
    public bool Obrigatorio { get; set; }
    // Ausente | AguardandoValidacao | Validado | Reprovado
    public string Situacao { get; set; } = string.Empty;
    public int? DocumentoId { get; set; }
    public string? NomeOriginal { get; set; }
    public DateTime? DataEnvio { get; set; }
    public bool Reutilizado { get; set; }
}

// Janela de detalhes (Figura 10).
public class SolicitacaoDetalheDto : SolicitacaoRespostaDto
{
    public string? CpfMascarado { get; set; }
    public string? Email { get; set; }
    public string DescricaoCompatibilidade { get; set; } = string.Empty;
    public List<string> CamposPendentes { get; set; } = new();
    public List<DocumentoAvaliadoDto> Documentos { get; set; } = new();
}

public class KanbanColunaDto
{
    public string Status { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
    public List<SolicitacaoRespostaDto> Solicitacoes { get; set; } = new();
}
