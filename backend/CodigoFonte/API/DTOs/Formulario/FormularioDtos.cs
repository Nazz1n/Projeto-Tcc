using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Formulario;

public class FormularioCampoDto
{
    public string Chave { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
    public string TipoCampo { get; set; } = string.Empty;
    public bool Obrigatorio { get; set; }
}

public class FormularioDocumentoStatusDto
{
    public string Chave { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
    public bool Obrigatorio { get; set; }

    // Ausente | AguardandoValidacao | Validado | Reprovado
    public string Estado { get; set; } = string.Empty;
    public string? NomeOriginal { get; set; }
    public DateTime? DataEnvio { get; set; }
    public bool Reutilizado { get; set; }

    // CRLV é do veículo; os demais são do motorista.
    public bool DoVeiculo { get; set; }
}

public class FormularioVeiculoDto
{
    public int VeiculoId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public decimal CapacidadeToneladas { get; set; }
}

public class FormularioDadosAtuaisDto
{
    public string Telefone { get; set; } = string.Empty;
    public string? NomeCompleto { get; set; }
    public string? Email { get; set; }
    public string? CpfMascarado { get; set; }
    public int? VeiculoSelecionadoId { get; set; }
    public List<FormularioVeiculoDto> Veiculos { get; set; } = new();
}

public class FormularioRespostaDto
{
    public int SolicitacaoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DataExpiracao { get; set; }
    public List<FormularioCampoDto> Campos { get; set; } = new();
    public List<FormularioDocumentoStatusDto> Documentos { get; set; } = new();
    public FormularioDadosAtuaisDto Dados { get; set; } = new();
}

public class FormularioDadosDto
{
    [StringLength(150)]
    public string? NomeCompleto { get; set; }

    [StringLength(254)]
    public string? Email { get; set; }

    // Só é alterado se vier preenchido e sem máscara.
    [StringLength(20)]
    public string? Cpf { get; set; }

    // Informe um veículo já cadastrado OU placa + capacidade.
    public int? VeiculoId { get; set; }

    [StringLength(8)]
    public string? Placa { get; set; }

    public decimal? CapacidadeToneladas { get; set; }
}

public class FormularioDocumentoEnvioDto
{
    [Required]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    public IFormFile? Arquivo { get; set; }
}

public class FormularioEnvioRespostaDto
{
    public string Status { get; set; } = string.Empty;
    public List<string> CamposPendentes { get; set; } = new();
    public List<string> DocumentosPendentes { get; set; } = new();
    public string Mensagem { get; set; } = string.Empty;
}
