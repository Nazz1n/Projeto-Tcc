import { useEffect, useMemo, useState } from 'react';
import { Link, Navigate, Route, Routes, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { api, clearToken, download, getToken, saveToken } from './api';

const STATUS_LABELS = {
  NovoContato: 'Novo contato',
  AguardandoPreenchimento: 'Aguardando preenchimento',
  ComPendencias: 'Com pendências',
  ProntoParaAnalise: 'Pronto para análise',
  EmAnalise: 'Em análise',
  Contratado: 'Contratado',
  Encerrado: 'Encerrado',
};

const STATUS_COLORS = ['blue', 'cyan', 'amber', 'green', 'violet', 'emerald', 'slate'];
const STATUS_OPTIONS = [
  ['EmAnalise', 'Em análise'],
  ['Contratado', 'Contratado'],
  ['Encerrado', 'Encerrado'],
];

function readUser() {
  try {
    return JSON.parse(sessionStorage.getItem('fretebot.user') || 'null');
  } catch {
    return null;
  }
}

function App() {
  const [token, setToken] = useState(getToken());
  const [user, setUser] = useState(readUser);

  const signIn = (result) => {
    saveToken(result.token);
    sessionStorage.setItem('fretebot.user', JSON.stringify(result));
    setToken(result.token);
    setUser(result);
  };

  const signOut = () => {
    clearToken();
    sessionStorage.removeItem('fretebot.user');
    setToken(null);
    setUser(null);
  };

  return (
    <Routes>
      <Route path="/login" element={token ? <Navigate to="/admin" replace /> : <LoginPage onLogin={signIn} />} />
      <Route path="/acesso" element={<PublicFormPage />} />
      <Route path="/acesso/:token" element={<PublicFormPage />} />
      <Route path="/form" element={<PublicFormPage />} />
      <Route path="/form/:token" element={<PublicFormPage />} />
      <Route path="/admin/*" element={token ? <AdminApp user={user} onSignOut={signOut} /> : <Navigate to="/login" replace />} />
      <Route path="/" element={<Navigate to={token ? '/admin' : '/login'} replace />} />
      <Route path="*" element={<Navigate to={token ? '/admin' : '/login'} replace />} />
    </Routes>
  );
}

function LoginPage({ onLogin }) {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      const result = await api('/Autenticacao/login', {
        method: 'POST',
        body: JSON.stringify({ email, senha: password }),
      });
      onLogin(result);
      navigate('/admin', { replace: true });
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-brand">
        <div className="brand-lockup"><span className="brand-mark">F</span><span>Fretebot</span></div>
        <div>
          <span className="eyebrow">Plataforma de captação</span>
          <h1>Do primeiro contato à contratação, em um só lugar.</h1>
          <p>Organize motoristas, acompanhe cadastros e conecte sua equipe à operação de fretes.</p>
        </div>
        <div className="login-footnote"><span className="online-dot" /> Painel de gestão de fretes</div>
      </section>
      <section className="login-panel">
        <div className="login-panel-inner">
          <span className="eyebrow">Acesso da equipe</span>
          <h2>Bem-vindo de volta</h2>
          <p className="muted">Entre com suas credenciais de atendente ou administrador.</p>
          <form onSubmit={submit} className="stack-form">
            <label className="field">
              <span>E-mail</span>
              <input autoComplete="username" type="email" required value={email} onChange={(event) => setEmail(event.target.value)} placeholder="voce@empresa.com" />
            </label>
            <label className="field">
              <span>Senha</span>
              <input autoComplete="current-password" type="password" required value={password} onChange={(event) => setPassword(event.target.value)} placeholder="Sua senha" />
            </label>
            {error && <Notice kind="error">{error}</Notice>}
            <button className="button primary wide" type="submit" disabled={busy}>{busy ? 'Validando acesso…' : 'Entrar no painel'} <span aria-hidden="true">→</span></button>
          </form>
          <p className="login-help">O acesso é protegido e expira conforme a política da sua sessão.</p>
        </div>
      </section>
    </main>
  );
}

function AdminApp({ user, onSignOut }) {
  const location = useLocation();
  const isAdmin = (user?.perfil || '').toLowerCase().includes('admin');
  const title = useMemo(() => {
    if (location.pathname.startsWith('/admin/motoristas')) return 'Motoristas';
    if (location.pathname.startsWith('/admin/cargas')) return 'Cargas';
    if (location.pathname.startsWith('/admin/relatorios')) return 'Relatórios';
    return 'Central de atendimento';
  }, [location.pathname]);

  return (
    <div className="admin-shell">
      <aside className="sidebar">
        <Link className="brand-lockup sidebar-brand" to="/admin"><span className="brand-mark">F</span><span>Fretebot</span></Link>
        <div className="sidebar-caption">OPERAÇÃO</div>
        <nav className="side-nav">
          <NavLink to="/admin" exact icon="◫">Central de atendimento</NavLink>
          <NavLink to="/admin/motoristas" icon="♙">Motoristas</NavLink>
          <NavLink to="/admin/cargas" icon="▰">Cargas</NavLink>
          {isAdmin && <NavLink to="/admin/relatorios" icon="▥">Relatórios</NavLink>}
        </nav>
        <div className="sidebar-bottom">
          <div className="sidebar-user">
            <span className="avatar">{initials(user?.nome)}</span>
            <span className="user-meta"><strong>{user?.nome || 'Atendente'}</strong><small>{user?.perfil || 'Equipe'}</small></span>
          </div>
          <button className="signout-button" onClick={onSignOut} title="Sair" aria-label="Sair">↪</button>
        </div>
      </aside>
      <div className="workspace">
        <header className="topbar">
          <div><div className="breadcrumb">Fretebot <span>/</span> {title}</div><h1>{title}</h1></div>
          <div className="topbar-right">
            <span className="connection-status"><i /> Sessão ativa</span>
            <span className="avatar top-avatar" title={user?.nome}>{initials(user?.nome)}</span>
          </div>
        </header>
        <main className="page-content">
          <Routes>
            <Route index element={<CentralPage user={user} />} />
            <Route path="motoristas" element={<MotoristasPage />} />
            <Route path="cargas" element={<CargasPage isAdmin={isAdmin} />} />
            {isAdmin && <Route path="relatorios" element={<ReportsPage />} />}
            <Route path="*" element={<Navigate to="/admin" replace />} />
          </Routes>
        </main>
      </div>
    </div>
  );
}

