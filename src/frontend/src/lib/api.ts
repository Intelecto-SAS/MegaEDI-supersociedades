export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function parse(res: Response) {
  const text = await res.text()
  const data = text ? JSON.parse(text) : null
  if (!res.ok) {
    throw new ApiError(res.status, data?.mensaje || data?.message || 'No se pudo completar la operación.')
  }
  return data
}

export const api = {
  get: <T>(url: string) => fetch(url, { credentials: 'include' }).then(parse) as Promise<T>,
  post: <T>(url: string, body?: unknown) =>
    fetch(url, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: body ? JSON.stringify(body) : undefined,
    }).then(parse) as Promise<T>,
  blob: async (url: string) => {
    const res = await fetch(url, { credentials: 'include' })
    if (!res.ok) throw new ApiError(res.status, 'No se pudo descargar el archivo.')
    return res.blob()
  },
}
