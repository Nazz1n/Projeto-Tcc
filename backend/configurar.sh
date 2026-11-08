#!/usr/bin/env bash
# Gera CodigoFonte/API/appsettings.Development.json a partir do modelo,
# criando uma chave JWT aleatória. Execute na pasta backend:  ./configurar.sh
set -euo pipefail
cd "$(dirname "$0")/CodigoFonte/API"
[ -e appsettings.Development.json ] && { echo "appsettings.Development.json já existe. Apague-o para gerar de novo."; exit 1; }

read -r -s -p "Senha do usuário root do MySQL: " SENHA_BANCO; echo
read -r -p "Nome do administrador inicial: " NOME_ADMIN
read -r -p "E-mail do administrador inicial: " EMAIL_ADMIN
read -r -s -p "Senha do administrador inicial (use uma senha longa): " SENHA_ADMIN; echo

SENHA_BANCO="$SENHA_BANCO" NOME_ADMIN="$NOME_ADMIN" EMAIL_ADMIN="$EMAIL_ADMIN" SENHA_ADMIN="$SENHA_ADMIN" \
python3 - <<'PY'
import json, os, base64
c = json.load(open('appsettings.Development.example.json', encoding='utf-8'))
c['ConnectionStrings']['DefaultConnection'] = c['ConnectionStrings']['DefaultConnection'].replace('TROQUE-PELA-SENHA-DO-MYSQL', os.environ['SENHA_BANCO'])
c['Jwt']['Key'] = base64.b64encode(os.urandom(32)).decode()
c['Seguranca']['ChaveHashCpf'] = base64.b64encode(os.urandom(32)).decode()
c['Administrador'] = {'Nome': os.environ['NOME_ADMIN'], 'Email': os.environ['EMAIL_ADMIN'], 'Senha': os.environ['SENHA_ADMIN']}
json.dump(c, open('appsettings.Development.json', 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
PY
echo "Criado: CodigoFonte/API/appsettings.Development.json (ignorado pelo Git; não o compartilhe)."