function NavLink({ to, exact, icon, children }) {
  const location = useLocation();
  const active = exact ? location.pathname === to : location.pathname.startsWith(to);
  return <Link className={`side-link ${active ? 'active' : ''}`} to={to}><span className="nav-icon">{icon}</span>{children}</Link>;
}

function CentralPage({ user }) {
  const [columns, setColumns] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showNew, setShowNew] = useState(false);
  const [selected, setSelected] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [realtimeState, setRealtimeState] = useState('connecting');
  const [realtimeError, setRealtimeError] = useState('');

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    api('/Solicitacoes/kanban')
      .then((result) => { if (active) setColumns(result); })
      .catch((requestError) => { if (active) setError(requestError.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [refresh]);

  useEffect(() => {
    let active = true;
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/painel', { accessTokenFactory: () => getToken() || '' })
      .withAutomaticReconnect()
      .build();
    const updateBoard = () => setRefresh((value) => value + 1);

    connection.on('SolicitacaoAtualizada', updateBoard);
    connection.on('SolicitacaoProntaParaAnalise', updateBoard);
    connection.onreconnecting(() => {
      if (active) setRealtimeState('connecting');
    });
    connection.onreconnected(() => {
      if (!active) return;
      setRealtimeState('connected');
      setRealtimeError('');
      updateBoard();
    });
    connection.onclose((connectionError) => {
      if (!active) return;
      setRealtimeState('disconnected');
      if (connectionError) setRealtimeError('Atualizações em tempo real indisponíveis. Use “Atualizar” para recarregar o funil.');
    });

    connection.start()
      .then(() => {
        if (active) {
          setRealtimeState('connected');
          setRealtimeError('');
        }
      })
      .catch(() => {
        if (active) {
          setRealtimeState('disconnected');
          setRealtimeError('Não foi possível conectar às atualizações em tempo real. O painel ainda pode ser atualizado manualmente.');
        }
      });

    return () => {
      active = false;
      connection.off('SolicitacaoAtualizada', updateBoard);
      connection.off('SolicitacaoProntaParaAnalise', updateBoard);
      connection.stop();
    };
  }, []);

  const totals = columns.reduce((total, column) => total + (column.solicitacoes?.length || 0), 0);
  const refreshBoard = () => setRefresh((value) => value + 1);

  return (
    <>
      <section className="page-intro">
        <div><h2>Funil de captação</h2><p>Acompanhe cada solicitação e avance o atendimento conforme a operação.</p></div>
        <button className="button primary" onClick={() => setShowNew(true)}><span>＋</span> Iniciar atendimento</button>
      </section>
      <div className="board-summary">
        <span><strong>{totals}</strong> solicitações no funil</span>
        <span className="refresh-stamp">{realtimeState === 'connected' ? 'Atualizações em tempo real ativas' : 'Atualizações automáticas indisponíveis'}</span>
        <button className="button subtle small" onClick={refreshBoard} disabled={loading}>↻ Atualizar</button>
      </div>
      {error && <Notice kind="error">{error}</Notice>}
      {realtimeError && <Notice kind="info">{realtimeError}</Notice>}
      {loading ? <Loading label="Carregando solicitações…" /> : (
        <div className="kanban-board">
          {columns.map((column, index) => (
            <section className={`kanban-column ${STATUS_COLORS[index % STATUS_COLORS.length]}`} key={column.status}>
              <header className="column-heading">
                <span className="column-indicator" />
                <h3>{column.rotulo || STATUS_LABELS[column.status] || column.status}</h3>
                <span className="count-badge">{column.solicitacoes?.length || 0}</span>
              </header>
              <div className="column-cards">
                {column.solicitacoes?.map((item) => <RequestCard key={item.solicitacaoId} item={item} onClick={() => setSelected(item)} />)}
                {!column.solicitacoes?.length && <div className="empty-column">Nenhuma solicitação nesta etapa</div>}
              </div>
            </section>
          ))}
          {!columns.length && <EmptyState title="Funil sem etapas" detail="A API não retornou colunas para o painel." />}
        </div>
      )}
      {showNew && <StartAtendimentoModal onClose={() => setShowNew(false)} onStarted={() => { setShowNew(false); refreshBoard(); }} />}
      {selected && <RequestDetails id={selected.solicitacaoId} currentUserId={user?.atendenteId} onClose={() => { setSelected(null); refreshBoard(); }} onUpdated={() => { setSelected(null); refreshBoard(); }} />}
    </>
  );
}

function RequestCard({ item, onClick }) {
  return (
    <button className="request-card" onClick={onClick}>
      <div className="request-card-top"><span className={`priority ${priorityClass(item.prioridade)}`}>{item.prioridade || 'Sem prioridade'}</span><span className="request-date">{formatDate(item.dataCriacao)}</span></div>
      <strong className="request-name">{item.nomeMotorista || 'Motorista sem nome'}</strong>
      <span className="request-phone">{formatPhone(item.telefone)}</span>
      <div className="request-card-bottom">
        <span>{item.cargaDescricao || 'Sem carga vinculada'}</span>
        {item.pendenciasDocumentais > 0 && <span className="pending-count">{item.pendenciasDocumentais} pend.</span>}
      </div>
      {(item.placa || item.compatibilidade) && <div className="request-tags">{item.placa && <span>{item.placa}</span>}{item.compatibilidade && item.compatibilidade !== 'NaoAplicavel' && <span>{compatibilityLabel(item.compatibilidade)}</span>}</div>}
    </button>
  );
}

function StartAtendimentoModal({ onClose, onStarted }) {
  const [phone, setPhone] = useState('');
  const [result, setResult] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      setResult(await api('/Atendimentos/iniciar', { method: 'POST', body: JSON.stringify({ telefone: phone }) }));
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(result.link.urlAcesso);
      setCopied(true);
    } catch {
      setError('Não foi possível copiar o link. Selecione e copie o endereço manualmente.');
    }
  }

  return (
    <Modal title={result ? 'Atendimento iniciado' : 'Iniciar atendimento'} onClose={onClose}>
      {!result ? (
        <form className="stack-form" onSubmit={submit}>
          <p className="muted">Informe o telefone do motorista. A API vai localizar ou criar o contato, abrir uma solicitação e gerar um link de cadastro.</p>
          <label className="field"><span>Telefone com DDD</span><input autoFocus type="tel" required value={phone} onChange={(event) => setPhone(event.target.value)} placeholder="(16) 99999-9999" /></label>
          {error && <Notice kind="error">{error}</Notice>}
          <div className="modal-actions"><button type="button" className="button subtle" onClick={onClose}>Cancelar</button><button className="button primary" type="submit" disabled={busy}>{busy ? 'Criando…' : 'Gerar link e iniciar'}</button></div>
        </form>
      ) : (
        <div className="stack-form">
          <Notice kind="success">Solicitação {result.solicitacaoCriada ? 'criada' : 'reutilizada'} com sucesso para {result.nomeMotorista || phone}.</Notice>
          <label className="field"><span>Link único do formulário</span><div className="copy-field"><input readOnly value={result.link.urlAcesso} /><button type="button" className="button subtle small" onClick={copyLink}>{copied ? 'Copiado' : 'Copiar'}</button></div></label>
          <p className="muted small-text">{result.link.mensagemSugerida}</p>
          <div className="modal-actions">
            <button className="button subtle" onClick={onStarted}>Fechar</button>
            {result.link.urlWhatsApp && <a className="button primary" href={result.link.urlWhatsApp} target="_blank" rel="noreferrer">Enviar pelo WhatsApp ↗</a>}
          </div>
        </div>
      )}
    </Modal>
  );
}

