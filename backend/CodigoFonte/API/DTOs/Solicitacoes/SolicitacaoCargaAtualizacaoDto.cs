using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Solicitacoes;

public class SolicitacaoCargaAtualizacaoDto
{
    // Nulo remove a associação com a carga.
    [Range(1, int.MaxValue, ErrorMessage = "Informe uma carga válida.")]
    public int? CargaId { get; set; }
}
