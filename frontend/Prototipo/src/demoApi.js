const STORAGE_KEY = 'fretebot.demo-data';
const statuses = [
  ['NovoContato', 'Novo contato'],
  ['AguardandoPreenchimento', 'Aguardando preenchimento'],
  ['ComPendencias', 'Com pendências'],
  ['ProntoParaAnalise', 'Pronto para análise'],
  ['EmAnalise', 'Em análise'],
  ['Contratado', 'Contratado'],
  ['Encerrado', 'Encerrado'],
];
const defaultFormFields = [
  { campoFormularioId: 1, chave: 'nome', rotulo: 'Nome completo', categoria: 'Campo', tipoCampo: 'text', obrigatorio: true, ativo: true },
  { campoFormularioId: 2, chave: 'cpf', rotulo: 'CPF', categoria: 'Campo', tipoCampo: 'text', obrigatorio: true, ativo: true },
  { campoFormularioId: 3, chave: 'telefone', rotulo: 'Telefone para contato', categoria: 'Campo', tipoCampo: 'tel', obrigatorio: true, ativo: true },
  { campoFormularioId: 4, chave: 'email', rotulo: 'E-mail', categoria: 'Campo', tipoCampo: 'email', obrigatorio: false, ativo: true },
  { campoFormularioId: 5, chave: 'placa', rotulo: 'Placa do veículo', categoria: 'Campo', tipoCampo: 'text', obrigatorio: true, ativo: true },
  { campoFormularioId: 6, chave: 'capacidade', rotulo: 'Capacidade em toneladas', categoria: 'Campo', tipoCampo: 'number', obrigatorio: true, ativo: true },
  { campoFormularioId: 7, chave: 'cnh', rotulo: 'CNH', categoria: 'Documento', tipoCampo: 'file', obrigatorio: true, ativo: true },
  { campoFormularioId: 8, chave: 'crlv', rotulo: 'CRLV', categoria: 'Documento', tipoCampo: 'file', obrigatorio: true, ativo: true },
  { campoFormularioId: 9, chave: 'comprovante_rntrc', rotulo: 'Comprovante RNTRC', categoria: 'Documento', tipoCampo: 'file', obrigatorio: true, ativo: true },
  { campoFormularioId: 10, chave: 'documento_adicional_carga', rotulo: 'Documento adicional da carga', categoria: 'Documento', tipoCampo: 'file', obrigatorio: false, ativo: true },
];

function daysAgo(days) {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return date.toISOString();
}