function RequestDetails({ id, currentUserId, onClose, onUpdated }) {
  const [detail, setDetail] = useState(null);
  const [attendants, setAttendants] = useState([]);
  const [loads, setLoads] = useState([]);
  const [selectedLoadId, setSelectedLoadId] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    setError('');
    api(`/Solicitacoes/${id}`)
      .then((result) => {
        setDetail(result);
        setSelectedLoadId(result.cargaId?.toString() || '');
      })
      .catch((requestError) => setError(requestError.message))
      .finally(() => setLoading(false));
    api('/Atendentes/ativos')
      .then(setAttendants)
      .catch((requestError) => setError(requestError.message));
    api('/Cargas')
      .then(setLoads)
      .catch((requestError) => setError(requestError.message));
  }, [id]);

  async function changeStatus(status) {
    setBusy(true);
    setError('');
    try {
      await api(`/Solicitacoes/${id}/status`, { method: 'PATCH', body: JSON.stringify({ novoStatus: status }) });
      onUpdated();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }

  async function validateDocument(documentId, status) {
    setBusy(true);
    setError('');
    try {
      await api(`/Documentos/${documentId}/validacao`, { method: 'PATCH', body: JSON.stringify({ statusValidacao: status }) });
      setDetail(await api(`/Solicitacoes/${id}`));
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }

  async function assumeAttendance() {
    if (!currentUserId) return;
    setBusy(true);
    setError('');
    try {
      await api(`/Solicitacoes/${id}/atendente`, {
        method: 'PATCH',
        body: JSON.stringify({ atendenteId: currentUserId }),
      });
      onUpdated();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }

  async function saveLoad() {
    setBusy(true);
    setError('');
    try {
      await api(`/Solicitacoes/${id}/carga`, {
        method: 'PATCH',
        body: JSON.stringify({ cargaId: selectedLoadId ? Number(selectedLoadId) : null }),
      });
      onUpdated();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }

  const assignedAttendant = attendants.find((attendant) => attendant.atendenteId === detail?.atendenteId);
  const availableLoads = loads.filter((load) => load.status === 'Disponivel' || load.cargaId === detail?.cargaId);

  return (
    <Modal title="Detalhes da solicitação" onClose={onClose} wide>
      {loading ? <Loading label="Carregando cadastro…" /> : detail && (
        <>
          <div className="detail-header">
            <div><h3>{detail.nomeMotorista || 'Motorista sem nome'}</h3><span>{formatPhone(detail.telefone)} · Solicitação #{detail.solicitacaoId}</span></div>
            <span className={`status-pill ${statusClass(detail.status)}`}>{detail.statusRotulo || STATUS_LABELS[detail.status] || detail.status}</span>
          </div>
          {error && <Notice kind="error">{error}</Notice>}
          <div className="detail-grid">
            <div className="detail-item"><span>Carga</span><strong>{detail.cargaDescricao || 'Não vinculada'}</strong></div>
            <div className="detail-item"><span>Veículo</span><strong>{detail.placa || 'Não informado'}{detail.capacidadeToneladas ? ` · ${detail.capacidadeToneladas} t` : ''}</strong></div>
            <div className="detail-item"><span>CPF</span><strong>{detail.cpfMascarado || 'Não informado'}</strong></div>
            <div className="detail-item"><span>E-mail</span><strong>{detail.email || 'Não informado'}</strong></div>
            <div className="detail-item"><span>Prioridade</span><strong>{detail.prioridade || '—'}</strong></div>
            <div className="detail-item"><span>Compatibilidade</span><strong>{detail.descricaoCompatibilidade || compatibilityLabel(detail.compatibilidade)}</strong></div>
            <div className="detail-item"><span>Atendente responsável</span><strong>{assignedAttendant?.nome || (detail.atendenteId ? `Atendente #${detail.atendenteId}` : 'Não atribuído')}</strong></div>
          </div>
          <div className="association-row">
            <label className="field"><span>Vincular uma carga</span><select disabled={['Contratado', 'Encerrado'].includes(detail.status)} value={selectedLoadId} onChange={(event) => setSelectedLoadId(event.target.value)}><option value="">Sem carga vinculada</option>{availableLoads.map((load) => <option key={load.cargaId} value={load.cargaId}>{load.origem} → {load.destino}{load.capacidadeMinimaToneladas ? ` · mín. ${load.capacidadeMinimaToneladas} t` : ''}</option>)}</select></label>
            <button className="button subtle" disabled={busy || ['Contratado', 'Encerrado'].includes(detail.status) || selectedLoadId === (detail.cargaId?.toString() || '')} onClick={saveLoad}>{busy ? 'Salvando…' : 'Salvar carga'}</button>
          </div>
          {detail.camposPendentes?.length > 0 && <Notice kind="warning">Campos pendentes: {detail.camposPendentes.join(', ')}</Notice>}
          <section className="documents-panel">
            <h4>Documentos do formulário</h4>
            {!detail.documentos?.length ? <p className="muted">Nenhum documento configurado.</p> : detail.documentos.map((doc) => (
              <div className="document-row" key={doc.chave}>
                <div><strong>{doc.rotulo}</strong><small>{doc.nomeOriginal || (doc.obrigatorio ? 'Obrigatório · ainda não enviado' : 'Opcional · ainda não enviado')}</small></div>
                <div className="document-actions">
                  <span className={`status-pill ${statusClass(doc.situacao)}`}>{doc.situacao || 'Ausente'}</span>
                  {doc.documentoId && doc.situacao === 'AguardandoValidacao' && <>
                    <button className="button mini success" disabled={busy} onClick={() => validateDocument(doc.documentoId, 'Validado')}>Validar</button>
                    <button className="button mini danger" disabled={busy} onClick={() => validateDocument(doc.documentoId, 'Reprovado')}>Reprovar</button>
                  </>}
                  {doc.documentoId && <button className="button mini subtle" onClick={() => downloadDocument(doc.documentoId, setError)}>Baixar</button>}
                </div>
              </div>
            ))}
          </section>
          <div className="modal-actions spread">
            <span className="muted small-text">Aberta em {formatDate(detail.dataCriacao)}</span>
            <div className="button-row">
              {currentUserId && detail.atendenteId !== currentUserId && <button className="button subtle" disabled={busy} onClick={assumeAttendance}>{busy ? 'Atribuindo…' : 'Assumir atendimento'}</button>}
              {STATUS_OPTIONS.filter(([value]) => value !== detail.status).map(([value, label]) => <button key={value} className={`button ${value === 'Encerrado' ? 'subtle' : 'primary'}`} disabled={busy} onClick={() => changeStatus(value)}>{busy ? 'Salvando…' : label}</button>)}
            </div>
          </div>
        </>
      )}
      {error && !detail && <Notice kind="error">{error}</Notice>}
    </Modal>
  );
}

async function downloadDocument(id, setError) {
  try {
    await download(`/Documentos/${id}/arquivo`, `documento-${id}`);
  } catch (requestError) {
    setError(requestError.message);
  }
}

function MotoristasPage() {
  const [search, setSearch] = useState('');
  const [term, setTerm] = useState('');
  const [drivers, setDrivers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreate, setShowCreate] = useState(false);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    const query = term ? `?busca=${encodeURIComponent(term)}` : '';
    api(`/Motoristas${query}`)
      .then((result) => { if (active) setDrivers(result); })
      .catch((requestError) => { if (active) setError(requestError.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [term]);

  return (
    <>
      <section className="page-intro">
        <div><h2>Base de motoristas</h2><p>Consulte os contatos e os dados da solicitação mais recente.</p></div>
        <button className="button primary" onClick={() => setShowCreate(true)}>＋ Cadastrar motorista</button>
      </section>
      <section className="surface table-surface">
        <form className="table-toolbar" onSubmit={(event) => { event.preventDefault(); setTerm(search.trim()); }}>
          <div><strong>{loading ? 'Carregando…' : `${drivers.length} motoristas`}</strong><span>Pesquise por nome, telefone ou placa.</span></div>
          <div className="search-control"><input aria-label="Buscar motorista" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar contatos…" /><button className="button subtle" type="submit">Buscar</button></div>
        </form>
        {error && <Notice kind="error">{error}</Notice>}
        {loading ? <Loading label="Buscando motoristas…" /> : drivers.length ? (
          <div className="table-scroll"><table><thead><tr><th>Motorista</th><th>Telefone</th><th>Veículo</th><th>Última solicitação</th><th>Cadastro</th></tr></thead><tbody>
            {drivers.map((driver) => <tr key={driver.motoristaId}>
              <td><div className="table-person"><span className="avatar soft-avatar">{initials(driver.nomeCompleto || 'M')}</span><span><strong>{driver.nomeCompleto || 'Nome pendente'}</strong><small>{driver.email || driver.cpfMascarado || 'Dados não informados'}</small></span></div></td>
              <td>{formatPhone(driver.telefone)}</td>
              <td>{driver.placa ? <><strong>{driver.placa}</strong>{driver.capacidadeToneladas && <small className="table-subline">{driver.capacidadeToneladas} toneladas</small>}</> : '—'}</td>
              <td>{driver.statusUltimaSolicitacao ? <span className={`status-pill ${statusClass(driver.statusUltimaSolicitacao)}`}>{STATUS_LABELS[driver.statusUltimaSolicitacao] || driver.statusUltimaSolicitacao}</span> : 'Sem solicitação'}</td>
              <td>{formatDate(driver.dataCadastro)}</td>
            </tr>)}
          </tbody></table></div>
        ) : <EmptyState title="Nenhum motorista encontrado" detail={term ? 'Tente outra busca.' : 'Os contatos criados no atendimento vão aparecer aqui.'} />}
      </section>
      {showCreate && <CreateDriverModal onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); setTerm(''); }} />}
    </>
  );
}

function CreateDriverModal({ onClose, onCreated }) {
  const [form, setForm] = useState({ nomeCompleto: '', telefone: '', email: '', rntrc: '' });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      await api('/Motoristas', { method: 'POST', body: JSON.stringify(form) });
      onCreated();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }
  return <Modal title="Cadastrar motorista" onClose={onClose}>
    <form className="stack-form" onSubmit={submit}>
      <label className="field"><span>Nome completo</span><input required maxLength="150" value={form.nomeCompleto} onChange={(event) => setForm({ ...form, nomeCompleto: event.target.value })} /></label>
      <label className="field"><span>Telefone com DDD</span><input required type="tel" maxLength="20" value={form.telefone} onChange={(event) => setForm({ ...form, telefone: event.target.value })} /></label>
      <label className="field"><span>E-mail (opcional)</span><input type="email" maxLength="254" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label>
      <label className="field"><span>RNTRC (opcional)</span><input maxLength="20" value={form.rntrc} onChange={(event) => setForm({ ...form, rntrc: event.target.value })} /></label>
      {error && <Notice kind="error">{error}</Notice>}
      <div className="modal-actions"><button className="button subtle" type="button" onClick={onClose}>Cancelar</button><button className="button primary" disabled={busy}>{busy ? 'Salvando…' : 'Salvar motorista'}</button></div>
    </form>
  </Modal>;
}

function CargasPage({ isAdmin }) {
  const [loads, setLoads] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [refresh, setRefresh] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    api('/Cargas')
      .then((result) => { if (active) setLoads(result); })
      .catch((requestError) => { if (active) setError(requestError.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [refresh]);

  async function changeStatus(load) {
    const next = load.status === 'Disponivel' ? 'Contratada' : 'Disponivel';
    setError('');
    try {
      await api(`/Cargas/${load.cargaId}/status`, { method: 'PATCH', body: JSON.stringify({ novoStatus: next }) });
      setRefresh((value) => value + 1);
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  return (
    <>
      <section className="page-intro">
        <div><h2>Cargas disponíveis</h2><p>Cadastre oportunidades e acompanhe o status das cargas.</p></div>
        {isAdmin && <button className="button primary" onClick={() => setShowCreate(true)}>＋ Nova carga</button>}
      </section>
      {!isAdmin && <Notice kind="info">Somente administradores podem cadastrar ou alterar o status das cargas.</Notice>}
      {error && <Notice kind="error">{error}</Notice>}
      <section className="load-grid">
        {loading ? <Loading label="Carregando cargas…" /> : loads.length ? loads.map((load) => (
          <article className="surface load-card" key={load.cargaId}>
            <div className="load-card-top"><span className={`status-pill ${load.status === 'Disponivel' ? 'positive' : 'neutral'}`}>{load.status === 'Disponivel' ? 'Disponível' : 'Contratada'}</span><span className="load-number">#{load.cargaId}</span></div>
            <div className="route-line"><div><small>ORIGEM</small><strong>{load.origem}</strong></div><span aria-hidden="true">→</span><div><small>DESTINO</small><strong>{load.destino}</strong></div></div>
            <div className="load-card-bottom"><span>Capacidade mínima <strong>{load.capacidadeMinimaToneladas ? `${load.capacidadeMinimaToneladas} t` : 'Não definida'}</strong></span><span>{formatDate(load.dataCadastro)}</span></div>
            {isAdmin && <button className="button subtle small full-button" onClick={() => changeStatus(load)}>{load.status === 'Disponivel' ? 'Marcar como contratada' : 'Reabrir carga'}</button>}
          </article>
        )) : <EmptyState title="Nenhuma carga cadastrada" detail="As novas cargas aparecerão nesta lista." />}
      </section>
      {showCreate && <CreateLoadModal onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); setRefresh((value) => value + 1); }} />}
    </>
  );
}

function CreateLoadModal({ onClose, onCreated }) {
  const [form, setForm] = useState({ origem: '', destino: '', capacidadeMinimaToneladas: '' });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      await api('/Cargas', {
        method: 'POST',
        body: JSON.stringify({
          origem: form.origem,
          destino: form.destino,
          capacidadeMinimaToneladas: form.capacidadeMinimaToneladas ? Number(form.capacidadeMinimaToneladas) : null,
        }),
      });
      onCreated();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }
  return <Modal title="Cadastrar carga" onClose={onClose}>
    <form className="stack-form" onSubmit={submit}>
      <label className="field"><span>Origem</span><input required maxLength="150" value={form.origem} onChange={(event) => setForm({ ...form, origem: event.target.value })} /></label>
      <label className="field"><span>Destino</span><input required maxLength="150" value={form.destino} onChange={(event) => setForm({ ...form, destino: event.target.value })} /></label>
      <label className="field"><span>Capacidade mínima (toneladas)</span><input type="number" min="0.01" max="999999.99" step="0.01" value={form.capacidadeMinimaToneladas} onChange={(event) => setForm({ ...form, capacidadeMinimaToneladas: event.target.value })} placeholder="Opcional" /></label>
      {error && <Notice kind="error">{error}</Notice>}
      <div className="modal-actions"><button className="button subtle" type="button" onClick={onClose}>Cancelar</button><button className="button primary" disabled={busy}>{busy ? 'Salvando…' : 'Salvar carga'}</button></div>
    </form>
  </Modal>;
}

function ReportsPage() {
  const [metrics, setMetrics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    api(`/Relatorios/indicadores${dateQuery(from, to)}`)
      .then((result) => { if (active) setMetrics(result); })
      .catch((requestError) => { if (active) setError(requestError.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [from, to]);

  async function exportCsv() {
    setBusy(true);
    setError('');
    try {
      await download(`/Relatorios/solicitacoes/exportar${dateQuery(from, to)}`, 'solicitacoes.csv');
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }

  const cards = metrics ? [
    ['Solicitações', metrics.totalSolicitacoes ?? 0],
    ['Cadastros completos', metrics.cadastrosCompletos ?? 0],
    ['Contratadas', metrics.contratadas ?? 0],
    ['Taxa de conversão', `${metrics.taxaConversaoPercentual ?? 0}%`],
  ] : [];

  return (
    <>
      <section className="page-intro">
        <div><h2>Indicadores da operação</h2><p>Monitore o funil e exporte os dados de solicitações em CSV.</p></div>
        <button className="button primary" onClick={exportCsv} disabled={busy || loading}>{busy ? 'Preparando…' : '↓ Exportar CSV'}</button>
      </section>
      <section className="surface report-filters">
        <span className="filter-label">Período dos indicadores</span>
        <label className="field compact-field"><span>De</span><input type="date" value={from} onChange={(event) => setFrom(event.target.value)} /></label>
        <label className="field compact-field"><span>Até</span><input type="date" value={to} onChange={(event) => setTo(event.target.value)} /></label>
      </section>
      {error && <Notice kind="error">{error}</Notice>}
      {loading ? <Loading label="Carregando indicadores…" /> : metrics && (
        <>
          <div className="metric-grid">{cards.map(([label, value]) => <article className="surface metric-card" key={label}><span>{label}</span><strong>{value}</strong></article>)}</div>
          <div className="report-lower-grid">
            <section className="surface report-panel"><h3>Solicitações por etapa</h3>
              {Object.entries(metrics.porStatus || {}).map(([status, count]) => <div className="status-bar-row" key={status}><span>{STATUS_LABELS[status] || status}</span><strong>{count}</strong></div>)}
              {!Object.keys(metrics.porStatus || {}).length && <p className="muted">Sem dados no período escolhido.</p>}
            </section>
            <section className="surface report-panel"><h3>Tempo médio de atendimento</h3>
              <div className="duration-stat"><strong>{formatMinutes(metrics.tempoMedioAteProntaParaAnaliseMinutos)}</strong><span>até cadastro completo</span></div>
              <div className="duration-stat"><strong>{formatMinutes(metrics.tempoMedioTotalAtendimentoMinutos)}</strong><span>até conclusão</span></div>
              <div className="completion-note">{metrics.percentualCadastrosCompletos ?? 0}% dos cadastros foram concluídos.</div>
            </section>
          </div>
        </>
      )}
    </>
  );
}

function PublicFormPage() {
  const { token: routeToken } = useParams();
  const [searchParams] = useSearchParams();
  const token = routeToken || searchParams.get('token') || '';
  const [form, setForm] = useState(null);
  const [values, setValues] = useState({ nomeCompleto: '', email: '', cpf: '', veiculoId: '', placa: '', capacidadeToneladas: '' });
  const [files, setFiles] = useState({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    if (!token) {
      setError('Este link não contém um token válido.');
      setLoading(false);
      return;
    }
    api('/Formulario', { headers: { 'X-Token-Acesso': token } })
      .then((result) => {
        setForm(result);
        setValues({
          nomeCompleto: result.dados?.nomeCompleto || '',
          email: result.dados?.email || '',
          cpf: result.dados?.cpfMascarado || '',
          veiculoId: result.dados?.veiculoSelecionadoId?.toString() || '',
          placa: '',
          capacidadeToneladas: '',
        });
      })
      .catch((requestError) => setError(requestError.message))
      .finally(() => setLoading(false));
  }, [token]);

  const fields = form?.campos || [];
  const required = (key) => fields.find((field) => field.chave === key)?.obrigatorio;
  const setValue = (key, value) => setValues((current) => ({ ...current, [key]: value }));

  async function saveData() {
    const selectedVehicle = form?.dados?.veiculos?.find((vehicle) => vehicle.veiculoId.toString() === values.veiculoId);
    const payload = {
      nomeCompleto: values.nomeCompleto,
      email: values.email,
      cpf: values.cpf,
      veiculoId: selectedVehicle ? selectedVehicle.veiculoId : null,
      placa: selectedVehicle ? null : values.placa,
      capacidadeToneladas: selectedVehicle ? null : (values.capacidadeToneladas ? Number(values.capacidadeToneladas) : null),
    };
    const result = await api('/Formulario/dados', {
      method: 'PUT',
      headers: { 'X-Token-Acesso': token },
      body: JSON.stringify(payload),
    });
    setForm(result);
    setValues((current) => ({ ...current, cpf: result.dados?.cpfMascarado || current.cpf }));
  }

  async function sendDocument(doc) {
    const file = files[doc.chave];
    if (!file) return;
    if (file.size > 10 * 1024 * 1024) {
      setError(`O arquivo ${file.name} excede o limite de 10 MB.`);
      return;
    }
    const body = new FormData();
    body.append('Tipo', doc.chave);
    body.append('Arquivo', file);
    await api('/Formulario/documentos', { method: 'POST', headers: { 'X-Token-Acesso': token }, body });
    setForm(await api('/Formulario', { headers: { 'X-Token-Acesso': token } }));
    setFiles((current) => ({ ...current, [doc.chave]: null }));
  }

  async function submit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');
    try {
      await saveData();
      for (const doc of form.documentos || []) {
        if (files[doc.chave]) await sendDocument(doc);
      }
      const result = await api('/Formulario/enviar', { method: 'POST', headers: { 'X-Token-Acesso': token } });
      if (result.camposPendentes?.length || result.documentosPendentes?.length) {
        const pending = [...(result.camposPendentes || []), ...(result.documentosPendentes || [])].join(', ');
        setNotice(`${result.mensagem} Pendências: ${pending}`);
      } else {
        setSubmitted(true);
      }
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <main className="public-form-page">
      <header className="public-form-header"><Link className="brand-lockup" to="/"><span className="brand-mark">F</span><span>Fretebot</span></Link><span className="secure-label">● Link seguro</span></header>
      <section className="public-form-card">
        {loading ? <Loading label="Validando seu link…" /> : error && !form ? <><Notice kind="error">{error}</Notice><p className="muted">Peça ao atendente um novo link de acesso caso ele tenha expirado ou já tenha sido utilizado.</p></> : submitted ? (
          <div className="form-success"><span className="success-check">✓</span><span className="eyebrow">Cadastro enviado</span><h1>Obrigado, recebemos seus dados.</h1><p>A equipe vai analisar as informações e entrar em contato para continuar o atendimento.</p></div>
        ) : form && (
          <>
            <div className="public-form-title"><span className="eyebrow">Solicitação #{form.solicitacaoId}</span><h1>Complete seu cadastro</h1><p>Olá{form.dados.nomeCompleto ? `, ${form.dados.nomeCompleto}` : ''}! Preencha os dados e envie os documentos para a equipe analisar sua solicitação.</p></div>
            {error && <Notice kind="error">{error}</Notice>}
            {notice && <Notice kind="warning">{notice}</Notice>}
            <form className="public-form" onSubmit={submit}>
              <section className="public-section">
                <div className="section-title"><span>01</span><div><h2>Seus dados</h2><p>Informações pessoais e do veículo.</p></div></div>
                <div className="public-field-grid">
                  {fields.some((field) => field.chave === 'nome') && <label className="field"><span>Nome completo{required('nome') ? ' *' : ''}</span><input required={required('nome')} maxLength="150" value={values.nomeCompleto} onChange={(event) => setValue('nomeCompleto', event.target.value)} /></label>}
                  {fields.some((field) => field.chave === 'telefone') && <label className="field"><span>Telefone</span><input value={formatPhone(form.dados.telefone)} readOnly /></label>}
                  {fields.some((field) => field.chave === 'email') && <label className="field"><span>E-mail{required('email') ? ' *' : ''}</span><input type="email" required={required('email')} maxLength="254" value={values.email} onChange={(event) => setValue('email', event.target.value)} /></label>}
                  {fields.some((field) => field.chave === 'cpf') && <label className="field"><span>CPF{required('cpf') ? ' *' : ''}</span><input required={required('cpf')} maxLength="14" value={values.cpf} onChange={(event) => setValue('cpf', event.target.value)} placeholder="000.000.000-00" /></label>}
                  {fields.some((field) => field.chave === 'placa') && form.dados.veiculos.length > 0 && <label className="field"><span>Veículo cadastrado</span><select value={values.veiculoId} onChange={(event) => { setValue('veiculoId', event.target.value); setValue('placa', ''); }}><option value="">Cadastrar outro veículo</option>{form.dados.veiculos.map((vehicle) => <option value={vehicle.veiculoId} key={vehicle.veiculoId}>{vehicle.placa} · {vehicle.capacidadeToneladas} t</option>)}</select></label>}
                  {fields.some((field) => field.chave === 'placa') && !values.veiculoId && <label className="field"><span>Placa{required('placa') ? ' *' : ''}</span><input required={required('placa')} maxLength="8" value={values.placa} onChange={(event) => setValue('placa', event.target.value.toUpperCase())} placeholder="ABC1D23" /></label>}
                  {fields.some((field) => field.chave === 'capacidade') && !values.veiculoId && <label className="field"><span>Capacidade em toneladas{required('capacidade') ? ' *' : ''}</span><input required={required('capacidade')} type="number" min="0.01" max="200" step="0.01" value={values.capacidadeToneladas} onChange={(event) => setValue('capacidadeToneladas', event.target.value)} /></label>}
                </div>
              </section>
              <section className="public-section">
                <div className="section-title"><span>02</span><div><h2>Documentos</h2><p>Envie arquivos PDF, JPG ou PNG de até 10 MB.</p></div></div>
                <div className="upload-list">
                  {form.documentos.map((doc) => <label className="upload-row" key={doc.chave}>
                    <span className={`upload-state ${doc.estado === 'Validado' ? 'uploaded' : ''}`}>{doc.estado === 'Validado' ? '✓' : '↑'}</span>
                    <span className="upload-copy"><strong>{doc.rotulo}{doc.obrigatorio && <em>Obrigatório</em>}</strong><small>{doc.nomeOriginal || uploadStatus(doc.estado)}</small></span>
                    <input type="file" accept=".pdf,image/jpeg,image/png" onChange={(event) => setFiles((current) => ({ ...current, [doc.chave]: event.target.files?.[0] || null }))} />
                    <span className="button subtle small">{files[doc.chave]?.name || 'Escolher arquivo'}</span>
                  </label>)}
                  {!form.documentos.length && <p className="muted">Não há documentos obrigatórios configurados.</p>}
                </div>
              </section>
              <div className="public-submit"><p>Seus dados são usados somente para a análise desta solicitação.</p><button className="button primary" disabled={saving}>{saving ? 'Enviando seus dados…' : 'Salvar e enviar cadastro'} <span>→</span></button></div>
            </form>
          </>
        )}
      </section>
      <footer className="public-footer">Fretebot · Captação e gestão de fretes</footer>
    </main>
  );
}

function Modal({ title, onClose, children, wide }) {
  useEffect(() => {
    const onKeyDown = (event) => { if (event.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [onClose]);

  return <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}><section className={`modal ${wide ? 'wide' : ''}`} role="dialog" aria-modal="true" aria-label={title}><header className="modal-header"><h2>{title}</h2><button className="close-button" onClick={onClose} aria-label="Fechar">×</button></header><div className="modal-content">{children}</div></section></div>;
}

function Notice({ kind = 'info', children }) {
  return <div className={`notice ${kind}`} role={kind === 'error' ? 'alert' : 'status'}>{children}</div>;
}

function Loading({ label }) {
  return <div className="loading-state"><span className="spinner" />{label}</div>;
}

function EmptyState({ title, detail }) {
  return <div className="empty-state"><span className="empty-icon">⌕</span><strong>{title}</strong><p>{detail}</p></div>;
}

function initials(name = '') {
  return name.trim().split(/\s+/).slice(0, 2).map((part) => part[0] || '').join('').toUpperCase() || 'FB';
}

function formatDate(value) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: 'short' }).format(date);
}

function formatPhone(value = '') {
  const digits = String(value).replace(/\D/g, '');
  const local = digits.startsWith('55') && digits.length > 11 ? digits.slice(2) : digits;
  if (local.length === 11) return `(${local.slice(0, 2)}) ${local.slice(2, 7)}-${local.slice(7)}`;
  if (local.length === 10) return `(${local.slice(0, 2)}) ${local.slice(2, 6)}-${local.slice(6)}`;
  return value || '—';
}

function priorityClass(value = '') {
  return `priority-${value.toLowerCase()}`;
}

function statusClass(value = '') {
  const status = value.toLowerCase();
  if (status.includes('validado') || status.includes('contratado') || status === 'positivo') return 'positive';
  if (status.includes('pend') || status.includes('aguardando') || status.includes('analise')) return 'warning';
  if (status.includes('reprov') || status.includes('encerrado')) return 'negative';
  return 'neutral';
}

function compatibilityLabel(value = '') {
  return ({
    Compativel: 'Compatível',
    Incompativel: 'Incompatível',
    NaoVerificada: 'Não verificada',
    SemRestricao: 'Sem restrição',
    NaoAplicavel: 'Não aplicável',
  })[value] || value || '—';
}

function uploadStatus(state) {
  return ({
    Ausente: 'Nenhum arquivo enviado',
    AguardandoValidacao: 'Aguardando validação da equipe',
    Validado: 'Documento validado',
    Reprovado: 'Documento reprovado. Envie uma nova versão.',
  })[state] || 'Nenhum arquivo enviado';
}

function dateQuery(from, to) {
  const params = new URLSearchParams();
  if (from) params.set('de', from);
  if (to) params.set('ate', to);
  const query = params.toString();
  return query ? `?${query}` : '';
}

function formatMinutes(value) {
  if (value == null) return '—';
  if (value < 60) return `${value} min`;
  const hours = Math.floor(value / 60);
  const minutes = Math.round(value % 60);
  return minutes ? `${hours}h ${minutes}min` : `${hours}h`;
}

export default App;
