
using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Solicitacoes;

public class SolicitacaoCadastroDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Informe um motorista válido.")]
    public int MotoristaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Informe um veículo válido.")]
    public int? VeiculoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Informe uma carga válida.")]
    public int? CargaId { get; set; }
}