function createInitialData() {
  const drivers = [
    { motoristaId: 1, nomeCompleto: 'Marcos Oliveira', telefone: '16991234567', email: 'marcos@email.com', placa: 'ABC1D23', capacidadeToneladas: 14, cpfMascarado: '***.456.789-**', dataCadastro: daysAgo(20) },
    { motoristaId: 2, nomeCompleto: 'Ana Paula Santos', telefone: '16987654321', email: 'ana@email.com', placa: 'DEF4G56', capacidadeToneladas: 18, cpfMascarado: '***.321.654-**', dataCadastro: daysAgo(17) },
    { motoristaId: 3, nomeCompleto: 'Roberto Lima', telefone: '16999887766', email: 'roberto@email.com', placa: 'GHI7J89', capacidadeToneladas: 12, cpfMascarado: '***.987.123-**', dataCadastro: daysAgo(11) },
    { motoristaId: 4, nomeCompleto: 'Juliana Costa', telefone: '16993456789', email: 'juliana@email.com', placa: 'KLM2N34', capacidadeToneladas: 24, cpfMascarado: '***.654.321-**', dataCadastro: daysAgo(7) },
    { motoristaId: 5, nomeCompleto: 'Carlos Ferreira', telefone: '16994561234', email: 'carlos@email.com', placa: 'OPQ5R67', capacidadeToneladas: 10, cpfMascarado: '***.123.987-**', dataCadastro: daysAgo(3) },
  ];
  const loads = [
    { cargaId: 1, origem: 'Araraquara, SP', destino: 'Santos, SP', capacidadeMinimaToneladas: 12, status: 'Disponivel', dataCadastro: daysAgo(8) },
    { cargaId: 2, origem: 'Ribeirão Preto, SP', destino: 'Curitiba, PR', capacidadeMinimaToneladas: 14, status: 'Disponivel', dataCadastro: daysAgo(5) },
    { cargaId: 3, origem: 'São Carlos, SP', destino: 'Belo Horizonte, MG', capacidadeMinimaToneladas: 10, status: 'Contratada', dataCadastro: daysAgo(2) },
  ];
  const requests = [
    { solicitacaoId: 1, motoristaId: 1, status: 'NovoContato', prioridade: 'Alta', dataCriacao: daysAgo(1), cargaId: null, atendenteId: null },
    { solicitacaoId: 2, motoristaId: 2, status: 'AguardandoPreenchimento', prioridade: 'Media', dataCriacao: daysAgo(4), cargaId: null, atendenteId: 1 },
    { solicitacaoId: 3, motoristaId: 3, status: 'ComPendencias', prioridade: 'Alta', dataCriacao: daysAgo(7), cargaId: null, atendenteId: 2 },
    { solicitacaoId: 4, motoristaId: 4, status: 'ProntoParaAnalise', prioridade: 'Media', dataCriacao: daysAgo(6), cargaId: 1, atendenteId: 1 },
    { solicitacaoId: 5, motoristaId: 5, status: 'EmAnalise', prioridade: 'Baixa', dataCriacao: daysAgo(2), cargaId: 2, atendenteId: 2 },
    { solicitacaoId: 6, motoristaId: 2, status: 'Contratado', prioridade: 'Media', dataCriacao: daysAgo(15), cargaId: 3, atendenteId: 1 },
  ];
  const documents = [
    { documentoId: 1, solicitacaoId: 3, chave: 'cnh', rotulo: 'CNH', nomeOriginal: 'cnh-roberto.pdf', situacao: 'AguardandoValidacao', obrigatorio: true },
    { documentoId: 2, solicitacaoId: 3, chave: 'documento-veiculo', rotulo: 'Documento do veículo', nomeOriginal: 'veiculo-roberto.pdf', situacao: 'Validado', obrigatorio: true },
    { documentoId: 3, solicitacaoId: 4, chave: 'cnh', rotulo: 'CNH', nomeOriginal: 'cnh-juliana.pdf', situacao: 'Validado', obrigatorio: true },
    { documentoId: 4, solicitacaoId: 5, chave: 'cnh', rotulo: 'CNH', nomeOriginal: 'cnh-carlos.pdf', situacao: 'AguardandoValidacao', obrigatorio: true },
  ];
  return {
    drivers,
    loads,
    requests,
    documents,
    attendants: [
      { atendenteId: 1, nome: 'Fernanda Alves', email: 'fernanda@fretebot.local', perfil: 'Administrador', ativo: true },
      { atendenteId: 2, nome: 'Lucas Martins', email: 'lucas@fretebot.local', perfil: 'Atendente', ativo: true },
    ],
    formFields: defaultFormFields,
    nextDriverId: 6,
    nextRequestId: 7,
    nextLoadId: 4,
    nextDocumentId: 5,
    nextAttendantId: 3,
  };
}

function readData() {
  const stored = localStorage.getItem(STORAGE_KEY);
  if (!stored) {
    const initialData = createInitialData();
    localStorage.setItem(STORAGE_KEY, JSON.stringify(initialData));
    return initialData;
  }
  try {
    const data = JSON.parse(stored);
    if (!data.formFields) data.formFields = defaultFormFields;
    if (!data.nextAttendantId) data.nextAttendantId = Math.max(0, ...data.attendants.map((item) => item.atendenteId)) + 1;
    data.requests = data.requests.map((request) => ({
      ...request,
      prioridade: request.prioridade === 'Normal' ? 'Media' : request.prioridade,
    }));
    data.attendants = data.attendants.map((member) => ({
      ...member,
      email: member.email || (member.atendenteId === 1 ? 'fernanda@fretebot.local' : 'lucas@fretebot.local'),
      ativo: member.ativo ?? true,
    }));
    writeData(data);
    return data;
  } catch (error) {
    console.error('Não foi possível ler os dados da demonstração.', error);
    const initialData = createInitialData();
    localStorage.setItem(STORAGE_KEY, JSON.stringify(initialData));
    return initialData;
  }
}

