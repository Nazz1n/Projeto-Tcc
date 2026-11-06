
namespace Captacao.Domain.Entidades;

public class Motorista
{
    public int MotoristaId { get; set; }

    public string Telefone { get; set; } = string.Empty;

    // CPF armazenado de forma criptografada.
    public string? Cpf { get; set; }

    // Hash utilizado para comparação e identificação.
    public string? CpfHash { get; set; }

    // Nulo até o formulário ser preenchido: no primeiro contato pelo WhatsApp
    // só o telefone é conhecido (MER/DER, seção 4.3.2).
    public string? NomeCompleto { get; set; }

    public string? Email { get; set; }

    public string? Rntrc { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    // Relacionamentos
    public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();

    public ICollection<Solicitacao> Solicitacoes { get; set; } = new List<Solicitacao>();

    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();


}
