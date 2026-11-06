
using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Solicitacoes;

public class SolicitacaoAtendenteAtualizacaoDto
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Informe um atendente válido.")]
    public int AtendenteId { get; set; }
}
