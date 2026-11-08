using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;
using Captacao.Domain.Regras;

namespace Captacao.Tests.Dominio;

public class FormatacaoDadosTests
{
    [Fact]
    public void MascararCpf_MostraSomenteDigitosDoMeio() =>
        Assert.Equal("***.982.***-25", FormatacaoDados.MascararCpf("529.982.247-25"));

    [Fact]
    public void MascararCpf_Invalido_RetornaNulo() =>
        Assert.Null(FormatacaoDados.MascararCpf("123"));

    [Theory]
    [InlineData("abc-1234", "ABC1234", true)]
    [InlineData("ABC1D23", "ABC1D23", true)]
    [InlineData("AB12345", "AB12345", false)]
    public void Placa_NormalizaEValida(string entrada, string esperada, bool valida)
    {
        var placa = FormatacaoDados.NormalizarPlaca(entrada);
        Assert.Equal(esperada, placa);
        Assert.Equal(valida, FormatacaoDados.PlacaValida(placa));
    }

    [Fact]
    public void Telefone_RemoveFormatacao() =>
        Assert.Equal("16991234567", FormatacaoDados.NormalizarTelefone("(16) 99123-4567"));
}

public class AvaliadorSolicitacaoTests
{
    private static List<CampoFormulario> Config(bool crlvObrigatorio = false) =>
        CatalogoFormulario.Padrao.Select(p => new CampoFormulario
        {
            Chave = p.Chave,
            Rotulo = p.Rotulo,
            Categoria = p.Categoria,
            TipoCampo = p.TipoCampo,
            Obrigatorio = p.Chave == CatalogoFormulario.Crlv ? crlvObrigatorio : p.ObrigatorioPadrao,
            Ativo = true
        }).ToList();

    private static Solicitacao Completa(string status, decimal capacidade = 30) => new()
    {
        SolicitacaoId = 1,
        Status = status,
        VeiculoId = 10,
        Motorista = new Motorista
        {
            MotoristaId = 1,
            Telefone = "16991234567",
            NomeCompleto = "João",
            Cpf = "cifrado"
        },
        Veiculo = new Veiculo { VeiculoId = 10, MotoristaId = 1, Placa = "ABC1234", CapacidadeToneladas = capacidade }
    };

    private static Documento Doc(string tipo, string status, int id) => new()
    {
        DocumentoId = id,
        MotoristaId = 1,
        SolicitacaoId = 1,
        VeiculoId = tipo == CatalogoFormulario.Crlv ? 10 : null,
        TipoDocumento = tipo,
        StatusValidacao = status,
        DataEnvio = DateTime.UtcNow.AddMinutes(id)
    };

    private static List<Documento> Todos(string status) => new()
    {
        Doc(CatalogoFormulario.Cnh, status, 1),
        Doc(CatalogoFormulario.Crlv, status, 2),
        Doc(CatalogoFormulario.ComprovanteRntrc, status, 3)
    };

    [Fact]
    public void FormularioNaoEnviado_PrioridadeBaixa()
    {
        var a = AvaliadorSolicitacao.Avaliar(Completa(StatusSolicitacao.AguardandoPreenchimento), Config(), new List<Documento>());
        Assert.Equal(PrioridadeSolicitacao.Baixa, a.Prioridade);
    }

    [Fact]
    public void TudoValidado_PrioridadeAlta()
    {
        var a = AvaliadorSolicitacao.Avaliar(Completa(StatusSolicitacao.ProntoParaAnalise), Config(true), Todos(StatusDocumento.Validado));
        Assert.Equal(PrioridadeSolicitacao.Alta, a.Prioridade);
        Assert.Equal(0, a.PendenciasDocumentais);
    }

    [Fact]
    public void DocumentoAguardandoValidacao_PrioridadeMedia()
    {
        var a = AvaliadorSolicitacao.Avaliar(Completa(StatusSolicitacao.ProntoParaAnalise), Config(true), Todos(StatusDocumento.Pendente));
        Assert.Equal(PrioridadeSolicitacao.Media, a.Prioridade);
    }

