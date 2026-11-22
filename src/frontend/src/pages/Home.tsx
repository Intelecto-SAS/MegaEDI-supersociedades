import type { ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '@/lib/api'
import type { ChartSlice, Home } from '@/lib/types'
import { Badge, Card, PageHeader, Table } from '@/components/ui'
import { useAuth } from '@/lib/auth'
import { Bar, BarChart, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'

const WINE = ['#6b1d34', '#962d46', '#c45c74', '#d4a0ae', '#3d1020', '#7a2438']
const PRIORITY: Record<string, string> = { Crítica: '#6b1d34', Alta: '#962d46', Media: '#c45c74', Baja: '#d4a0ae' }
const HORIZON: Record<string, string> = { Vencidas: '#9a4b1f', '7 días': '#962d46', '30 días': '#c45c74', Después: '#6b8f71', 'Sin plazo': '#a39aa0' }

function ChartCard({ title, hint, children }: { title: string; hint: string; children: ReactNode }) {
  return (
    <Card className="h-72 flex flex-col">
      <h2 className="serif text-lg text-navy">{title}</h2>
      <p className="text-xs text-muted mb-2">{hint}</p>
      <div className="flex-1 min-h-0">{children}</div>
    </Card>
  )
}

function slices(rows: ChartSlice[] | undefined) {
  return (rows ?? []).filter((r) => r.value > 0)
}

export function HomePage() {
  const { session } = useAuth()
  const { data } = useQuery({ queryKey: ['inicio'], queryFn: () => api.get<Home>('/api/inicio') })
  if (!data) return <p className="text-muted">Cargando el entorno de trabajo…</p>
  const byType = slices(data.byType)
  const byPriority = slices(data.byPriority)
  const horizon = slices(data.deadlineHorizon)
  const totalCases = byType.reduce((n, r) => n + r.value, 0)
  return (
    <div>
      <PageHeader kicker="Inicio" title={data.greeting}>
        {session?.homeExperience === 'alta-direccion'
          ? 'Cifras de los registros actuales: dónde está la carga, qué es crítico y qué se vence.'
          : 'Carga de su ámbito: composición de procesos, concentración de riesgo y presión de plazos.'}
      </PageHeader>
      <div className="grid md:grid-cols-2 xl:grid-cols-4 gap-3 mb-6">
        {data.kpis.map((k) => (
          <Link key={k.id} to={k.href || '#'}>
            <Card className="hover:border-brand/40 relative overflow-hidden">
              <span className="absolute inset-y-0 left-0 w-1 bg-wine" />
              <p className="text-[11px] uppercase tracking-wider text-muted">{k.label}</p>
              <p className="serif text-3xl text-navy mt-1">{k.value}</p>
              <p className="text-xs text-muted mt-1">{k.hint}</p>
            </Card>
          </Link>
        ))}
      </div>
      <div className="grid lg:grid-cols-3 gap-4 mb-6">
        <ChartCard title="Procesos por tipo" hint={`${totalCases} expedientes en el ámbito visible`}>
          {byType.length === 0 ? <p className="text-sm text-muted">Sin procesos en el ámbito.</p> : (
            <div className="h-full flex items-center gap-2">
              <div className="flex-1 h-full min-w-0">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={byType} dataKey="value" nameKey="name" innerRadius={46} outerRadius={70} paddingAngle={2}>
                      {byType.map((s, i) => <Cell key={s.name} fill={WINE[i % WINE.length]} />)}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </div>
              <ul className="text-xs space-y-1.5 shrink-0 max-w-[46%]">
                {byType.map((s, i) => (
                  <li key={s.name} className="flex items-center gap-2">
                    <span className="h-2 w-2 rounded-full shrink-0" style={{ background: WINE[i % WINE.length] }} />
                    <span className="text-ink">{s.name}</span>
                    <span className="text-muted tabular-nums">{s.value}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </ChartCard>
        <ChartCard title="Concentración de prioridad" hint="Cuántos procesos exigen atención inmediata">
          {byPriority.length === 0 ? <p className="text-sm text-muted">Sin prioridades registradas.</p> : (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={byPriority} layout="vertical" margin={{ left: 8, right: 8 }}>
                <XAxis type="number" allowDecimals={false} tick={{ fontSize: 11 }} />
                <YAxis type="category" dataKey="name" width={72} tick={{ fontSize: 12 }} />
                <Tooltip />
                <Bar dataKey="value" radius={[0, 4, 4, 0]}>
                  {byPriority.map((s) => <Cell key={s.name} fill={PRIORITY[s.name] || '#962d46'} />)}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          )}
        </ChartCard>
        <ChartCard title="Presión de plazos" hint="Pendientes abiertos según fecha de vencimiento">
          {horizon.length === 0 ? <p className="text-sm text-muted">Sin pendientes abiertos.</p> : (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={horizon}>
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} width={28} />
                <Tooltip />
                <Bar dataKey="value" radius={[4, 4, 0, 0]}>
                  {horizon.map((s) => <Cell key={s.name} fill={HORIZON[s.name] || '#962d46'} />)}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          )}
        </ChartCard>
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
