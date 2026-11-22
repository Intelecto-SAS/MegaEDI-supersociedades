import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { Official } from '@/lib/types'
import { useAuth } from '@/lib/auth'
import { useNavigate } from 'react-router-dom'
import { useMemo, useState } from 'react'

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
    <div className="min-h-screen bg-navy text-white">
      <div className="max-w-6xl mx-auto px-6 py-12">
        <p className="text-[11px] uppercase tracking-[0.22em] text-gold">Demostración institucional</p>
        <h1 className="serif text-5xl mt-3">EDI 360</h1>
        <p className="mt-2 text-white/70 max-w-2xl">
          Ecosistema Digital Integrado de la Superintendencia de Sociedades. Entre con el perfil del funcionario ficticio que va a presentar. Los datos y las personas son de demostración.
        </p>
        {err && <p className="mt-4 text-amber-200">{err}</p>}
        <div className="mt-10 space-y-8">
          {groups.map(([dept, people]) => (
            <section key={dept}>
              <h2 className="text-xs uppercase tracking-[0.16em] text-white/40 mb-3">{dept}</h2>
              <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
                {people.map((o) => (
                  <button
                    key={o.id}
                    onClick={() => enter(o)}
                    disabled={busy === o.id}
                    className="text-left rounded-xl border border-white/10 bg-white/5 hover:bg-white/10 p-4 transition"
                  >
                    <p className="font-medium">{o.displayName}</p>
                    <p className="text-sm text-white/60">{o.position}</p>
                    <p className="text-[11px] text-gold mt-2">{busy === o.id ? 'Entrando…' : 'Entrar a su bandeja'}</p>
                  </button>
                ))}
              </div>
            </section>
          ))}
        </div>
        <p className="mt-12 text-xs text-white/40">Contraseña de demostración: demo · Sin conexión a sistemas reales ni a Azure.</p>
      </div>
    </div>
  )
}