function writeData(data) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
}

function parseBody(body) {
  if (!body || body instanceof FormData) return {};
  return typeof body === 'string' ? JSON.parse(body) : body;
}

function getDriver(data, id) {
  return data.drivers.find((driver) => driver.motoristaId === id);
}

function findLatestRequest(requests) {
  return [...requests].sort((left, right) =>
    Date.parse(right.dataCriacao) - Date.parse(left.dataCriacao)
    || right.solicitacaoId - left.solicitacaoId
  )[0];
}

function requestsInPeriod(data, queryString) {
  const params = new URLSearchParams(queryString);
  const from = params.get('de') ? Date.parse(params.get('de')) : Number.NEGATIVE_INFINITY;
  const until = params.get('ate') ? Date.parse(`${params.get('ate')}T23:59:59.999`) : Number.POSITIVE_INFINITY;
  const status = params.get('status');
  return data.requests.filter((request) => {
    const created = Date.parse(request.dataCriacao);
    return created >= from && created <= until && (!status || request.status === status);
  });
}

function csvCell(value) {
  let text = value == null ? '' : String(value).replace(/[\r\n]/g, ' ');
  if ('=+-@\t'.includes(text[0])) text = `'${text}`;
  if (text.includes(';') || text.includes('"')) text = `"${text.replace(/"/g, '""')}"`;
  return text;
}

function getRequest(data, id) {
  const request = data.requests.find((item) => item.solicitacaoId === Number(id));
  if (!request) throw new Error('Solicitação não encontrada na demonstração.');
  const driver = getDriver(data, request.motoristaId);
  const load = data.loads.find((item) => item.cargaId === request.cargaId);
  const documents = data.documents.filter((item) => item.solicitacaoId === request.solicitacaoId);
  const activeDocuments = data.formFields.filter((field) => field.categoria === 'Documento' && field.ativo);
  return {
    ...request,
    nomeMotorista: driver?.nomeCompleto,
    telefone: driver?.telefone,
    email: driver?.email,
    cpfMascarado: driver?.cpfMascarado,
    placa: driver?.placa,
    capacidadeToneladas: driver?.capacidadeToneladas,
    cargaDescricao: load ? `${load.origem} → ${load.destino}` : null,
    statusRotulo: statuses.find(([status]) => status === request.status)?.[1],
    compatibilidade: request.cargaId ? 'Compativel' : 'NaoAplicavel',
    descricaoCompatibilidade: request.cargaId ? 'Veículo compatível com a carga' : 'Não aplicável',
    camposPendentes: request.status === 'ComPendencias' ? ['E-mail'] : [],
    documentos: activeDocuments.map((field) => {
      const doc = documents.find((item) => item.chave === field.chave);
      return doc ? { ...doc, rotulo: field.rotulo, obrigatorio: field.obrigatorio } : {
        chave: field.chave,
        rotulo: field.rotulo,
        obrigatorio: field.obrigatorio,
        situacao: 'Ausente',
      };
    }),
  };
}

function kanban(data) {
  return statuses.map(([status, rotulo]) => ({
    status,
    rotulo,
    solicitacoes: data.requests
      .filter((request) => request.status === status)
      .map((request) => {
        const detail = getRequest(data, request.solicitacaoId);
        return {
          solicitacaoId: request.solicitacaoId,
          nomeMotorista: detail.nomeMotorista,
          telefone: detail.telefone,
          prioridade: request.prioridade,
          dataCriacao: request.dataCriacao,
          cargaDescricao: detail.cargaDescricao,
          pendenciasDocumentais: detail.documentos.filter((doc) => doc.situacao !== 'Validado').length,
          placa: detail.placa,
          compatibilidade: detail.compatibilidade,
        };
      }),
  }));
}

