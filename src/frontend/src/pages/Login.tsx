import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { Official } from '@/lib/types'
import { useAuth } from '@/lib/auth'
import { useNavigate } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { BrandMark, FlagStripe } from '@/components/ui'

export function LoginPage() {
  const { login } = useAuth()
  const nav = useNavigate()
  const [busy, setBusy] = useState<string | null>(null)
  const [err, setErr] = useState('')
  const { data } = useQuery({
    queryKey: ['oficiales'],
    queryFn: () => api.get<Official[]>('/api/auth/oficiales'),
  })

  const groups = useMemo(() => {
    const map = new Map<string, Official[]>()
    for (const o of data ?? []) {
      const key = o.department
      map.set(key, [...(map.get(key) ?? []), o])
    }
    return [...map.entries()]
  }, [data])

  async function enter(o: Official) {
    setBusy(o.id)
    setErr('')
    try {
      await login(o.userName)
      nav('/')
    } catch (e: any) {
      setErr(e.message)
    } finally {
      setBusy(null)
    }
  }

  return (
    <div className="min-h-screen bg-white text-ink flex flex-col">
      <FlagStripe />
      <div className="max-w-6xl mx-auto px-6 py-10 flex-1 w-full">
        <div className="flex flex-col lg:flex-row lg:items-end gap-8 mb-10 pb-8 border-b border-line">
          <BrandMark />
          <div className="lg:pb-2">
            <p className="text-[11px] uppercase tracking-[0.22em] text-brand">Demostración institucional</p>
            <h1 className="serif text-4xl sm:text-5xl mt-1 text-navy">EDI 360</h1>
            <p className="mt-2 text-muted max-w-xl text-sm sm:text-base">
              Ecosistema Digital Integrado. Entre con el perfil del funcionario ficticio que va a presentar. Los datos y las personas son de demostración.
            </p>
          </div>
        </div>
        {err && <p className="mb-4 text-brand">{err}</p>}
        <div className="space-y-8">
          {groups.map(([dept, people]) => (
            <section key={dept}>
              <h2 className="text-xs uppercase tracking-[0.16em] text-muted mb-3">{dept}</h2>
              <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
                {people.map((o) => (
                  <button
                    key={o.id}
                    onClick={() => enter(o)}
                    disabled={busy === o.id}
                    className="text-left rounded-xl border border-line bg-paper hover:border-brand hover:bg-sand p-4 transition"
                  >
                    <p className="font-semibold text-navy">{o.displayName}</p>
                    <p className="text-sm text-muted">{o.position}</p>
                    <p className="text-[11px] text-brand mt-2 font-semibold">{busy === o.id ? 'Entrando…' : 'Entrar a su bandeja'}</p>
                  </button>
                ))}
              </div>
            </section>
          ))}
        </div>
        <p className="mt-12 text-xs text-muted">Contraseña de demostración: demo · Sin conexión a sistemas reales ni a Azure.</p>
      </div>
      <div>
        <div className="h-0.5 bg-brand" aria-hidden />
        <FlagStripe className="h-1" />
      </div>
    </div>
  )
}
