
using Captacao.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Infrastructure.Persistencia
{
    public class CaptacaoDbContext : DbContext
    {
        public CaptacaoDbContext(DbContextOptions<CaptacaoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Motorista> Motoristas { get; set; }
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<Atendente> Atendentes { get; set; }
        public DbSet<Carga> Cargas { get; set; }
        public DbSet<Solicitacao> Solicitacoes { get; set; }
        public DbSet<Documento> Documentos { get; set; }

        public DbSet<LinkAcesso> LinksAcesso { get; set; }
        public DbSet<HistoricoStatus> HistoricosStatus { get; set; }
        public DbSet<CampoFormulario> CamposFormulario { get; set; }
        public DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MOTORISTA
            modelBuilder.Entity<Motorista>(entity =>
            {
                entity.HasKey(e => e.MotoristaId);

                entity.HasIndex(e => e.Telefone)
                    .IsUnique();

                entity.HasIndex(e => e.CpfHash)
                    .IsUnique();

                entity.Property(e => e.Cpf)
                    .HasMaxLength(500);

                // Nulo até o motorista preencher o formulário.
                entity.Property(e => e.NomeCompleto)
                    .HasMaxLength(150);

                entity.Property(e => e.Telefone)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasMaxLength(254);

                entity.Property(e => e.Rntrc)
                    .HasMaxLength(20);

                entity.Property(e => e.CpfHash)
                    .HasMaxLength(255);
            });

            // VEICULO
            modelBuilder.Entity<Veiculo>(entity =>
            {
                entity.HasKey(e => e.VeiculoId);

                entity.HasIndex(e => e.Placa)
                    .IsUnique();

                entity.Property(e => e.Placa)
                    .HasMaxLength(7)
                    .IsRequired();

                entity.Property(e => e.TipoVeiculo)
                    .HasMaxLength(50);

                entity.Property(e => e.CapacidadeToneladas)
                    .HasPrecision(8, 2);

                entity.HasOne(e => e.Motorista)
                    .WithMany(e => e.Veiculos)
                    .HasForeignKey(e => e.MotoristaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ATENDENTE
           modelBuilder.Entity<Atendente>(entity =>
            {
                entity.HasKey(e => e.AtendenteId);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.Nome)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.SenhaHash)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Perfil)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Ativo)
                    .HasDefaultValue(true)
                    .IsRequired();
            });

            // CARGA
            modelBuilder.Entity<Carga>(entity =>
            {
                entity.HasKey(e => e.CargaId);

                entity.Property(e => e.Origem)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.Destino)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.CapacidadeMinimaToneladas)
                    .HasPrecision(8, 2);

                entity.Property(e => e.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasOne(e => e.Atendente)
                    .WithMany(e => e.Cargas)
                    .HasForeignKey(e => e.AtendenteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SOLICITACAO
            modelBuilder.Entity<Solicitacao>(entity =>
            {
                entity.HasKey(e => e.SolicitacaoId);

                entity.Property(e => e.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(e => e.Prioridade)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasOne(e => e.Motorista)
                    .WithMany(e => e.Solicitacoes)
                    .HasForeignKey(e => e.MotoristaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Veiculo)
                    .WithMany(e => e.Solicitacoes)
                    .HasForeignKey(e => e.VeiculoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Carga)
                    .WithMany(e => e.Solicitacoes)
                    .HasForeignKey(e => e.CargaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Atendente)
                    .WithMany(e => e.Solicitacoes)
                    .HasForeignKey(e => e.AtendenteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // DOCUMENTO
            modelBuilder.Entity<Documento>(entity =>
            {
                entity.HasKey(e => e.DocumentoId);

                entity.Property(e => e.TipoDocumento)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.ChaveArquivo)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.NomeOriginal)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.TipoConteudo)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.StatusValidacao)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasOne(e => e.Motorista)
                    .WithMany(e => e.Documentos)
                    .HasForeignKey(e => e.MotoristaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Veiculo)
                    .WithMany(e => e.Documentos)
                    .HasForeignKey(e => e.VeiculoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Solicitacao)
                    .WithMany(e => e.Documentos)
                    .HasForeignKey(e => e.SolicitacaoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.MotoristaId);
                entity.HasIndex(e => e.VeiculoId);
                entity.HasIndex(e => e.SolicitacaoId);
            });

    

            // LINK DE ACESSO
            modelBuilder.Entity<LinkAcesso>(entity =>
            {
                entity.HasKey(e => e.LinkAcessoId);

                entity.HasIndex(e => e.TokenHash)
                    .IsUnique();

                entity.Property(e => e.TokenHash)
                    .HasMaxLength(255)
                    .IsRequired();

                // Garante consumo único do link mesmo com envios
                // simultâneos (UPDATE ... WHERE DataUtilizacao IS NULL).
                entity.Property(e => e.DataUtilizacao)
                    .IsConcurrencyToken();

                entity.HasOne(e => e.Solicitacao)
                    .WithMany(e => e.LinksAcesso)
                    .HasForeignKey(e => e.SolicitacaoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // HISTORICO DE STATUS
            modelBuilder.Entity<HistoricoStatus>(entity =>
            {
                entity.HasKey(e => e.HistoricoStatusId);

                // Nulo no registro de criação da solicitação.
                entity.Property(e => e.StatusAnterior)
                    .HasMaxLength(30);

                entity.Property(e => e.NovoStatus)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasOne(e => e.Solicitacao)
                    .WithMany(e => e.HistoricosStatus)
                    .HasForeignKey(e => e.SolicitacaoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Atendente)
                    .WithMany(e => e.HistoricosStatus)
                    .HasForeignKey(e => e.AtendenteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // CAMPO DO FORMULARIO
            modelBuilder.Entity<CampoFormulario>(entity =>
            {
                entity.HasKey(e => e.CampoFormularioId);

                entity.HasIndex(e => e.Chave)
                    .IsUnique();

                entity.Property(e => e.Chave)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Rotulo)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.Categoria)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.TipoCampo)
                    .HasMaxLength(30)
                    .IsRequired();
            });

            // REGISTRO DE AUDITORIA
            modelBuilder.Entity<RegistroAuditoria>(entity =>
            {
                entity.HasKey(e => e.RegistroAuditoriaId);

                entity.Property(e => e.Acao)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Detalhes)
                    .HasColumnType("longtext");

                entity.HasOne(e => e.Atendente)
                    .WithMany(e => e.RegistrosAuditoria)
                    .HasForeignKey(e => e.AtendenteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