function getForm(data, token) {
  const requestId = Number(token.replace('demo-', ''));
  const request = data.requests.find((item) => item.solicitacaoId === requestId);
  if (!request) throw new Error('Solicitação não encontrada na demonstração.');
  if (request.status === 'NovoContato') {
    request.status = 'AguardandoPreenchimento';
    writeData(data);
  }
  const detail = getRequest(data, requestId);
  const driver = getDriver(data, request.motoristaId);
  return {
    solicitacaoId: requestId,
    campos: data.formFields.filter((field) => field.categoria === 'Campo' && field.ativo),
    dados: {
      nomeCompleto: driver.nomeCompleto || '',
      telefone: driver.telefone || '',
      email: driver.email || '',
      cpfMascarado: driver.cpfMascarado || '',
      veiculos: driver.placa ? [{ veiculoId: driver.motoristaId, placa: driver.placa, capacidadeToneladas: driver.capacidadeToneladas }] : [],
      veiculoSelecionadoId: driver.placa ? driver.motoristaId : null,
    },
    documentos: detail.documentos.map((document) => ({ ...document, estado: document.situacao })),
  };
}

export function demoApi(path, options = {}) {
  const data = readData();
  const [route, queryString = ''] = path.split('?');
  const parts = route.split('/').filter(Boolean);
  const method = (options.method || 'GET').toUpperCase();
  const body = parseBody(options.body);
  let result;

  if (route === '/Autenticacao/login' && method === 'POST') {
    const credentials = body.email === 'demo@fretebot.local' && body.senha === 'demo123';
    if (!credentials) throw new Error('No modo demonstração, use demo@fretebot.local e a senha demo123.');
    result = { token: 'demo-token', atendenteId: 1, nome: 'Fernanda Alves', perfil: 'Administrador', email: body.email };
  } else if (route === '/Solicitacoes/kanban') {
    result = kanban(data);
  } else if (parts[0] === 'Solicitacoes' && parts[1] && !parts[2] && method === 'GET') {
    result = getRequest(data, parts[1]);
  } else if (parts[0] === 'Solicitacoes' && parts[1] === 'motorista' && parts[2] && method === 'GET') {
    result = data.requests
      .filter((request) => request.motoristaId === Number(parts[2]))
      .sort((left, right) => Date.parse(right.dataCriacao) - Date.parse(left.dataCriacao) || right.solicitacaoId - left.solicitacaoId)
      .map((request) => getRequest(data, request.solicitacaoId));
  } else if (parts[0] === 'Solicitacoes' && parts[1] && parts[2] && method === 'PATCH') {
    const request = data.requests.find((item) => item.solicitacaoId === Number(parts[1]));
    if (!request) throw new Error('Solicitação não encontrada na demonstração.');
    if (parts[2] === 'status') request.status = body.novoStatus;
    if (parts[2] === 'atendente') request.atendenteId = body.atendenteId;
    if (parts[2] === 'carga') request.cargaId = body.cargaId;
    writeData(data);
    result = null;
  } else if (route === '/Atendentes' && method === 'GET') {
    result = data.attendants;
  } else if (route === '/Atendentes' && method === 'POST') {
    const member = {
      atendenteId: data.nextAttendantId++,
      nome: body.nome,
      email: body.email,
      perfil: 'Atendente',
      ativo: true,
    };
    data.attendants.push(member);
    writeData(data);
    result = member;
  } else if (parts[0] === 'Atendentes' && parts[1] && parts[2] === undefined && method === 'PUT') {
    const member = data.attendants.find((item) => item.atendenteId === Number(parts[1]));
    if (!member) throw new Error('Usuário não encontrado na demonstração.');
    Object.assign(member, { nome: body.nome, email: body.email });
    writeData(data);
    result = member;
  } else if (parts[0] === 'Atendentes' && parts[1] && ['inativar', 'reativar'].includes(parts[2]) && method === 'PATCH') {
    const member = data.attendants.find((item) => item.atendenteId === Number(parts[1]));
    if (!member) throw new Error('Usuário não encontrado na demonstração.');
    member.ativo = parts[2] === 'reativar';
    writeData(data);
    result = null;
  } else if (route === '/Atendentes/ativos') {
    result = data.attendants.filter((member) => member.ativo);
  } else if (route === '/CamposFormulario' && method === 'GET') {
    result = data.formFields;
  } else if (parts[0] === 'CamposFormulario' && parts[1] && parts[2] === undefined && method === 'PUT') {
    const field = data.formFields.find((item) => item.campoFormularioId === Number(parts[1]));
    if (!field) throw new Error('Campo do formulário não encontrado na demonstração.');
    if (body.obrigatorio && !field.ativo) throw new Error('Um item inativo não pode ser obrigatório. Ative-o primeiro.');
    Object.assign(field, { rotulo: body.rotulo.trim(), obrigatorio: body.obrigatorio });
    writeData(data);
    result = field;
  } else if (parts[0] === 'CamposFormulario' && parts[1] && ['ativar', 'desativar'].includes(parts[2]) && method === 'PATCH') {
    const field = data.formFields.find((item) => item.campoFormularioId === Number(parts[1]));
    if (!field) throw new Error('Campo do formulário não encontrado na demonstração.');
    if (field.categoria !== 'Documento') throw new Error('Somente documentos podem ser ativados ou desativados.');
    field.ativo = parts[2] === 'ativar';
    if (!field.ativo) field.obrigatorio = false;
    writeData(data);
    result = field;
  } else if (route === '/Atendimentos/iniciar' && method === 'POST') {
    const digits = String(body.telefone || '').replace(/\D/g, '');
    if (digits.length < 10 || digits.length > 15) throw new Error('Telefone inválido. Informe DDD e número.');
    const shouldAddDdi = digits.length === 10 || (digits.length === 11 && (!digits.startsWith('1') || digits[2] === '9'));
    const canonicalPhone = shouldAddDdi
      ? `55${digits}`
      : digits;
    let driver = data.drivers.find((item) => {
      const savedDigits = String(item.telefone || '').replace(/\D/g, '');
      return savedDigits === digits || savedDigits === canonicalPhone;
    });
    const motoristaCriado = !driver;
    if (!driver) {
      driver = { motoristaId: data.nextDriverId++, nomeCompleto: '', telefone: canonicalPhone, email: '', dataCadastro: new Date().toISOString() };
      data.drivers.push(driver);
    }
    let request = findLatestRequest(data.requests.filter((item) =>
      item.motoristaId === driver.motoristaId && !['Contratado', 'Encerrado'].includes(item.status)
    ));
    const solicitacaoCriada = !request;
    if (!request) {
      request = { solicitacaoId: data.nextRequestId++, motoristaId: driver.motoristaId, status: 'NovoContato', prioridade: 'Baixa', dataCriacao: new Date().toISOString(), cargaId: null, atendenteId: 1 };
      data.requests.push(request);
    }
    writeData(data);
    const urlAcesso = `${window.location.origin}/acesso/demo-${request.solicitacaoId}`;
    result = {
      motoristaId: driver.motoristaId,
      nomeMotorista: driver.nomeCompleto,
      motoristaCriado,
      solicitacaoId: request.solicitacaoId,
      solicitacaoCriada,
      statusSolicitacao: request.status,
      link: {
        urlAcesso,
        urlWhatsApp: `https://wa.me/${String(driver.telefone).replace(/\D/g, '')}?text=${encodeURIComponent(urlAcesso)}`,
        mensagemSugerida: 'Link de demonstração local. Os dados ficam salvos somente neste navegador.',
      },
    };
  } else if (route === '/Motoristas' && method === 'GET') {
    const term = new URLSearchParams(queryString).get('busca')?.toLowerCase() || '';
    result = data.drivers
      .map((driver) => {
        const latest = findLatestRequest(data.requests.filter((item) => item.motoristaId === driver.motoristaId));
        return {
          ...driver,
          ultimaSolicitacaoId: latest?.solicitacaoId,
          statusUltimaSolicitacao: latest?.status,
          compatibilidadeUltimaSolicitacao: latest ? getRequest(data, latest.solicitacaoId).compatibilidade : null,
        };
      })
      .filter((driver) => !term || [driver.nomeCompleto, driver.telefone, driver.placa].some((value) => value?.toLowerCase().includes(term)));
  } else if (route === '/Motoristas' && method === 'POST') {
    data.drivers.push({ ...body, motoristaId: data.nextDriverId++, dataCadastro: new Date().toISOString() });
    writeData(data);
    result = null;
  } else if (parts[0] === 'Motoristas' && parts[1] && method === 'GET') {
    const driver = data.drivers.find((item) => item.motoristaId === Number(parts[1]));
    if (!driver) throw new Error('Motorista não encontrado na demonstração.');
    const latestRequest = findLatestRequest(data.requests.filter((item) => item.motoristaId === driver.motoristaId));
    result = {
      ...driver,
      ultimaSolicitacaoId: latestRequest?.solicitacaoId,
      statusUltimaSolicitacao: latestRequest?.status,
      compatibilidadeUltimaSolicitacao: latestRequest ? getRequest(data, latestRequest.solicitacaoId).compatibilidade : null,
    };
  } else if (route === '/Cargas' && method === 'GET') {
    result = data.loads;
  } else if (route === '/Cargas' && method === 'POST') {
    data.loads.push({ ...body, cargaId: data.nextLoadId++, status: 'Disponivel', dataCadastro: new Date().toISOString() });
    writeData(data);
    result = null;
  } else if (parts[0] === 'Cargas' && parts[1] && parts[2] === undefined && method === 'PUT') {
    const load = data.loads.find((item) => item.cargaId === Number(parts[1]));
    if (!load) throw new Error('Carga não encontrada na demonstração.');
    Object.assign(load, body);
    writeData(data);
    result = load;
  } else if (parts[0] === 'Cargas' && parts[1] && parts[2] === 'status' && method === 'PATCH') {
    const load = data.loads.find((item) => item.cargaId === Number(parts[1]));
    if (!load) throw new Error('Carga não encontrada na demonstração.');
    load.status = body.novoStatus;
    writeData(data);
    result = null;
  } else if (parts[0] === 'Documentos' && parts[1] && parts[2] === 'validacao' && method === 'PATCH') {
    const document = data.documents.find((item) => item.documentoId === Number(parts[1]));
    if (!document) throw new Error('Documento não encontrado na demonstração.');
    document.situacao = body.statusValidacao;
    writeData(data);
    result = null;
  } else if (route.startsWith('/Relatorios/indicadores')) {
    const requests = requestsInPeriod(data, queryString);
    const total = requests.length;
    const hired = requests.filter((item) => item.status === 'Contratado').length;
    result = {
      totalSolicitacoes: total,
      cadastrosCompletos: requests.filter((item) => ['ProntoParaAnalise', 'EmAnalise', 'Contratado'].includes(item.status)).length,
      contratadas: hired,
      taxaConversaoPercentual: total ? Math.round((hired / total) * 1000) / 10 : 0,
      porStatus: Object.fromEntries(statuses.map(([status]) => [status, requests.filter((item) => item.status === status).length])),
      tempoMedioAteProntaParaAnaliseMinutos: 85,
      tempoMedioTotalAtendimentoMinutos: 240,
      percentualCadastrosCompletos: total ? Math.round((requests.filter((item) => ['ProntoParaAnalise', 'EmAnalise', 'Contratado'].includes(item.status)).length / total) * 1000) / 10 : 0,
    };
  } else if (route.startsWith('/Relatorios/solicitacoes/exportar')) {
    const requests = requestsInPeriod(data, queryString);
    const header = ['Solicitação', 'Data de criação', 'Status', 'Prioridade', 'Motorista', 'CPF', 'Telefone', 'Placa', 'Capacidade (t)', 'Carga', 'Atendente'];
    const rows = requests.map((request) => {
      const driver = getDriver(data, request.motoristaId);
      const load = data.loads.find((item) => item.cargaId === request.cargaId);
      const attendant = data.attendants.find((item) => item.atendenteId === request.atendenteId);
      const date = new Date(request.dataCriacao).toISOString().slice(0, 16).replace('T', ' ');
      return [
        request.solicitacaoId,
        date,
        statuses.find(([status]) => status === request.status)?.[1] || request.status,
        request.prioridade,
        driver?.nomeCompleto || '',
        driver?.cpfMascarado || '',
        driver?.telefone || '',
        driver?.placa || '',
        driver?.capacidadeToneladas ?? '',
        load ? `${load.origem} → ${load.destino}` : '',
        attendant?.nome || '',
      ];
    });
    result = {
      content: `\uFEFF${[header, ...rows].map((row) => row.map(csvCell).join(';')).join('\n')}\n`,
      contentType: 'text/csv;charset=utf-8',
      filename: 'solicitacoes-demo.csv',
    };
  } else if (parts[0] === 'Documentos' && parts[1] && parts[2] === 'arquivo') {
    const document = data.documents.find((item) => item.documentoId === Number(parts[1]));
    if (!document) throw new Error('Documento não encontrado na demonstração.');
    result = {
      content: `Arquivo ilustrativo de demonstração. O arquivo original (${document.nomeOriginal}) não está armazenado no modo local.`,
      contentType: 'text/plain;charset=utf-8',
      filename: `documento-${document.documentoId}-demo.txt`,
    };
  } else if (parts[0] === 'Formulario') {
    const accessToken = options.headers?.['X-Token-Acesso'] || options.headers?.get?.('X-Token-Acesso') || '';
    if (parts[1] === 'dados' && method === 'PUT') {
      const form = getForm(data, accessToken);
      const request = data.requests.find((item) => item.solicitacaoId === form.solicitacaoId);
      const driver = getDriver(data, request.motoristaId);
      Object.assign(driver, body, { cpfMascarado: body.cpf ? '***.000.000-**' : driver.cpfMascarado });
      writeData(data);
      result = getForm(data, accessToken);
    } else if (parts[1] === 'documentos' && method === 'POST') {
      const form = getForm(data, accessToken);
      const file = options.body.get('Arquivo');
      data.documents.push({
        documentoId: data.nextDocumentId++,
        solicitacaoId: form.solicitacaoId,
        chave: options.body.get('Tipo'),
        rotulo: data.formFields.find((field) => field.chave === options.body.get('Tipo'))?.rotulo || options.body.get('Tipo'),
        nomeOriginal: file?.name || 'arquivo-demo',
        situacao: 'AguardandoValidacao',
        obrigatorio: data.formFields.find((field) => field.chave === options.body.get('Tipo'))?.obrigatorio ?? true,
      });
      writeData(data);
      result = null;
    } else if (parts[1] === 'enviar' && method === 'POST') {
      const form = getForm(data, accessToken);
      const request = data.requests.find((item) => item.solicitacaoId === form.solicitacaoId);
      const driver = getDriver(data, request.motoristaId);
      const valuesByKey = {
        nome: driver.nomeCompleto,
        cpf: driver.cpfMascarado,
        telefone: driver.telefone,
        email: driver.email,
        placa: driver.placa,
        capacidade: driver.capacidadeToneladas,
      };
      const camposPendentes = form.campos
        .filter((field) => field.obrigatorio && !valuesByKey[field.chave])
        .map((field) => field.rotulo);
      const documentosPendentes = form.documentos
        .filter((field) => field.obrigatorio && !data.documents.some((doc) => doc.solicitacaoId === form.solicitacaoId && doc.chave === field.chave))
        .map((field) => field.rotulo);
      if (!camposPendentes.length && !documentosPendentes.length) {
        request.status = 'ProntoParaAnalise';
        writeData(data);
        result = { mensagem: 'Cadastro enviado na demonstração.', camposPendentes, documentosPendentes };
      } else {
        result = { mensagem: 'O cadastro ainda possui informações pendentes.', camposPendentes, documentosPendentes };
      }
    } else {
      result = getForm(data, accessToken);
    }
  } else {
    throw new Error(`O endpoint ${route} não está disponível na demonstração local.`);
  }

  return Promise.resolve(result);
}
