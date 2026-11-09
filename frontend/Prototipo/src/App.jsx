import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, Navigate, Route, Routes, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { api, clearToken, download, getToken, saveToken } from './api';
import cccLogo from './assets/ccc-logo.png';

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
function allowedStatusOptions(status) {
  if (['Contratado', 'Encerrado'].includes(status)) return [];
  if (status === 'EmAnalise') return STATUS_OPTIONS.filter(([v]) => v !== 'EmAnalise' && v !== undefined);
  if (['ComPendencias', 'ProntoParaAnalise'].includes(status)) return STATUS_OPTIONS.filter(([v]) => v === 'EmAnalise' || v === 'Encerrado');
  return STATUS_OPTIONS.filter(([v]) => v === 'Encerrado');
}

function readUser() {
  try {
    return JSON.parse(sessionStorage.getItem('fretebot.user') || 'null');
  } catch {
    return null;
  }
}

function readSavedTheme() {
  try {
    return localStorage.getItem('fretebot.theme') === 'dark';
  } catch {
    return false;
  }
}

function App() {
  const [token, setToken] = useState(getToken());
  const [user, setUser] = useState(readUser);
  const [darkMode, setDarkMode] = useState(readSavedTheme);

  useEffect(() => {
    const theme = darkMode ? 'dark' : 'light';
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
    try {
      localStorage.setItem('fretebot.theme', theme);
    } catch {
      // The selected theme still applies for this session if storage is unavailable.
    }
  }, [darkMode]);

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
      <Route path="/admin/*" element={token ? <AdminApp user={user} darkMode={darkMode} onToggleDarkMode={() => setDarkMode((value) => !value)} onSignOut={signOut} /> : <Navigate to="/login" replace />} />
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
        <div className="brand-lockup"><img className="brand-logo" src={cccLogo} alt="CCC — Central de Cargas e Contratação" /></div>
        <div>
          <span className="eyebrow">Central de Carga e Contratação</span>
          <h1>Do primeiro contato à contratação, em um só lugar.</h1>
          <p>Organize motoristas, acompanhe cadastros e conecte sua equipe à operação de fretes.</p>
        </div>
        <div className="login-animation" aria-hidden="true">
          <span className="pixel-crate crate-one" />
          <span className="pixel-crate crate-two" />
          <span className="pixel-crate crate-three" />
          <div className="pixel-truck">
            <span className="truck-smoke smoke-one" />
            <span className="truck-smoke smoke-two" />
            <span className="truck-smoke smoke-three" />
            <span className="truck-cargo" />
            <span className="truck-cabin"><span /></span>
            <span className="truck-wheel wheel-one" />
            <span className="truck-wheel wheel-two" />
          </div>
          <span className="truck-road" />
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

function AdminApp({ user, darkMode, onToggleDarkMode, onSignOut }) {
  const location = useLocation();
  const isAdmin = (user?.perfil || '').toLowerCase().includes('admin');
  const profileKey = `fretebot.profilePhoto.${(user?.email || user?.nome || 'local').toLowerCase()}`;
  const [profilePhoto, setProfilePhoto] = useState(() => {
    try {
      return localStorage.getItem(profileKey) || '';
    } catch {
      return '';
    }
  });
  const [profileMenuOpen, setProfileMenuOpen] = useState(false);
  const [profileError, setProfileError] = useState('');
  const profileMenuRef = useRef(null);
  const photoInputRef = useRef(null);
  const title = useMemo(() => {
    if (location.pathname.startsWith('/admin/motoristas')) return 'Motoristas';
    if (location.pathname.startsWith('/admin/cargas')) return 'Cargas';
    if (location.pathname.startsWith('/admin/relatorios')) return 'Relatórios';
    if (location.pathname.startsWith('/admin/equipe')) return 'Equipe e acessos';
    if (location.pathname.startsWith('/admin/formulario')) return 'Formulário e documentos';
    return 'Central de atendimento';
  }, [location.pathname]);

  useEffect(() => {
    if (!profileMenuOpen) return undefined;
    const closeOnOutsideClick = (event) => {
      if (!profileMenuRef.current?.contains(event.target)) setProfileMenuOpen(false);
    };
    const closeOnEscape = (event) => {
      if (event.key === 'Escape') setProfileMenuOpen(false);
    };
    document.addEventListener('pointerdown', closeOnOutsideClick);
    document.addEventListener('keydown', closeOnEscape);
    return () => {
      document.removeEventListener('pointerdown', closeOnOutsideClick);
      document.removeEventListener('keydown', closeOnEscape);
    };
  }, [profileMenuOpen]);

  function saveProfilePhoto(event) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      setProfileError('Escolha uma imagem JPG, PNG ou WEBP.');
      return;
    }
    if (file.size > 2 * 1024 * 1024) {
      setProfileError('A foto deve ter no máximo 2 MB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result !== 'string') {
        setProfileError('Não foi possível carregar esta imagem.');
        return;
      }
      try {
        localStorage.setItem(profileKey, reader.result);
        setProfilePhoto(reader.result);
        setProfileError('');
        setProfileMenuOpen(false);
      } catch {
        setProfileError('Não foi possível salvar a foto neste navegador.');
      }
    };
    reader.onerror = () => setProfileError('Não foi possível carregar esta imagem.');
    reader.readAsDataURL(file);
  }

  function removeProfilePhoto() {
    try {
      localStorage.removeItem(profileKey);
      setProfilePhoto('');
      setProfileError('');
    } catch {
      setProfileError('Não foi possível remover a foto neste navegador.');
    }
  }

  return (
    <div className="admin-shell">
      <aside className="sidebar">
        <Link className="brand-lockup sidebar-brand" to="/admin"><img className="brand-logo" src={cccLogo} alt="CCC — Central de Cargas e Contratação" /></Link>
        <div className="sidebar-caption">OPERAÇÃO</div>
        <nav className="side-nav">
          <NavLink to="/admin" exact icon="◫">Central de atendimento</NavLink>
          <NavLink to="/admin/motoristas" icon="♙">Motoristas</NavLink>
          <NavLink to="/admin/cargas" icon="▰">Cargas</NavLink>
          {isAdmin && <>
            <div className="sidebar-caption admin-nav-caption">ADMINISTRAÇÃO</div>
            <NavLink to="/admin/formulario" icon="▤">Formulário e documentos</NavLink>
            <NavLink to="/admin/equipe" icon="♧">Equipe e acessos</NavLink>
            <NavLink to="/admin/relatorios" icon="▥">Relatórios</NavLink>
          </>}
        </nav>
        <div className="sidebar-bottom">
          <div className="sidebar-user">
          <UserAvatar name={user?.nome} photo={profilePhoto} className="avatar" />
            <span className="user-meta"><strong>{user?.nome || 'Atendente'}</strong><small>{user?.perfil || 'Equipe'}</small></span>
          </div>
          <button className="signout-button" onClick={onSignOut} title="Sair" aria-label="Sair">↪</button>
        </div>
      </aside>
      <div className="workspace">
        <header className="topbar">
          <div><div className="breadcrumb">CCC <span>/</span> {title}</div><h1>{title}</h1></div>
          <div className="topbar-right">
            <span className="connection-status"><i /> Sessão ativa</span>
            <div className="profile-menu-anchor" ref={profileMenuRef}>
              <button
                type="button"
                className="profile-trigger"
                title="Opções do perfil"
                aria-label="Abrir opções do perfil"
                aria-haspopup="menu"
                aria-expanded={profileMenuOpen}
                onClick={() => { setProfileMenuOpen((open) => !open); setProfileError(''); }}
              >
                <UserAvatar name={user?.nome} photo={profilePhoto} className="avatar top-avatar" />
              </button>
              {profileMenuOpen && <div className="profile-menu" role="menu" aria-label="Opções do perfil">
                <button className="profile-menu-item" type="button" role="menuitem" onClick={() => photoInputRef.current?.click()}>
                  <span className="profile-menu-icon" aria-hidden="true">▧</span>
                  <span>Definir foto de perfil</span>
                </button>
                {profilePhoto && <button className="profile-menu-item" type="button" role="menuitem" onClick={removeProfilePhoto}>
                  <span className="profile-menu-icon" aria-hidden="true">×</span>
                  <span>Remover foto</span>
                </button>}
                <div className="profile-menu-divider" />
                <label className="profile-menu-item profile-theme-item" role="menuitem">
                  <span className="profile-menu-icon" aria-hidden="true">{darkMode ? '☾' : '☼'}</span>
                  <span>Modo noturno</span>
                  <input type="checkbox" role="switch" aria-label="Modo noturno" checked={darkMode} onChange={onToggleDarkMode} />
                </label>
                {profileError && <p className="profile-menu-error" role="alert">{profileError}</p>}
                <p className="profile-menu-hint">Foto salva somente neste navegador · JPG, PNG ou WEBP até 2 MB</p>
              </div>}
              <input ref={photoInputRef} className="profile-photo-input" type="file" accept="image/jpeg,image/png,image/webp" onChange={saveProfilePhoto} />
            </div>
          </div>
        </header>
        <main className="page-content">
          <Routes>
            <Route index element={<CentralPage user={user} />} />
            <Route path="motoristas" element={<MotoristasPage />} />
            <Route path="cargas" element={<CargasPage isAdmin={isAdmin} />} />
            {isAdmin && <Route path="relatorios" element={<ReportsPage />} />}
            {isAdmin && <Route path="formulario" element={<FormularioConfigPage />} />}
            {isAdmin && <Route path="equipe" element={<EquipePage />} />}
            <Route path="*" element={<Navigate to="/admin" replace />} />
          </Routes>
        </main>
      </div>
    </div>
  );
}

