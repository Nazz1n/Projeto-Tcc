using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Solicitacoes;

// O atendente responsável é sempre o usuário autenticado (JWT); por isso
// não existe AtendenteId neste DTO.
public class SolicitacaoStatusAtualizacaoDto
{
    [Required(ErrorMessage = "Informe o novo status.")]
    [StringLength(30)]
    public string NovoStatus { get; set; } = string.Empty;
}
