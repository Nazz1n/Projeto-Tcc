# Gera backend\CodigoFonte\API\appsettings.Development.json a partir do modelo,
# criando uma chave JWT aleatória. Execute na pasta backend:  .\configurar.ps1
$ErrorActionPreference = 'Stop'
$pasta   = Join-Path $PSScriptRoot 'CodigoFonte\API'
$modelo  = Join-Path $pasta 'appsettings.Development.example.json'
$destino = Join-Path $pasta 'appsettings.Development.json'

if (Test-Path $destino) {
    Write-Host "Já existe $destino. Apague-o para gerar de novo."; exit 1
}

function Ler-Senha($texto) {
    $s = Read-Host $texto -AsSecureString
    [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
}

$senhaBanco = Ler-Senha 'Senha do usuario root do MySQL'
$nomeAdmin  = Read-Host 'Nome do administrador inicial'
$emailAdmin = Read-Host 'E-mail do administrador inicial'
$senhaAdmin = Ler-Senha 'Senha do administrador inicial (use uma senha longa)'

$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$chave = [Convert]::ToBase64String($bytes)

$bytesCpf = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytesCpf)
$chaveCpf = [Convert]::ToBase64String($bytesCpf)

$json = Get-Content $modelo -Raw -Encoding UTF8
$json = $json.Replace('TROQUE-PELA-SENHA-DO-MYSQL', $senhaBanco.Replace('\','\\').Replace('"','\"'))
$json = $json.Replace('"Key": "TROQUE-POR-CHAVE-BASE64-DE-32-BYTES"', '"Key": "' + $chave + '"')
$json = $json.Replace('"ChaveHashCpf": "TROQUE-POR-CHAVE-BASE64-DE-32-BYTES"', '"ChaveHashCpf": "' + $chaveCpf + '"')
$json = $json.Replace('"Administrador",', '"' + $nomeAdmin.Replace('"','\"') + '",')
$json = $json.Replace('admin@exemplo.com', $emailAdmin)
$json = $json.Replace('TROQUE-POR-UMA-SENHA-LONGA', $senhaAdmin.Replace('\','\\').Replace('"','\"'))

[IO.File]::WriteAllText($destino, $json, (New-Object Text.UTF8Encoding($false)))
Write-Host "Criado: $destino (este arquivo é ignorado pelo Git; não o compartilhe)."