function UserAvatar({ name, photo, className }) {
  return (
    <span className={`${className}${photo ? ' has-photo' : ''}`}>
      {photo ? <img src={photo} alt="" /> : initials(name)}
    </span>
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
  const [priorityFilter, setPriorityFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
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

  const requests = columns.flatMap((column) => column.solicitacoes || []);
  const totals = requests.length;
  const openCount = columns
    .filter((column) => !['Contratado', 'Encerrado'].includes(column.status))
    .reduce((total, column) => total + (column.solicitacoes?.length || 0), 0);
  const readyCount = columns.find((column) => column.status === 'ProntoParaAnalise')?.solicitacoes?.length || 0;
  const highPriorityCount = requests.filter((item) => item.prioridade === 'Alta').length;
  const hiredCount = columns.find((column) => column.status === 'Contratado')?.solicitacoes?.length || 0;
  const visibleColumns = columns
    .filter((column) => !statusFilter || column.status === statusFilter)
    .map((column) => ({
      ...column,
      solicitacoes: (column.solicitacoes || []).filter((item) => !priorityFilter || item.prioridade === priorityFilter),
    }));
  const refreshBoard = () => setRefresh((value) => value + 1);

  return (
    <>
      <section className="page-intro">
        <div><h2>Funil de captação</h2><p>Acompanhe cada solicitação e avance o atendimento conforme a operação.</p></div>
        <button className="button primary" onClick={() => setShowNew(true)}><span>＋</span> Iniciar atendimento</button>
      </section>
      <div className="kanban-kpis">
        <article className="surface metric-card"><span>Solicitações abertas</span><strong>{openCount}</strong></article>
        <article className="surface metric-card"><span>Prontas para análise</span><strong>{readyCount}</strong></article>
        <article className="surface metric-card"><span>Prioridade alta</span><strong>{highPriorityCount}</strong></article>
        <article className="surface metric-card"><span>Contratadas</span><strong>{hiredCount}</strong></article>
      </div>
      <div className="board-summary">
        <span><strong>{totals}</strong> solicitações no funil</span>
        <span className="refresh-stamp">{realtimeState === 'connected' ? 'Atualizações em tempo real ativas' : 'Atualizações automáticas indisponíveis'}</span>
        <button className="button subtle small" onClick={refreshBoard} disabled={loading}>↻ Atualizar</button>
      </div>
      <div className="board-filters">
        <label className="field compact-field"><span>Filtrar por status</span><select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}><option value="">Todos os status</option>{columns.map((column) => <option value={column.status} key={column.status}>{column.rotulo}</option>)}</select></label>
        <label className="field compact-field"><span>Filtrar por prioridade</span><select value={priorityFilter} onChange={(event) => setPriorityFilter(event.target.value)}><option value="">Todas</option><option>Alta</option><option>Media</option><option>Baixa</option></select></label>
        {(statusFilter || priorityFilter) && <button className="button subtle small clear-filters" onClick={() => { setStatusFilter(''); setPriorityFilter(''); }}>Limpar filtros</button>}
      </div>
      {error && <Notice kind="error">{error}</Notice>}
      {realtimeError && <Notice kind="info">{realtimeError}</Notice>}
      {loading ? <Loading label="Carregando solicitações…" /> : (
        <div className="kanban-board">
          {visibleColumns.map((column, index) => (
            <section className={`kanban-column ${STATUS_COLORS[index % STATUS_COLORS.length]}`} key={column.status}>
              <header className="column-heading">
                <span className="column-indicator" />
                <h3>{column.rotulo || STATUS_LABELS[column.status] || column.status}</h3>
                <span className="count-badge">{column.solicitacoes?.length || 0}</span>
              </header>
              <div className="column-cards">
                {column.solicitacoes?.map((item) => <RequestCard key={item.solicitacaoId} item={item} status={column.status} onClick={() => setSelected(item)} />)}
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

function RequestCard({ item, status, onClick }) {
  return (
    <button className="request-card" onClick={onClick}>
      <div className="request-card-top"><span className="request-number">#{item.solicitacaoId}</span><span className={`priority ${priorityClass(item.prioridade)}`}>{item.prioridade || 'Sem prioridade'}</span><span className="request-date">{formatDate(item.dataCriacao)}</span></div>
      <strong className="request-name">{item.nomeMotorista || 'Motorista sem nome'}</strong>
      <span className="request-phone">{formatPhone(item.telefone)}</span>
      <div className="request-card-bottom">
        <span>{item.cargaDescricao || 'Sem carga vinculada'}</span>
        {item.pendenciasDocumentais > 0 && <span className="pending-count">{item.pendenciasDocumentais} pend.</span>}
      </div>
      <span className="request-status">{STATUS_LABELS[status] || status} · {compatibilityLabel(item.compatibilidade)}</span>
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

  async function copyLink(event) {
    const input = event.currentTarget.parentElement.querySelector('input');
    try {
      if (!navigator.clipboard?.writeText) throw new Error('Clipboard API indisponível.');
      await navigator.clipboard.writeText(result.link.urlAcesso);
      setCopied(true);
      setError('');
    } catch {
      input.focus();
      input.select();
      input.setSelectionRange(0, input.value.length);
      if (document.execCommand('copy')) {
        setCopied(true);
        setError('');
      } else {
        setCopied(false);
        setError('O endereço está selecionado. Use a opção Copiar do seu aparelho.');
      }
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
          {error && <Notice kind="info">{error}</Notice>}
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
  const pendingRequiredDocuments = detail?.documentos?.filter(
    (doc) => doc.obrigatorio && doc.situacao !== 'Validado',
  ) || [];
  const pendingRequiredFields = detail?.camposPendentes || [];
  const cannotHire = pendingRequiredFields.length > 0 || pendingRequiredDocuments.length > 0;

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
                  {doc.documentoId && <button className="button mini subtle" onClick={() => downloadDocument(doc.documentoId, doc.nomeOriginal, setError)}>Baixar</button>}
                </div>
              </div>
            ))}
          </section>
          {detail.status === 'EmAnalise' && cannotHire && <Notice kind="warning">
            Para contratar, preencha os campos obrigatórios e valide todos os documentos obrigatórios.
          </Notice>}
          <div className="modal-actions spread">
            <span className="muted small-text">Aberta em {formatDate(detail.dataCriacao)}</span>
            <div className="button-row">
              {currentUserId && detail.atendenteId !== currentUserId && <button className="button subtle" disabled={busy} onClick={assumeAttendance}>{busy ? 'Atribuindo…' : 'Assumir atendimento'}</button>}
              {STATUS_OPTIONS.filter(([value]) => value !== detail.status).map(([value, label]) => <button key={value} className={`button ${value === 'Encerrado' ? 'subtle' : 'primary'}`} disabled={busy || (value === 'Contratado' && cannotHire)} onClick={() => changeStatus(value)}>{busy ? 'Salvando…' : label}</button>)}
            </div>
          </div>
        </>
      )}
      {error && !detail && <Notice kind="error">{error}</Notice>}
    </Modal>
  );
}

async function downloadDocument(id, filename, setError) {
  try {
    await download(`/Documentos/${id}/arquivo`, filename || `documento-${id}`);
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
  const [selectedDriver, setSelectedDriver] = useState(null);
  const [selectedRequestId, setSelectedRequestId] = useState(null);
  const [refresh, setRefresh] = useState(0);

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
  }, [term, refresh]);

  const ongoingCount = drivers.filter((driver) => driver.statusUltimaSolicitacao && !['Contratado', 'Encerrado'].includes(driver.statusUltimaSolicitacao)).length;
  const capacityCount = drivers.filter((driver) => driver.capacidadeToneladas != null).length;

  return (
    <>
      <section className="page-intro">
        <div><h2>Base de motoristas</h2><p>Consulte os contatos e os dados da solicitação mais recente.</p></div>
        <button className="button primary" onClick={() => setShowCreate(true)}>＋ Cadastrar motorista</button>
      </section>
      <div className="driver-kpis">
        <article className="surface metric-card"><span>Motoristas cadastrados</span><strong>{loading ? '…' : drivers.length}</strong></article>
        <article className="surface metric-card"><span>Com telefone informado</span><strong>{loading ? '…' : drivers.filter((driver) => driver.telefone).length}</strong></article>
        <article className="surface metric-card"><span>Solicitações em andamento</span><strong>{loading ? '…' : ongoingCount}</strong></article>
        <article className="surface metric-card"><span>Veículos com capacidade</span><strong>{loading ? '…' : capacityCount}</strong></article>
      </div>
      <section className="surface table-surface">
        <form className="table-toolbar" onSubmit={(event) => { event.preventDefault(); setTerm(search.trim()); }}>
          <div><strong>{loading ? 'Carregando…' : `${drivers.length} motoristas`}</strong><span>Pesquise por nome, telefone ou placa.</span></div>
          <div className="search-control"><input aria-label="Buscar motorista" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar contatos…" /><button className="button subtle" type="submit">Buscar</button></div>
        </form>
        {error && <Notice kind="error">{error}</Notice>}
        {loading ? <Loading label="Buscando motoristas…" /> : drivers.length ? (
          <div className="table-scroll"><table className="driver-table"><thead><tr><th>Motorista</th><th>Telefone / WhatsApp</th><th>CPF</th><th>Placa</th><th>Capacidade</th><th>Última solicitação</th><th>Situação</th><th></th></tr></thead><tbody>
            {drivers.map((driver) => <tr key={driver.motoristaId}>
              <td><div className="table-person"><span className="avatar soft-avatar">{initials(driver.nomeCompleto || 'M')}</span><span><strong>{driver.nomeCompleto || 'Nome pendente'}</strong><small>{driver.email || 'E-mail não informado'}</small></span></div></td>
              <td><a className="whatsapp-link" href={`https://wa.me/${String(driver.telefone || '').replace(/\D/g, '')}`} target="_blank" rel="noreferrer">{formatPhone(driver.telefone)} ↗</a></td>
              <td>{driver.cpfMascarado || '—'}</td>
              <td>{driver.placa || '—'}</td>
              <td>{driver.capacidadeToneladas == null ? '—' : `${driver.capacidadeToneladas} t`}</td>
              <td>{driver.ultimaSolicitacaoId ? `#${driver.ultimaSolicitacaoId}` : '—'}</td>
              <td>{driver.statusUltimaSolicitacao ? <span className={`status-pill ${statusClass(driver.statusUltimaSolicitacao)}`}>{STATUS_LABELS[driver.statusUltimaSolicitacao] || driver.statusUltimaSolicitacao}</span> : 'Sem solicitação'}</td>
              <td><button className="button mini subtle" onClick={() => setSelectedDriver(driver.motoristaId)}>Ver cadastro</button></td>
            </tr>)}
          </tbody></table></div>
        ) : <EmptyState title="Nenhum motorista encontrado" detail={term ? 'Tente outra busca.' : 'Os contatos criados no atendimento vão aparecer aqui.'} />}
      </section>
      {showCreate && <CreateDriverModal onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); setTerm(''); setRefresh((value) => value + 1); }} />}
      {selectedDriver && <DriverDetailsModal id={selectedDriver} onClose={() => setSelectedDriver(null)} onOpenRequest={(id) => { setSelectedDriver(null); setSelectedRequestId(id); }} />}
      {selectedRequestId && <RequestDetails id={selectedRequestId} onClose={() => setSelectedRequestId(null)} onUpdated={() => { setSelectedRequestId(null); setRefresh((value) => value + 1); }} />}
    </>
  );
}

function DriverDetailsModal({ id, onClose, onOpenRequest }) {
  const [driver, setDriver] = useState(null);
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    Promise.all([api(`/Motoristas/${id}`), api(`/Solicitacoes/motorista/${id}`)])
      .then(([driverResult, requestResult]) => {
        if (!active) return;
        setDriver({ ...driverResult, compatibilidadeUltimaSolicitacao: requestResult[0]?.compatibilidade });
        setRequests(requestResult);
      })
      .catch((requestError) => { if (active) setError(requestError.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [id]);

  return <Modal title="Ficha do motorista" onClose={onClose}>
    {loading ? <Loading label="Carregando cadastro…" /> : driver && <>
      {error && <Notice kind="error">{error}</Notice>}
      <div className="detail-header"><div><h3>{driver.nomeCompleto || 'Motorista sem nome'}</h3><span>{formatPhone(driver.telefone)} · Cadastrado em {formatDate(driver.dataCadastro)}</span></div></div>
      <div className="detail-grid driver-detail-grid">
        <div className="detail-item"><span>CPF</span><strong>{driver.cpfMascarado || 'Não informado'}</strong></div>
        <div className="detail-item"><span>E-mail</span><strong>{driver.email || 'Não informado'}</strong></div>
        <div className="detail-item"><span>RNTRC</span><strong>{driver.rntrc || 'Não informado'}</strong></div>
        <div className="detail-item"><span>Veículo</span><strong>{driver.placa || 'Não informado'}</strong></div>
        <div className="detail-item"><span>Capacidade</span><strong>{driver.capacidadeToneladas == null ? 'Não informada' : `${driver.capacidadeToneladas} t`}</strong></div>
        <div className="detail-item"><span>Compatibilidade recente</span><strong>{compatibilityLabel(driver.compatibilidadeUltimaSolicitacao)}</strong></div>
      </div>
      <section className="documents-panel">
        <h4>Histórico de solicitações</h4>
        {!requests.length ? <p className="muted">Nenhuma solicitação vinculada a este motorista.</p> : requests.map((request) => <div className="driver-history-row" key={request.solicitacaoId}>
          <div><strong>Solicitação #{request.solicitacaoId}</strong><small>{formatDate(request.dataCriacao)} · {request.cargaDescricao || 'Sem carga vinculada'}</small></div>
          <span className={`status-pill ${statusClass(request.status)}`}>{STATUS_LABELS[request.status] || request.status}</span>
          <button className="button mini subtle" onClick={() => onOpenRequest(request.solicitacaoId)}>Abrir</button>
        </div>)}
      </section>
    </>}
    {error && !driver && <Notice kind="error">{error}</Notice>}
  </Modal>;
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
      await api('/Motoristas', {
  method: 'POST',
  body: JSON.stringify({ ...form, email: form.email || null, rntrc: form.rntrc || null }),
});
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

function EquipePage() {
  const [members, setMembers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [editing, setEditing] = useState(null);
  const [showCreate, setShowCreate] = useState(false);
  const [refresh, setRefresh] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    api('/Atendentes')
      .then((result) => { if (active) setMembers(result); })
      .catch((requestError) => { if (active) setError(requestError.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [refresh]);

  async function toggleActive(member) {
    const action = member.ativo ? 'inativar' : 'reativar';
    setError('');
    try {
      await api(`/Atendentes/${member.atendenteId}/${action}`, { method: 'PATCH' });
      setRefresh((value) => value + 1);
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  function reload() {
    setEditing(null);
    setShowCreate(false);
    setRefresh((value) => value + 1);
  }

  return (
    <>
      <section className="page-intro">
        <div><h2>Equipe e acessos</h2><p>Gerencie os usuários internos e consulte seus perfis e permissões.</p></div>
        <button className="button primary" onClick={() => setShowCreate(true)}>＋ Adicionar usuário</button>
      </section>
      {error && <Notice kind="error">{error}</Notice>}
      <section className="surface table-surface team-table-surface">
        <div className="table-toolbar"><div><strong>Usuários cadastrados</strong><span>Administradores e atendentes com acesso ao painel.</span></div><span className="count-badge">{members.length}</span></div>
        {loading ? <Loading label="Carregando equipe…" /> : members.length ? (
          <div className="table-scroll"><table><thead><tr><th>Usuário</th><th>Perfil</th><th>Estado</th><th>Permissões principais</th><th>Ações</th></tr></thead><tbody>
            {members.map((member) => {
              const isMemberAdmin = (member.perfil || '').toLowerCase().includes('admin');
              return <tr key={member.atendenteId}>
                <td><div className="table-person"><span className="avatar soft-avatar">{initials(member.nome)}</span><span><strong>{member.nome}</strong><small>{member.email}</small></span></div></td>
                <td><span className={`status-pill ${isMemberAdmin ? 'violet-pill' : 'neutral'}`}>{member.perfil}</span></td>
                <td><span className={`status-pill ${member.ativo ? 'positive' : 'negative'}`}>{member.ativo ? 'Ativo' : 'Inativo'}</span></td>
                <td className="permission-summary">{isMemberAdmin ? 'Configuração, cargas, equipe e relatórios' : 'Kanban, motoristas, documentos e solicitações'}</td>
                <td><div className="team-actions">
                  <button className="button mini subtle" onClick={() => setEditing(member)}>Editar</button>
                  <button className={`button mini ${member.ativo ? 'danger' : 'success'}`} onClick={() => toggleActive(member)}>{member.ativo ? 'Desativar' : 'Reativar'}</button>
                </div></td>
              </tr>;
            })}
          </tbody></table></div>
        ) : <EmptyState title="Nenhum usuário cadastrado" detail="Adicione usuários para que a equipe possa acessar o painel." />}
      </section>
      {(showCreate || editing) && <AtendenteModal member={editing} onClose={() => { setShowCreate(false); setEditing(null); }} onSaved={reload} />}
    </>
  );
}

function AtendenteModal({ member, onClose, onSaved }) {
  const [form, setForm] = useState({ nome: member?.nome || '', email: member?.email || '', senha: '' });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      await api(member ? `/Atendentes/${member.atendenteId}` : '/Atendentes', {
        method: member ? 'PUT' : 'POST',
        body: JSON.stringify(member ? { nome: form.nome, email: form.email } : form),
      });
      onSaved();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setBusy(false);
    }
  }

  return <Modal title={member ? 'Editar usuário' : 'Adicionar atendente'} onClose={onClose}>
    <form className="stack-form" onSubmit={submit}>
      <p className="muted">{member ? 'Atualize os dados de acesso do usuário.' : 'Novos usuários são cadastrados com o perfil Atendente.'}</p>
      <label className="field"><span>Nome completo</span><input autoFocus required maxLength="150" value={form.nome} onChange={(event) => setForm({ ...form, nome: event.target.value })} /></label>
      <label className="field"><span>E-mail</span><input required type="email" maxLength="150" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label>
      {!member && <label className="field"><span>Senha inicial</span><input required type="password" minLength="10" maxLength="128" value={form.senha} onChange={(event) => setForm({ ...form, senha: event.target.value })} /><small className="field-hint">Use pelo menos 10 caracteres.</small></label>}
      {error && <Notice kind="error">{error}</Notice>}
      <div className="modal-actions"><button className="button subtle" type="button" onClick={onClose}>Cancelar</button><button className="button primary" disabled={busy}>{busy ? 'Salvando…' : 'Salvar usuário'}</button></div>
    </form>
  </Modal>;
}

function FormularioConfigPage() {
  const [fields, setFields] = useState([]);
  const [draft, setDraft] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  useEffect(() => {
    let active = true;
    api('/CamposFormulario')
      .then((result) => {
        if (!active) return;
        setFields(result);
        setDraft(result);
      })
      .catch((requestError) => { if (active) setError(requestError.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);

  function updateDraft(id, changes) {
    setDraft((current) => current.map((field) => field.campoFormularioId === id ? { ...field, ...changes } : field));
    setNotice('');
  }

  async function saveConfiguration() {
    setSaving(true);
    setError('');
    setNotice('');
    try {
      for (const field of draft) {
        const saved = fields.find((item) => item.campoFormularioId === field.campoFormularioId);
        if (!saved) continue;
        let current = saved;

        if (field.categoria === 'Documento' && current.ativo !== field.ativo) {
          current = await api(`/CamposFormulario/${field.campoFormularioId}/${field.ativo ? 'ativar' : 'desativar'}`, { method: 'PATCH' });
          const updatedField = current;
          setFields((items) => items.map((item) => item.campoFormularioId === updatedField.campoFormularioId ? updatedField : item));
        }

        const obrigatorio = current.ativo ? field.obrigatorio : false;
        if (current.rotulo !== field.rotulo || current.obrigatorio !== obrigatorio) {
          current = await api(`/CamposFormulario/${field.campoFormularioId}`, {
            method: 'PUT',
            body: JSON.stringify({ rotulo: field.rotulo, obrigatorio }),
          });
          const updatedField = current;
          setFields((items) => items.map((item) => item.campoFormularioId === updatedField.campoFormularioId ? updatedField : item));
        }
      }
      const current = await api('/CamposFormulario');
      setFields(current);
      setDraft(current);
      setNotice('Configurações salvas e aplicadas ao formulário.');
    } catch (requestError) {
      setError(`${requestError.message} As alterações concluídas foram mantidas; revise e salve novamente.`);
    } finally {
      setSaving(false);
    }
  }

  const formFields = draft.filter((field) => field.categoria === 'Campo');
  const documents = draft.filter((field) => field.categoria === 'Documento');

  function renderField(field, document = false) {
    return <div className={`config-row ${!field.ativo ? 'inactive' : ''}`} key={field.campoFormularioId}>
      <label className="config-label"><strong>{field.rotulo}</strong><small>{field.chave}</small></label>
      {document && <label className="config-toggle"><span>Obrigatório</span><input type="checkbox" checked={field.obrigatorio} disabled={!field.ativo} onChange={(event) => updateDraft(field.campoFormularioId, { obrigatorio: event.target.checked })} /><i /></label>}
      {document && <label className="config-toggle"><span>Ativo</span><input type="checkbox" checked={field.ativo} onChange={(event) => updateDraft(field.campoFormularioId, { ativo: event.target.checked, ...(event.target.checked ? {} : { obrigatorio: false }) })} /><i /></label>}
      {!document && <label className="config-toggle"><span>Obrigatório</span><input type="checkbox" checked={field.obrigatorio} onChange={(event) => updateDraft(field.campoFormularioId, { obrigatorio: event.target.checked })} /><i /></label>}
    </div>;
  }

  return <>
    <section className="page-intro">
      <div><h2>Configuração do formulário</h2><p>Defina os campos e documentos que serão solicitados aos motoristas.</p></div>
      <button className="button primary" onClick={saveConfiguration} disabled={loading || saving}>{saving ? 'Salvando…' : 'Salvar configurações'}</button>
    </section>
    {error && <Notice kind="error">{error}</Notice>}
    {notice && <Notice kind="success">{notice}</Notice>}
    {loading ? <Loading label="Carregando configurações…" /> : <div className="config-grid">
      <section className="surface config-panel">
        <header className="config-panel-heading"><div><h3>Campos do formulário</h3><p>Informações preenchidas pelo motorista.</p></div><span className="status-pill neutral">{formFields.length} campos</span></header>
        <div className="config-list">{formFields.map((field) => renderField(field))}</div>
      </section>
      <section className="surface config-panel">
        <header className="config-panel-heading"><div><h3>Documentos exigidos</h3><p>Arquivos considerados na triagem da solicitação.</p></div><span className="status-pill neutral">{documents.filter((field) => field.ativo).length} ativos</span></header>
        <div className="config-list">{documents.map((field) => renderField(field, true))}</div>
        <Notice kind="info">Um documento inativo não pode ser obrigatório. Desativá-lo também remove a obrigatoriedade.</Notice>
      </section>
    </div>}
  </>;
}

function CargasPage({ isAdmin }) {
  const [loads, setLoads] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [editingLoad, setEditingLoad] = useState(undefined);
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

  const availableCount = loads.filter((load) => load.status === 'Disponivel').length;
  const contractedCount = loads.filter((load) => load.status === 'Contratada').length;
  const restrictedCount = loads.filter((load) => load.capacidadeMinimaToneladas != null).length;

  return (
    <>
      <section className="page-intro">
        <div><h2>Cargas cadastradas</h2><p>Consulte as oportunidades de transporte e suas condições.</p></div>
        {isAdmin && <button className="button primary" onClick={() => setEditingLoad(null)}>＋ Cadastrar carga</button>}
      </section>
      <div className="load-kpis">
        <article className="surface metric-card"><span>Cargas cadastradas</span><strong>{loads.length}</strong></article>
        <article className="surface metric-card"><span>Disponíveis</span><strong>{availableCount}</strong></article>
        <article className="surface metric-card"><span>Contratadas</span><strong>{contractedCount}</strong></article>
        <article className="surface metric-card"><span>Com capacidade mínima</span><strong>{restrictedCount}</strong></article>
      </div>
      {!isAdmin && <Notice kind="info">O perfil Atendente pode consultar as cargas; cadastro e edição são exclusivos do Administrador.</Notice>}
      {error && <Notice kind="error">{error}</Notice>}
      <section className="surface table-surface load-table-surface">
        {loading ? <Loading label="Carregando cargas…" /> : loads.length ? <div className="table-scroll"><table><thead><tr><th>Carga</th><th>Origem</th><th>Destino</th><th>Capacidade mínima</th><th>Status</th>{isAdmin && <th>Ações</th>}</tr></thead><tbody>
          {loads.map((load) => <tr key={load.cargaId}>
            <td><strong>C-{String(load.cargaId).padStart(3, '0')}</strong><small className="table-subline">Carga registrada</small></td>
            <td>{load.origem}</td><td>{load.destino}</td>
            <td>{load.capacidadeMinimaToneladas == null ? 'Sem restrição' : `${load.capacidadeMinimaToneladas} t`}</td>
            <td><span className={`status-pill ${load.status === 'Disponivel' ? 'positive' : 'neutral'}`}>{load.status === 'Disponivel' ? 'Disponível' : 'Contratada'}</span></td>
            {isAdmin && <td><div className="team-actions"><button className="button mini subtle" onClick={() => setEditingLoad(load)}>Editar</button><button className="button mini subtle" onClick={() => changeStatus(load)}>{load.status === 'Disponivel' ? 'Contratar' : 'Reabrir'}</button></div></td>}
          </tr>)}
        </tbody></table></div> : <EmptyState title="Nenhuma carga cadastrada" detail="As novas cargas aparecerão nesta lista." />}
      </section>
      {editingLoad !== undefined && <CreateLoadModal load={editingLoad} onClose={() => setEditingLoad(undefined)} onCreated={() => { setEditingLoad(undefined); setRefresh((value) => value + 1); }} />}
    </>
  );
}

function CreateLoadModal({ load, onClose, onCreated }) {
  const [form, setForm] = useState({ origem: load?.origem || '', destino: load?.destino || '', capacidadeMinimaToneladas: load?.capacidadeMinimaToneladas ?? '' });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      await api(load ? `/Cargas/${load.cargaId}` : '/Cargas', {
        method: load ? 'PUT' : 'POST',
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
  return <Modal title={load ? 'Editar carga' : 'Cadastrar carga'} onClose={onClose}>
    <form className="stack-form" onSubmit={submit}>
      <label className="field"><span>Origem</span><input required maxLength="150" value={form.origem} onChange={(event) => setForm({ ...form, origem: event.target.value })} /></label>
      <label className="field"><span>Destino</span><input required maxLength="150" value={form.destino} onChange={(event) => setForm({ ...form, destino: event.target.value })} /></label>
      <label className="field"><span>Capacidade mínima (toneladas)</span><input type="number" min="0.01" max="999999.99" step="0.01" value={form.capacidadeMinimaToneladas} onChange={(event) => setForm({ ...form, capacidadeMinimaToneladas: event.target.value })} placeholder="Opcional" /></label>
      {error && <Notice kind="error">{error}</Notice>}
      <div className="modal-actions"><button className="button subtle" type="button" onClick={onClose}>Cancelar</button><button className="button primary" disabled={busy}>{busy ? 'Salvando…' : load ? 'Salvar alterações' : 'Salvar carga'}</button></div>
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
  const statusEntries = Object.entries(metrics?.porStatus || {});
  const maxStatusCount = Math.max(1, ...statusEntries.map(([, count]) => count));

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
            <section className="surface report-panel"><h3>Solicitações por status</h3>
              {statusEntries.length ? <div className="status-chart" role="img" aria-label="Quantidade de solicitações por status">{statusEntries.map(([status, count]) => (
                <div className="status-chart-item" key={status} title={`${STATUS_LABELS[status] || status}: ${count}`}>
                  <strong>{count}</strong>
                  <div className="status-chart-track"><span style={{ height: `${Math.max(5, (count / maxStatusCount) * 100)}%` }} /></div>
                  <small>{STATUS_LABELS[status] || status}</small>
                </div>
              ))}</div> : <p className="muted">Sem dados no período escolhido.</p>}
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
  const [step, setStep] = useState(0);

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
  const fieldLabel = (key, fallback) => fields.find((field) => field.chave === key)?.rotulo || fallback;
  const setValue = (key, value) => setValues((current) => ({ ...current, [key]: value }));
  const advanceStep = (event) => {
    if (event.currentTarget.form.reportValidity()) setStep((current) => Math.min(current + 1, 2));
  };

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
      throw new Error(`O arquivo ${file.name} excede o limite de 10 MB.`);
    
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
      <header className="public-form-header"><Link className="brand-lockup" to="/"><img className="brand-logo" src={cccLogo} alt="CCC — Central de Cargas e Contratação" /></Link><span className="secure-label">● Link seguro</span></header>
      <section className="public-form-card">
        {loading ? <Loading label="Validando seu link…" /> : error && !form ? <><Notice kind="error">{error}</Notice><p className="muted">Peça ao atendente um novo link de acesso caso ele tenha expirado ou já tenha sido utilizado.</p></> : submitted ? (
          <div className="form-success"><span className="success-check">✓</span><span className="eyebrow">Cadastro enviado</span><h1>Obrigado, recebemos seus dados.</h1><p>A equipe vai analisar as informações e entrar em contato para continuar o atendimento.</p></div>
        ) : form && (
          <>
            <div className="public-form-title"><span className="eyebrow">Formulário seguro · Solicitação #{form.solicitacaoId}</span><h1>Vamos atualizar seu cadastro?</h1><p>Olá{form.dados.nomeCompleto ? `, ${form.dados.nomeCompleto}` : ''}! Preencha as informações para a equipe analisar sua solicitação.</p></div>
            {error && <Notice kind="error">{error}</Notice>}
            {notice && <Notice kind="warning">{notice}</Notice>}
            <div className="form-stepper" aria-label="Etapas do formulário">
              {['Dados pessoais', 'Veículo', 'Documentos'].map((label, index) => <div className={`form-step ${step === index ? 'current' : step > index ? 'complete' : ''}`} key={label}><span>{step > index ? '✓' : index + 1}</span>{label}</div>)}
            </div>
            <form className="public-form" onSubmit={submit}>
              {step === 0 && <section className="public-section">
                <div className="section-title"><span>1</span><div><h2>Dados pessoais</h2><p>Confira seus dados de contato.</p></div></div>
                <div className="public-field-grid">
                  {fields.some((field) => field.chave === 'nome') && <label className="field"><span>{fieldLabel('nome', 'Nome completo')}{required('nome') ? ' *' : ''}</span><input required={required('nome')} maxLength="150" value={values.nomeCompleto} onChange={(event) => setValue('nomeCompleto', event.target.value)} /></label>}
                  {fields.some((field) => field.chave === 'cpf') && <label className="field"><span>{fieldLabel('cpf', 'CPF')}{required('cpf') ? ' *' : ''}</span><input required={required('cpf')} maxLength="14" value={values.cpf} onChange={(event) => setValue('cpf', event.target.value)} placeholder="000.000.000-00" /></label>}
                  {fields.some((field) => field.chave === 'telefone') && <label className="field"><span>{fieldLabel('telefone', 'Telefone para contato')}</span><input value={formatPhone(form.dados.telefone)} readOnly /></label>}
                  {fields.some((field) => field.chave === 'email') && <label className="field"><span>{fieldLabel('email', 'E-mail')}{required('email') ? ' *' : ''}</span><input type="email" required={required('email')} maxLength="254" value={values.email} onChange={(event) => setValue('email', event.target.value)} /></label>}
                </div>
              </section>}
              {step === 1 && <section className="public-section">
                <div className="section-title"><span>2</span><div><h2>Dados do veículo</h2><p>Informe o veículo utilizado na solicitação.</p></div></div>
                <div className="public-field-grid">
                  {fields.some((field) => field.chave === 'placa') && form.dados.veiculos.length > 0 && <label className="field"><span>Veículo já cadastrado</span><select value={values.veiculoId} onChange={(event) => { setValue('veiculoId', event.target.value); setValue('placa', ''); }}><option value="">Cadastrar outro veículo</option>{form.dados.veiculos.map((vehicle) => <option value={vehicle.veiculoId} key={vehicle.veiculoId}>{vehicle.placa} · {vehicle.capacidadeToneladas} t</option>)}</select></label>}
                  {fields.some((field) => field.chave === 'placa') && !values.veiculoId && <label className="field"><span>{fieldLabel('placa', 'Placa do veículo')}{required('placa') ? ' *' : ''}</span><input required={required('placa')} maxLength="8" value={values.placa} onChange={(event) => setValue('placa', event.target.value.toUpperCase())} placeholder="ABC1D23" /></label>}
                  {fields.some((field) => field.chave === 'capacidade') && !values.veiculoId && <label className="field"><span>{fieldLabel('capacidade', 'Capacidade em toneladas')}{required('capacidade') ? ' *' : ''}</span><input required={required('capacidade')} type="number" min="0.01" max="200" step="0.01" value={values.capacidadeToneladas} onChange={(event) => setValue('capacidadeToneladas', event.target.value)} placeholder="Ex.: 14" /></label>}
                </div>
              </section>}
              {step === 2 && <section className="public-section">
                <div className="section-title"><span>3</span><div><h2>Documentos</h2><p>Envie os documentos necessários para análise.</p></div></div>
                <div className="upload-list">
                  {form.documentos.map((doc) => <label className="upload-row" key={doc.chave}>
                    <span className={`upload-state ${doc.estado === 'Validado' ? 'uploaded' : ''}`}>{doc.estado === 'Validado' ? '✓' : '↑'}</span>
                    <span className="upload-copy"><strong>{doc.rotulo}{doc.obrigatorio && <em>Obrigatório</em>}</strong><small>{files[doc.chave]?.name || doc.nomeOriginal || uploadStatus(doc.estado)}</small></span>
                    <input type="file" accept=".pdf,image/jpeg,image/png" onChange={(event) => setFiles((current) => ({ ...current, [doc.chave]: event.target.files?.[0] || null }))} />
                    <span className="button subtle small">{files[doc.chave]?.name || 'Escolher arquivo'}</span>
                  </label>)}
                  {!form.documentos.length && <p className="muted">Não há documentos ativos configurados.</p>}
                </div>
              </section>}
              <div className="public-submit">
                <p>🔒 Seus dados são usados somente para a análise desta solicitação.</p>
                <div className="form-step-actions">
                  {step > 0 && <button type="button" className="button subtle" onClick={() => setStep((current) => current - 1)}>← Voltar</button>}
                  {step < 2 ? <button type="button" className="button primary" onClick={advanceStep}>Continuar →</button> : <button className="button primary" disabled={saving}>{saving ? 'Enviando seus dados…' : 'Enviar cadastro →'}</button>}
                </div>
              </div>
            </form>
          </>
        )}
      </section>
      <footer className="public-footer">CCC · Central de Carga e Contratação</footer>
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