    [Fact]
    public void DocumentoObrigatorioAusente_PrioridadeMedia()
    {
        var a = AvaliadorSolicitacao.Avaliar(Completa(StatusSolicitacao.ComPendencias), Config(true), new List<Documento>());
        Assert.True(a.PossuiDocumentoObrigatorioAusente);
        Assert.Equal(PrioridadeSolicitacao.Media, a.Prioridade);
    }

    [Fact]
    public void DocumentoReprovado_PrioridadeBaixa()
    {
        var docs = Todos(StatusDocumento.Validado);
        docs[0].StatusValidacao = StatusDocumento.Reprovado;

        var a = AvaliadorSolicitacao.Avaliar(Completa(StatusSolicitacao.EmAnalise), Config(true), docs);

        Assert.True(a.PossuiDocumentoObrigatorioReprovado);
        Assert.Equal(PrioridadeSolicitacao.Baixa, a.Prioridade);
    }

    [Fact]
    public void CampoObrigatorioFaltando_PrioridadeBaixa()
    {
        var s = Completa(StatusSolicitacao.ProntoParaAnalise);
        s.Motorista.NomeCompleto = null;

        var a = AvaliadorSolicitacao.Avaliar(s, Config(), Todos(StatusDocumento.Validado));

        Assert.Contains(CatalogoFormulario.Nome, a.CamposPendentes);
        Assert.Equal(PrioridadeSolicitacao.Baixa, a.Prioridade);
    }

    [Fact]
    public void DocumentoInativo_NaoContaComoPendencia()
    {
        var config = Config(true);
        config.Single(c => c.Chave == CatalogoFormulario.Crlv).Ativo = false;

        var docs = Todos(StatusDocumento.Validado).Where(d => d.TipoDocumento != CatalogoFormulario.Crlv).ToList();

        var a = AvaliadorSolicitacao.Avaliar(Completa(StatusSolicitacao.ProntoParaAnalise), config, docs);

        Assert.Equal(PrioridadeSolicitacao.Alta, a.Prioridade);
    }

    [Fact]
    public void VeiculoMenorQueMinimoDaCarga_Incompativel_PrioridadeBaixa()
    {
        var s = Completa(StatusSolicitacao.ProntoParaAnalise, capacidade: 10);
        s.Carga = new Carga { CargaId = 1, CapacidadeMinimaToneladas = 25 };

        var a = AvaliadorSolicitacao.Avaliar(s, Config(true), Todos(StatusDocumento.Validado));

        Assert.Equal(SituacaoCompatibilidade.Incompativel, a.Compatibilidade);
        Assert.Equal(PrioridadeSolicitacao.Baixa, a.Prioridade);
    }

    [Theory]
    [InlineData(30, 25, SituacaoCompatibilidade.Compativel)]
    [InlineData(25, 25, SituacaoCompatibilidade.Compativel)]
    [InlineData(20, 25, SituacaoCompatibilidade.Incompativel)]
    public void Compatibilidade_ComparaCapacidade(double capacidade, double minimo, SituacaoCompatibilidade esperado)
    {
        var s = Completa(StatusSolicitacao.ProntoParaAnalise, (decimal)capacidade);
        s.Carga = new Carga { CapacidadeMinimaToneladas = (decimal)minimo };

        Assert.Equal(esperado, AvaliadorSolicitacao.AvaliarCompatibilidade(s));
    }

    [Fact]
    public void SemCarga_NaoAplicavel() =>
        Assert.Equal(SituacaoCompatibilidade.NaoAplicavel,
            AvaliadorSolicitacao.AvaliarCompatibilidade(Completa(StatusSolicitacao.NovoContato)));

    [Fact]
    public void CargaSemMinimo_SemRestricao()
    {
        var s = Completa(StatusSolicitacao.NovoContato);
        s.Carga = new Carga { CapacidadeMinimaToneladas = null };

        Assert.Equal(SituacaoCompatibilidade.SemRestricao, AvaliadorSolicitacao.AvaliarCompatibilidade(s));
    }

