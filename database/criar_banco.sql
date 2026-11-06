-- =====================================================================
-- Sistema de Gestão de Captação e Atendimento de Caminhoneiros
-- =====================================================================

--   mysql -u <usuario> -p < database/criar_banco.sql
-- ou, em um banco já criado:
--   mysql -u <usuario> -p <nome_do_banco> < database/criar_banco.sql
--   (neste caso, comente as duas primeiras instruções abaixo)



CREATE DATABASE IF NOT EXISTS `captacao_caminhoneiros`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE `captacao_caminhoneiros`;

SET FOREIGN_KEY_CHECKS = 0;


-- Atendentes: 

CREATE TABLE `Atendentes` (
  `AtendenteId` int NOT NULL AUTO_INCREMENT,
  `Nome`        varchar(150) NOT NULL,
  `Email`       varchar(150) NOT NULL,
  `SenhaHash`   varchar(255) NOT NULL,
  `Perfil`      varchar(50)  NOT NULL,
  `Ativo`       tinyint(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (`AtendenteId`),
  UNIQUE KEY `IX_Atendentes_Email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE `CamposFormulario` (
  `CampoFormularioId` int NOT NULL AUTO_INCREMENT,
  `Chave`        varchar(100) NOT NULL,
  `Rotulo`       varchar(150) NOT NULL,
  `Categoria`    varchar(50)  NOT NULL,
  `TipoCampo`    varchar(30)  NOT NULL,
  `Obrigatorio`  tinyint(1)   NOT NULL,
  `Ativo`        tinyint(1)   NOT NULL,
  `DataCriacao`  datetime(6)  NOT NULL,
  PRIMARY KEY (`CampoFormularioId`),
  UNIQUE KEY `IX_CamposFormulario_Chave` (`Chave`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE `Motoristas` (
  `MotoristaId`      int NOT NULL AUTO_INCREMENT,
  `Telefone`         varchar(20)  NOT NULL,
  `Cpf`              varchar(500) NULL,
  `CpfHash`          varchar(255) NULL,
  `NomeCompleto`     varchar(150) NULL,
  `Email`            varchar(254) NULL,
  `Rntrc`            varchar(20)  NULL,
  `DataCadastro`     datetime(6)  NOT NULL,
  `DataAtualizacao`  datetime(6)  NULL,
  PRIMARY KEY (`MotoristaId`),
  UNIQUE KEY `IX_Motoristas_Telefone` (`Telefone`),
  UNIQUE KEY `IX_Motoristas_CpfHash` (`CpfHash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE `Cargas` (
  `CargaId`                    int NOT NULL AUTO_INCREMENT,
  `Origem`                     varchar(150) NOT NULL,
  `Destino`                    varchar(150) NOT NULL,
  `CapacidadeMinimaToneladas`  decimal(8,2) NULL,
  `Status`                     varchar(30)  NOT NULL,
  `DataCadastro`               datetime(6)  NOT NULL,
  `AtendenteId`                int NOT NULL,
  PRIMARY KEY (`CargaId`),
  KEY `IX_Cargas_AtendenteId` (`AtendenteId`),
  CONSTRAINT `FK_Cargas_Atendentes_AtendenteId`
    FOREIGN KEY (`AtendenteId`) REFERENCES `Atendentes` (`AtendenteId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE `Veiculos` (
  `VeiculoId`           int NOT NULL AUTO_INCREMENT,
  `Placa`               varchar(7)   NOT NULL,
  `CapacidadeToneladas` decimal(8,2) NOT NULL,
  `TipoVeiculo`         varchar(50)  NULL,
  `Ativo`               tinyint(1)   NOT NULL,
  `MotoristaId`         int NOT NULL,
  PRIMARY KEY (`VeiculoId`),
  UNIQUE KEY `IX_Veiculos_Placa` (`Placa`),
  KEY `IX_Veiculos_MotoristaId` (`MotoristaId`),
  CONSTRAINT `FK_Veiculos_Motoristas_MotoristaId`
    FOREIGN KEY (`MotoristaId`) REFERENCES `Motoristas` (`MotoristaId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE `Solicitacoes` (
  `SolicitacaoId`    int NOT NULL AUTO_INCREMENT,
  `Status`           varchar(30) NOT NULL,
  `Prioridade`       varchar(20) NOT NULL,
  `DataCriacao`      datetime(6) NOT NULL,
  `DataAtualizacao`  datetime(6) NULL,
  `MotoristaId`      int NOT NULL,
  `VeiculoId`        int NULL,
  `CargaId`          int NULL,
  `AtendenteId`      int NULL,
  PRIMARY KEY (`SolicitacaoId`),
  KEY `IX_Solicitacoes_AtendenteId` (`AtendenteId`),
  KEY `IX_Solicitacoes_CargaId` (`CargaId`),
  KEY `IX_Solicitacoes_MotoristaId` (`MotoristaId`),
  KEY `IX_Solicitacoes_VeiculoId` (`VeiculoId`),
  CONSTRAINT `FK_Solicitacoes_Atendentes_AtendenteId`
    FOREIGN KEY (`AtendenteId`) REFERENCES `Atendentes` (`AtendenteId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Solicitacoes_Cargas_CargaId`
    FOREIGN KEY (`CargaId`) REFERENCES `Cargas` (`CargaId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Solicitacoes_Motoristas_MotoristaId`
    FOREIGN KEY (`MotoristaId`) REFERENCES `Motoristas` (`MotoristaId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Solicitacoes_Veiculos_VeiculoId`
    FOREIGN KEY (`VeiculoId`) REFERENCES `Veiculos` (`VeiculoId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE `Documentos` (
  `DocumentoId`     int NOT NULL AUTO_INCREMENT,
  `TipoDocumento`   varchar(50)  NOT NULL,
  `ChaveArquivo`    varchar(255) NOT NULL,
  `NomeOriginal`    varchar(255) NOT NULL,
  `TipoConteudo`    varchar(100) NOT NULL,
  `TamanhoBytes`    bigint       NOT NULL,
  `StatusValidacao` varchar(20)  NOT NULL,
  `DataEnvio`       datetime(6)  NOT NULL,
  `DataValidacao`   datetime(6)  NULL,
  `MotoristaId`     int NOT NULL,
  `SolicitacaoId`   int NULL,
  `VeiculoId`       int NULL,
  PRIMARY KEY (`DocumentoId`),
  KEY `IX_Documentos_MotoristaId` (`MotoristaId`),
  KEY `IX_Documentos_SolicitacaoId` (`SolicitacaoId`),
  KEY `IX_Documentos_VeiculoId` (`VeiculoId`),
  CONSTRAINT `FK_Documentos_Motoristas_MotoristaId`
    FOREIGN KEY (`MotoristaId`) REFERENCES `Motoristas` (`MotoristaId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Documentos_Solicitacoes_SolicitacaoId`
    FOREIGN KEY (`SolicitacaoId`) REFERENCES `Solicitacoes` (`SolicitacaoId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Documentos_Veiculos_VeiculoId`
    FOREIGN KEY (`VeiculoId`) REFERENCES `Veiculos` (`VeiculoId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE `HistoricosStatus` (
  `HistoricoStatusId` int NOT NULL AUTO_INCREMENT,
  `StatusAnterior`    varchar(30) NULL,
  `NovoStatus`        varchar(30) NOT NULL,
  `DataAlteracao`     datetime(6) NOT NULL,
  `SolicitacaoId`     int NOT NULL,
  `AtendenteId`       int NULL,
  PRIMARY KEY (`HistoricoStatusId`),
  KEY `IX_HistoricosStatus_AtendenteId` (`AtendenteId`),
  KEY `IX_HistoricosStatus_SolicitacaoId` (`SolicitacaoId`),
  CONSTRAINT `FK_HistoricosStatus_Atendentes_AtendenteId`
    FOREIGN KEY (`AtendenteId`) REFERENCES `Atendentes` (`AtendenteId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_HistoricosStatus_Solicitacoes_SolicitacaoId`
    FOREIGN KEY (`SolicitacaoId`) REFERENCES `Solicitacoes` (`SolicitacaoId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE `LinksAcesso` (
  `LinkAcessoId`    int NOT NULL AUTO_INCREMENT,
  `TokenHash`       varchar(255) NOT NULL,
  `DataCriacao`     datetime(6)  NOT NULL,
  `DataExpiracao`   datetime(6)  NOT NULL,
  `DataInvalidacao` datetime(6)  NULL,
  `DataUtilizacao`  datetime(6)  NULL,
  `SolicitacaoId`   int NOT NULL,
  PRIMARY KEY (`LinkAcessoId`),
  UNIQUE KEY `IX_LinksAcesso_TokenHash` (`TokenHash`),
  KEY `IX_LinksAcesso_SolicitacaoId` (`SolicitacaoId`),
  CONSTRAINT `FK_LinksAcesso_Solicitacoes_SolicitacaoId`
    FOREIGN KEY (`SolicitacaoId`) REFERENCES `Solicitacoes` (`SolicitacaoId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;



CREATE TABLE `RegistrosAuditoria` (
  `RegistroAuditoriaId` int NOT NULL AUTO_INCREMENT,
  `Acao`          varchar(100) NOT NULL,
  `Detalhes`      longtext     NULL,
  `DataRegistro`  datetime(6)  NOT NULL,
  `AtendenteId`   int NULL,
  PRIMARY KEY (`RegistroAuditoriaId`),
  KEY `IX_RegistrosAuditoria_AtendenteId` (`AtendenteId`),
  CONSTRAINT `FK_RegistrosAuditoria_Atendentes_AtendenteId`
    FOREIGN KEY (`AtendenteId`) REFERENCES `Atendentes` (`AtendenteId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

SET FOREIGN_KEY_CHECKS = 1;

