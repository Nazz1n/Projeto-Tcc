namespace Captacao.Api.DTOs.Motoristas;

public class MotoristaRespostaDto
{
    public int MotoristaId { get; set; }

    // Nulo até o motorista preencher o formulário.
    public string? NomeCompleto { get; set; }

    public string Telefone { get; set; } = string.Empty;

    // Sempre parcialmente oculto: ***.982.***-25 (RF13).
    public string? CpfMascarado { get; set; }

    public string? Email { get; set; }

    public string? Rntrc { get; set; }

    public DateTime DataCadastro { get; set; }

    // Veículo ativo mais recente.
    public string? Placa { get; set; }

    public decimal? CapacidadeToneladas { get; set; }

    // Solicitação mais recente (RF16).
    public int? UltimaSolicitacaoId { get; set; }

    public string? StatusUltimaSolicitacao { get; set; }

    public DateTime? DataUltimaSolicitacao { get; set; }
}
