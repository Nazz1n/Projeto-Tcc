const API_BASE_URL = import.meta.env.VITE_API_URL ?? '';
const TOKEN_KEY = 'fretebot.token';

function connectionError(path) {
  const endpoint = API_BASE_URL
    ? `${API_BASE_URL}/api${path}`
    : `${window.location.origin}/api${path} (proxy local para https://localhost:7033)`;
  return new Error(`API indisponível em ${endpoint}. Confirme se o backend está em execução e se a URL configurada está correta.`);
}

export function getToken() {
  return sessionStorage.getItem(TOKEN_KEY);
}

export function saveToken(token) {
  sessionStorage.setItem(TOKEN_KEY, token);
}

export function clearToken() {
  sessionStorage.removeItem(TOKEN_KEY);
}
function handleSessionExpired() {
  clearToken();
  sessionStorage.removeItem('fretebot.user');
  window.location.assign('/login');
}

export async function api(path, options = {}) {
  const headers = new Headers(options.headers || {});
  const token = getToken();

  if (token) headers.set('Authorization', `Bearer ${token}`);

  const isFormData = options.body instanceof FormData;
  if (options.body && !isFormData && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  let response;
  try {
    response = await fetch(`${API_BASE_URL}/api${path}`, {
      ...options,
      headers,
    });
  } catch {
    throw connectionError(path);
  }
    if (response.status === 401 && token) {
    handleSessionExpired();
    throw new Error('Sua sessão expirou. Entre novamente.');
  }

  if (response.status === 204) return null;

  const contentType = response.headers.get('content-type') || '';
  const result = contentType.includes('application/json')
    ? await response.json()
    : await response.text();
    if (response.status === 401 && token) {
    handleSessionExpired();
    throw new Error('Sua sessão expirou. Entre novamente.');
  }

  if (!response.ok) {
    if (response.status === 500 && typeof result === 'string' && !result.trim()) {
      throw connectionError(path);
    }

    const message = (typeof result === 'string' && result.trim())
      || result?.mensagem
      || result?.title
      || `Falha na requisição (${response.status}).`;
    throw new Error(message);
  }

  return result;
}

export async function download(path, filename) {
  const headers = new Headers();
  const token = getToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let response;
  try {
    response = await fetch(`${API_BASE_URL}/api${path}`, { headers });
  } catch {
    throw connectionError(path);
  }

  if (!response.ok) {
    const result = await response.json().catch(() => null);
    if (response.status === 500 && !result) throw connectionError(path);
    throw new Error(result?.mensagem || `Falha ao baixar arquivo (${response.status}).`);
  }

  const downloadName = getDownloadFilename(response.headers.get('content-disposition')) || filename;
  saveBlob(await response.blob(), downloadName);
}

function getDownloadFilename(contentDisposition) {
  if (!contentDisposition) return null;

  const encodedName = contentDisposition.match(/filename\*\s*=\s*UTF-8''([^;]+)/i)?.[1];
  if (encodedName) {
    try {
      return decodeURIComponent(encodedName.trim().replace(/^"|"$/g, ''));
    } catch {
      return null;
    }
  }

  const plainName = contentDisposition.match(/filename\s*=\s*(?:"([^"]+)"|([^;]+))/i);
  return (plainName?.[1] || plainName?.[2])?.trim() || null;
}

function saveBlob(blob, filename) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}
