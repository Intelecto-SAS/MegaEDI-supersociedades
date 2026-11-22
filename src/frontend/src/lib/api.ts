export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export function isLocalHost() {
  if (typeof window === 'undefined') return false
  const host = window.location.hostname
  return host === '127.0.0.1' || host === 'localhost'
}

export function connectionErrorMessage() {
  if (isLocalHost()) {
    return 'No hay conexión con el servidor. Debe estar en marcha en http://127.0.0.1:5080'
  }
  return 'No hay conexión con el servidor. Abra /api/salud en esta misma dirección: si no responde, el servicio backend no se desplegó.'
}

async function parse(res: Response) {
  const text = await res.text()
  const data = text ? JSON.parse(text) : null
  if (!res.ok) {
    throw new ApiError(res.status, data?.mensaje || data?.message || 'No se pudo completar la operación.')
  }
  return data
}

async function request(url: string, init?: RequestInit) {
  try {
    const res = await fetch(url, { credentials: 'include', ...init })
    return await parse(res)
  } catch (e) {
    if (e instanceof ApiError) throw e
    throw new ApiError(0, connectionErrorMessage())
  }
}

export const api = {
  get: <T>(url: string) => request(url) as Promise<T>,
  post: <T>(url: string, body?: unknown) =>
    request(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: body ? JSON.stringify(body) : undefined,
    }) as Promise<T>,
  blob: async (url: string) => {
    try {
      const res = await fetch(url, { credentials: 'include' })
      if (!res.ok) throw new ApiError(res.status, 'No se pudo descargar el archivo.')
      return res.blob()
    } catch (e) {
      if (e instanceof ApiError) throw e
      throw new ApiError(0, connectionErrorMessage())
    }
  },
}
