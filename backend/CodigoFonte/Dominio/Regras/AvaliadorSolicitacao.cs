using System.Globalization;
using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;

namespace Captacao.Domain.Regras;

public enum SituacaoCompatibilidade
{
    NaoAplicavel,   // sem carga associada
    SemRestricao,   // carga sem capacidade mínima
    Compativel,
    Incompativel,
    NaoVerificada   // há mínimo, mas falta a capacidade do veículo
}

public enum EstadoDocumento { Ausente, AguardandoValidacao, Validado, Reprovado }

public sealed class AvaliacaoDocumento
{
    public string Chave { get; init; } = string.Empty;
    public string Rotulo { get; init; } = string.Empty;
    public bool Obrigatorio { get; init; }
    public EstadoDocumento Estado { get; init; }
    public int? DocumentoId { get; init; }
    public string? NomeOriginal { get; init; }
    public DateTime? DataEnvio { get; init; }
    public bool Reutilizado { get; init; }
}

public sealed class AvaliacaoSolicitacao
{
    public string Prioridade { get; init; } = PrioridadeSolicitacao.Baixa;
    public SituacaoCompatibilidade Compatibilidade { get; init; }
    public string DescricaoCompatibilidade { get; init; } = string.Empty;
    public IReadOnlyList<string> CamposPendentes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AvaliacaoDocumento> Documentos { get; init; } = Array.Empty<AvaliacaoDocumento>();
    public bool CamposObrigatoriosPreenchidos => CamposPendentes.Count == 0;
    public bool PossuiDocumentoObrigatorioAusente { get; init; }
    public bool PossuiDocumentoObrigatorioReprovado { get; init; }
    public bool PossuiDocumentoObrigatorioAguardandoValidacao { get; init; }
    // Documentos obrigatórios ainda não validados (número do cartão do Kanban).
    public int PendenciasDocumentais { get; init; }
}

public static class AvaliadorSolicitacao
{
    // Função pura: Motorista, Veiculo e Carga devem estar carregados na
    // solicitação quando existirem.
    public static AvaliacaoSolicitacao Avaliar(
        Solicitacao solicitacao,
        IReadOnlyCollection<CampoFormulario> configuracao,
        IReadOnlyCollection<Documento> documentosDoMotorista)
    {
        var camposPendentes = configuracao
            .Where(c => c.Ativo && c.Obrigatorio && EhCategoria(c, CategoriaCampoFormulario.Campo))
            .Where(c => !CampoPreenchido(solicitacao, c.Chave))
            .Select(c => c.Chave)
            .ToList();

        var documentos = configuracao
            .Where(c => c.Ativo && EhCategoria(c, CategoriaCampoFormulario.Documento))
            .Select(c => AvaliarDocumento(solicitacao, c, documentosDoMotorista))
            .ToList();

        var obrigatorios = documentos.Where(d => d.Obrigatorio).ToList();
        var ausente = obrigatorios.Any(d => d.Estado == EstadoDocumento.Ausente);
        var reprovado = obrigatorios.Any(d => d.Estado == EstadoDocumento.Reprovado);
        var aguardando = obrigatorios.Any(d => d.Estado == EstadoDocumento.AguardandoValidacao);
        var pendencias = obrigatorios.Count(d => d.Estado != EstadoDocumento.Validado);

        var compat = AvaliarCompatibilidade(solicitacao);

        var prioridade = Classificar(
            solicitacao.Status, camposPendentes.Count == 0,
            ausente, reprovado, aguardando, compat);

        return new AvaliacaoSolicitacao
        {
            Prioridade = prioridade,
            Compatibilidade = compat,
            DescricaoCompatibilidade = Descrever(solicitacao, compat),
            CamposPendentes = camposPendentes,
            Documentos = documentos,
            PossuiDocumentoObrigatorioAusente = ausente,
            PossuiDocumentoObrigatorioReprovado = reprovado,
            PossuiDocumentoObrigatorioAguardandoValidacao = aguardando,
            PendenciasDocumentais = pendencias
        };
    }

    // RF07.
    //  Baixa: etapa inicial/formulário incompleto, documento obrigatório
    //         reprovado ou veículo incompatível.
    //  Média: formulário enviado e campos preenchidos, mas documento
    //         obrigatório ausente/aguardando validação, ou compatibilidade
    //         não verificada.
    //  Alta:  tudo preenchido, documentos obrigatórios validados e
    //         compatibilidade confirmada (quando aplicável).
    public static string Classificar(
        string status,
        bool camposObrigatoriosPreenchidos,
        bool documentoAusente,
        bool documentoReprovado,
        bool documentoAguardandoValidacao,
        SituacaoCompatibilidade compatibilidade)
    {
        var formularioEnviado = !StatusSolicitacao.FormularioNaoConcluido(status);

        if (!formularioEnviado || !camposObrigatoriosPreenchidos ||
            documentoReprovado || compatibilidade == SituacaoCompatibilidade.Incompativel)
            return PrioridadeSolicitacao.Baixa;

        if (documentoAusente || documentoAguardandoValidacao ||
            compatibilidade == SituacaoCompatibilidade.NaoVerificada)
            return PrioridadeSolicitacao.Media;

        return PrioridadeSolicitacao.Alta;
    }

