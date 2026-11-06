using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.CamposFormulario;

// RF05: o administrador configura rótulo e obrigatoriedade. Categoria e
// tipo são fixos, porque cada campo predefinido tem um local próprio de
// armazenamento no modelo de dados.
public class CampoFormularioAtualizacaoDto
{
    [Required]
    [StringLength(150)]
    public string Rotulo { get; set; } = string.Empty;

    public bool Obrigatorio { get; set; }
}
