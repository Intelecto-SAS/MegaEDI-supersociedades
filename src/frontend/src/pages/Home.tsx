import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '@/lib/api'
import type { Home } from '@/lib/types'
import { Badge, Card, PageHeader, Table } from '@/components/ui'
import { useAuth } from '@/lib/auth'

export function HomePage() {
  const { session } = useAuth()
  const { data } = useQuery({ queryKey: ['inicio'], queryFn: () => api.get<Home>('/api/inicio') })
  if (!data) return <p className="text-muted">Cargando el entorno de trabajo…</p>
  return (
    <div>
      <PageHeader kicker="Inicio" title={data.greeting}>
        {session?.homeExperience === 'alta-direccion'
          ? 'Cifras tomadas de los registros actuales. Cada indicador abre su detalle.'
          : 'Pendientes, plazos y casos de su ámbito.'}
      </PageHeader>
      <div className="grid md:grid-cols-2 xl:grid-cols-4 gap-3 mb-6">
        {data.kpis.map((k) => (
          <Link key={k.id} to={k.href || '#'}>
            <Card className="hover:border-brand/40">
              <p className="text-[11px] uppercase tracking-wider text-muted">{k.label}</p>
              <p className="serif text-3xl text-navy mt-1">{k.value}</p>
              <p className="text-xs text-muted mt-1">{k.hint}</p>
            </Card>
          </Link>
        ))}
      </div>
      <div className="grid lg:grid-cols-3 gap-4">
        <Card className="lg:col-span-2">
          <h2 className="serif text-xl text-navy mb-3">Pendientes</h2>
          <Table headers={['Asunto', 'Estado', 'Prioridad', 'Plazo']}>
            {data.pending.map((p) => (
              <tr key={p.id} className="hover:bg-sand/40">
                <td className="px-3 py-2">{p.title}</td>
                <td className="px-3 py-2"><Badge>{p.status}</Badge></td>
                <td className="px-3 py-2">{p.priority}</td>
                <td className="px-3 py-2 text-muted">{p.dueAt ? new Date(p.dueAt).toLocaleDateString('es-CO') : '—'}</td>
              </tr>
            ))}
          </Table>
        </Card>
        <Card>
          <h2 className="serif text-xl text-navy mb-3">Acciones rápidas</h2>
          <div className="flex flex-col gap-2">
            {data.actions.map((a) => (
              <Link key={a.href} to={a.href} className="rounded-md border border-line px-3 py-2 hover:bg-sand text-sm">{a.label}</Link>
            ))}
          </div>
          <h3 className="mt-6 text-[11px] uppercase tracking-wider text-muted">Actividad reciente</h3>
          <ul className="mt-2 space-y-2">
            {data.activity.map((a, i) => (
              <li key={i} className="text-sm">
                <span className="font-medium">{a.title}</span>
                <span className="text-muted"> · {a.detail}</span>
              </li>
            ))}
          </ul>
        </Card>
      </div>
    </div>
  )
}

export function WorkPage() {
  const { data } = useQuery({ queryKey: ['inicio'], queryFn: () => api.get<Home>('/api/inicio') })
  if (!data) return null
  return (
    <div>
      <PageHeader kicker="Mi trabajo" title="Bandeja personal" />
      <div className="grid lg:grid-cols-2 gap-4">
        <Card>
          <h2 className="serif text-lg mb-2">Casos</h2>
          {data.cases.map((c) => (
            <Link key={c.id} to={`/justicia/expedientes/${c.id}`} className="block py-2 border-b border-line text-sm">
              <span className="text-navy font-medium">{c.number}</span> · {c.title}
            </Link>
          ))}
        </Card>
        <Card>
          <h2 className="serif text-lg mb-2">Próximos vencimientos</h2>
          {data.deadlines.map((c) => (
            <p key={c.id} className="py-2 border-b border-line text-sm">{c.title} · {c.dueAt ? new Date(c.dueAt).toLocaleDateString('es-CO') : ''}</p>
          ))}
        </Card>
      </div>
    </div>
  )
}