    [Fact]
    public void CargaComMinimoSemVeiculo_NaoVerificada_PrioridadeMedia()
    {
        var s = Completa(StatusSolicitacao.ProntoParaAnalise);
        s.Veiculo = null;
        s.VeiculoId = null;
        s.Carga = new Carga { CapacidadeMinimaToneladas = 20 };

        var config = Config();
        config.Single(c => c.Chave == CatalogoFormulario.Placa).Obrigatorio = false;
        config.Single(c => c.Chave == CatalogoFormulario.Capacidade).Obrigatorio = false;

        var docs = Todos(StatusDocumento.Validado).Where(d => d.TipoDocumento != CatalogoFormulario.Crlv).ToList();

        var a = AvaliadorSolicitacao.Avaliar(s, config, docs);

        Assert.Equal(SituacaoCompatibilidade.NaoVerificada, a.Compatibilidade);
        Assert.Equal(PrioridadeSolicitacao.Media, a.Prioridade);
    }

    [Fact]
    public void DocumentoReprovadoAnterior_ReaproveitaOutroValido()
    {
        // RF13: documento do motorista de outra solicitação é reaproveitado.
        var antigo = Doc(CatalogoFormulario.Cnh, StatusDocumento.Validado, 1);
        antigo.SolicitacaoId = 99;

        var a = AvaliadorSolicitacao.Avaliar(Completa(StatusSolicitacao.ProntoParaAnalise), Config(),
            new List<Documento> { antigo });

        var cnh = a.Documentos.Single(d => d.Chave == CatalogoFormulario.Cnh);
        Assert.Equal(EstadoDocumento.Validado, cnh.Estado);
    }
}

public class RegrasStatusTests
{
    [Fact]
    public void ContratacaoPermitida_BloqueiaDocumentoObrigatorioAguardandoValidacao()
    {
        var avaliacao = new AvaliacaoSolicitacao
        {
            Documentos = new[]
            {
                new AvaliacaoDocumento
                {
                    Chave = CatalogoFormulario.Cnh,
                    Rotulo = "CNH",
                    Obrigatorio = true,
                    Estado = EstadoDocumento.AguardandoValidacao
                }
            }
        };

        Assert.False(RegrasStatus.ContratacaoPermitida(avaliacao, out var motivo));
        Assert.Contains("CNH", motivo);
    }

    [Fact]
    public void ContratacaoPermitida_BloqueiaDocumentoObrigatorioReprovado()
    {
        var avaliacao = new AvaliacaoSolicitacao
        {
            Documentos = new[]
            {
                new AvaliacaoDocumento
                {
                    Chave = CatalogoFormulario.Cnh,
                    Rotulo = "CNH",
                    Obrigatorio = true,
                    Estado = EstadoDocumento.Reprovado
                }
            }
        };

        Assert.False(RegrasStatus.ContratacaoPermitida(avaliacao, out var motivo));
        Assert.Contains("CNH", motivo);
    }

    [Fact]
    public void ContratacaoPermitida_BloqueiaCamposObrigatoriosPendentes()
    {
        var avaliacao = new AvaliacaoSolicitacao
        {
            CamposPendentes = new[] { CatalogoFormulario.Cpf }
        };

        Assert.False(RegrasStatus.ContratacaoPermitida(avaliacao, out var motivo));
        Assert.Contains(CatalogoFormulario.Cpf, motivo);
    }

    [Fact]
    public void ContratacaoPermitida_IgnoraDocumentoOpcionalPendente()
    {
        var avaliacao = new AvaliacaoSolicitacao
        {
            Documentos = new[]
            {
                new AvaliacaoDocumento
                {
                    Chave = "documento_adicional",
                    Rotulo = "Documento adicional",
                    Obrigatorio = false,
                    Estado = EstadoDocumento.AguardandoValidacao
                }
            }
        };

        Assert.True(RegrasStatus.ContratacaoPermitida(avaliacao, out var motivo));
        Assert.Null(motivo);
    }

    [Fact]
    public void ContratacaoPermitida_AceitaCamposEDocumentosObrigatoriosValidos()
    {
        var avaliacao = new AvaliacaoSolicitacao
        {
            Documentos = new[]
            {
                new AvaliacaoDocumento
                {
                    Chave = CatalogoFormulario.Cnh,
                    Rotulo = "CNH",
                    Obrigatorio = true,
                    Estado = EstadoDocumento.Validado
                }
            }
        };

        Assert.True(RegrasStatus.ContratacaoPermitida(avaliacao, out var motivo));
        Assert.Null(motivo);
    }