    public static SituacaoCompatibilidade AvaliarCompatibilidade(Solicitacao s)
    {
        if (s.Carga == null) return SituacaoCompatibilidade.NaoAplicavel;

        var minima = s.Carga.CapacidadeMinimaToneladas;
        if (!minima.HasValue) return SituacaoCompatibilidade.SemRestricao;

        if (s.Veiculo == null || s.Veiculo.CapacidadeToneladas <= 0)
            return SituacaoCompatibilidade.NaoVerificada;

        return s.Veiculo.CapacidadeToneladas >= minima.Value
            ? SituacaoCompatibilidade.Compativel
            : SituacaoCompatibilidade.Incompativel;
    }

    private static string Descrever(Solicitacao s, SituacaoCompatibilidade c)
    {
        var minima = Fmt(s.Carga?.CapacidadeMinimaToneladas);
        var cap = Fmt(s.Veiculo?.CapacidadeToneladas);
        return c switch
        {
            SituacaoCompatibilidade.Compativel => $"O veículo suporta {cap} t e a carga exige no mínimo {minima} t.",
            SituacaoCompatibilidade.Incompativel => $"O veículo suporta {cap} t, abaixo do mínimo de {minima} t exigido pela carga.",
            SituacaoCompatibilidade.NaoVerificada => "A capacidade do veículo não foi informada; a compatibilidade não pôde ser verificada.",
            SituacaoCompatibilidade.SemRestricao => "A carga não possui capacidade mínima definida.",
            _ => "Nenhuma carga associada à solicitação."
        };
    }

    private static string Fmt(decimal? v) =>
        v.HasValue ? v.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-";

    private static bool CampoPreenchido(Solicitacao s, string chave)
    {
        var m = s.Motorista;
        var v = s.Veiculo;
        return chave.ToLowerInvariant() switch
        {
            CatalogoFormulario.Nome => !string.IsNullOrWhiteSpace(m?.NomeCompleto),
            CatalogoFormulario.Cpf => !string.IsNullOrWhiteSpace(m?.Cpf),
            CatalogoFormulario.Telefone => !string.IsNullOrWhiteSpace(m?.Telefone),
            CatalogoFormulario.Email => !string.IsNullOrWhiteSpace(m?.Email),
            CatalogoFormulario.Placa => v != null && !string.IsNullOrWhiteSpace(v.Placa),
            CatalogoFormulario.Capacidade => v != null && v.CapacidadeToneladas > 0,
            _ => true
        };
    }

    // 1) arquivo mais recente enviado NESTA solicitação (mesmo reprovado);
    // 2) senão, o mais recente do motorista que não esteja reprovado (RF13);
    // 3) senão, ausente. Documentos do veículo só valem para o veículo
    //    selecionado.
    private static AvaliacaoDocumento AvaliarDocumento(
        Solicitacao s, CampoFormulario cfg, IReadOnlyCollection<Documento> docs)
    {
        var candidatos = docs
            .Where(d => d.TipoDocumento.Equals(cfg.Chave, StringComparison.OrdinalIgnoreCase)
                        && (d.VeiculoId == null || d.VeiculoId == s.VeiculoId))
            .ToList();

        var daSolicitacao = candidatos
            .Where(d => d.SolicitacaoId == s.SolicitacaoId)
            .OrderByDescending(d => d.DataEnvio).ThenByDescending(d => d.DocumentoId)
            .FirstOrDefault();

        var efetivo = daSolicitacao ?? candidatos
            .Where(d => !Igual(d.StatusValidacao, StatusDocumento.Reprovado))
            .OrderByDescending(d => d.DataEnvio).ThenByDescending(d => d.DocumentoId)
            .FirstOrDefault();

        var estado = efetivo == null ? EstadoDocumento.Ausente
            : Igual(efetivo.StatusValidacao, StatusDocumento.Validado) ? EstadoDocumento.Validado
            : Igual(efetivo.StatusValidacao, StatusDocumento.Reprovado) ? EstadoDocumento.Reprovado
            : EstadoDocumento.AguardandoValidacao;

        return new AvaliacaoDocumento
        {
            Chave = cfg.Chave, Rotulo = cfg.Rotulo, Obrigatorio = cfg.Obrigatorio,
            Estado = estado, DocumentoId = efetivo?.DocumentoId,
            NomeOriginal = efetivo?.NomeOriginal, DataEnvio = efetivo?.DataEnvio,
            Reutilizado = efetivo != null && efetivo.SolicitacaoId != s.SolicitacaoId
        };
    }

    private static bool EhCategoria(CampoFormulario c, string cat) =>
        string.Equals(c.Categoria, cat, StringComparison.OrdinalIgnoreCase);

    private static bool Igual(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