    [Theory]
    [InlineData(StatusSolicitacao.ComPendencias, StatusSolicitacao.EmAnalise, true)]
    [InlineData(StatusSolicitacao.ProntoParaAnalise, StatusSolicitacao.EmAnalise, true)]
    [InlineData(StatusSolicitacao.EmAnalise, StatusSolicitacao.Contratado, true)]
    [InlineData(StatusSolicitacao.ProntoParaAnalise, StatusSolicitacao.Contratado, false)]
    [InlineData(StatusSolicitacao.NovoContato, StatusSolicitacao.EmAnalise, false)]
    [InlineData(StatusSolicitacao.AguardandoPreenchimento, StatusSolicitacao.Encerrado, true)]
    [InlineData(StatusSolicitacao.Contratado, StatusSolicitacao.Encerrado, false)]
    [InlineData(StatusSolicitacao.Encerrado, StatusSolicitacao.EmAnalise, false)]
    public void TransicaoManual(string atual, string novo, bool permitida)
    {
        Assert.Equal(permitida, RegrasStatus.TransicaoManualPermitida(atual, novo, out var motivo));
        if (!permitida) Assert.False(string.IsNullOrEmpty(motivo));
    }

    [Theory]
    [InlineData(StatusSolicitacao.NovoContato, true)]
    [InlineData(StatusSolicitacao.AguardandoPreenchimento, true)]
    [InlineData(StatusSolicitacao.ComPendencias, true)]
    [InlineData(StatusSolicitacao.EmAnalise, false)]
    [InlineData(StatusSolicitacao.Contratado, false)]
    public void EnvioAlteraStatus_SoNasEtapasIniciais(string atual, bool altera) =>
        Assert.Equal(altera, RegrasStatus.EnvioAlteraStatus(atual));

    [Fact]
    public void Normalizar_AceitaRotuloComAcentoEEspacos()
    {
        Assert.Equal(StatusSolicitacao.ComPendencias, StatusSolicitacao.Normalizar("Com pendências"));
        Assert.Equal(StatusSolicitacao.EmAnalise, StatusSolicitacao.Normalizar("em_analise"));
        Assert.Null(StatusSolicitacao.Normalizar("inexistente"));
    }
}

public class CalculadoraIndicadoresTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SemDados_RetornaZeros()
    {
        var r = CalculadoraIndicadores.Calcular(new List<SolicitacaoResumo>(), new List<TransicaoResumo>());

        Assert.Equal(0, r.TotalSolicitacoes);
        Assert.Equal(0, r.TaxaConversaoPercentual);
        Assert.Null(r.TempoMedioAteProntaParaAnaliseMinutos);
    }

    [Fact]
    public void CalculaConversaoCadastroCompletoETempos()
    {
        var sol = new List<SolicitacaoResumo>
        {
            new(1, StatusSolicitacao.Contratado, T0),
            new(2, StatusSolicitacao.ProntoParaAnalise, T0),
            new(3, StatusSolicitacao.NovoContato, T0),
            new(4, StatusSolicitacao.Encerrado, T0)
        };

        var hist = new List<TransicaoResumo>
        {
            new(1, StatusSolicitacao.ProntoParaAnalise, T0.AddMinutes(30)),
            new(1, StatusSolicitacao.Contratado, T0.AddMinutes(120)),
            new(2, StatusSolicitacao.ProntoParaAnalise, T0.AddMinutes(10))
        };

        var r = CalculadoraIndicadores.Calcular(sol, hist);

        Assert.Equal(4, r.TotalSolicitacoes);
        Assert.Equal(2, r.CadastrosCompletos);
        Assert.Equal(50, r.PercentualCadastrosCompletos);
        Assert.Equal(1, r.Contratadas);
        Assert.Equal(25, r.TaxaConversaoPercentual);
        Assert.Equal(20, r.TempoMedioAteProntaParaAnaliseMinutos);
        Assert.Equal(120, r.TempoMedioTotalAtendimentoMinutos);
        Assert.Equal(1, r.PorStatus[StatusSolicitacao.NovoContato]);
    }
}
