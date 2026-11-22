import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { useState } from 'react'
import { api } from '@/lib/api'
import { Badge, Button, Card, Field, Input, PageHeader, Table } from '@/components/ui'

export function SocietiesPage() {
  const [q, setQ] = useState('')
  const { data } = useQuery({ queryKey: ['soc', q], queryFn: () => api.get<any>(`/api/sociedades?q=${encodeURIComponent(q)}&size=30`) })
  return (
    <div>
      <PageHeader kicker="Supervisión" title="Sociedades">
        Una sola ficha por sociedad. Los demás módulos consultan este maestro.
      </PageHeader>
      <Input className="max-w-md mb-4" placeholder="Nombre o NIT de demostración" value={q} onChange={(e) => setQ(e.target.value)} />
      <Table headers={['NIT', 'Nombre', 'Estado', 'Situación', 'Tamaño']}>
        {(data?.items ?? []).map((s: any) => (
          <tr key={s.id}>
            <td className="px-3 py-2"><Link className="text-navy font-medium" to={`/supervision/sociedades/${s.id}`}>{s.nit}</Link></td>
            <td className="px-3 py-2">{s.name}</td>
            <td className="px-3 py-2">{s.status}</td>
            <td className="px-3 py-2">{s.situation}</td>
            <td className="px-3 py-2">{s.size}</td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export function SocietyDetailPage() {
  const { id } = useParams()
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['soc-d', id], queryFn: () => api.get<any>(`/api/sociedades/${id}`) })
  if (!data) return null
  return (
    <div>
      <PageHeader kicker="Ficha 360" title={data.name} actions={<Button variant="ghost" onClick={() => api.post(`/api/sociedades/${id}/alertas`).then(() => qc.invalidateQueries())}>Ejecutar reglas de alerta</Button>}>
        {data.nit} · {data.legalForm} · Origen: {data.dataOrigin}
      </PageHeader>
      <div className="grid lg:grid-cols-3 gap-4">
        <Card>
          <p className="text-muted text-xs uppercase">Situación</p>
          <p className="serif text-2xl text-navy">{data.situation}</p>
          <p className="text-sm mt-2">{data.status} · {data.size} · {data.region}</p>
          <p className="text-xs text-muted mt-3">{data.notes}</p>
        </Card>
        <Card>
          <h3 className="serif text-lg mb-2">Representantes</h3>
          {data.parties?.map((p: any, i: number) => <p key={i} className="text-sm">{p.name} · {p.role}</p>)}
        </Card>
        <Card>
          <h3 className="serif text-lg mb-2">Alertas</h3>
          {data.alerts?.map((a: any) => <p key={a.id} className="text-sm mb-1"><Badge tone={a.level === 'Alta' ? 'warn' : 'navy'}>{a.rule}</Badge> {a.trigger}</p>)}
        </Card>
      </div>
      <div className="grid lg:grid-cols-2 gap-4 mt-4">
        <Card>
          <h3 className="serif text-lg mb-2">Cifras financieras (misma fuente D5)</h3>
          <Table headers={['Periodo', 'Activos', 'Pasivos', 'Patrimonio', 'Resultado']}>
            {data.financials?.map((f: any) => (
              <tr key={f.id}>
                <td className="px-3 py-2">{f.period}</td>
                <td className="px-3 py-2">{f.assets.toLocaleString('es-CO')}</td>
                <td className="px-3 py-2">{f.liabilities.toLocaleString('es-CO')}</td>
                <td className="px-3 py-2">{f.equity.toLocaleString('es-CO')}</td>
                <td className="px-3 py-2">{f.result.toLocaleString('es-CO')}</td>
              </tr>
            ))}
          </Table>
        </Card>
        <Card>
          <h3 className="serif text-lg mb-2">Procesos</h3>
          {data.cases?.map((c: any) => (
            <Link key={c.id} to={`/justicia/expedientes/${c.id}`} className="block text-sm py-1">{c.number} · {c.stage}</Link>
          ))}
        </Card>
      </div>
    </div>
  )
}

export function AlertsPage() {
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['alertas'], queryFn: () => api.get<any>('/api/alertas?size=40') })
  return (
    <div>
      <PageHeader kicker="Riesgo" title="Alertas">
        El motor de reglas señala; una persona revisa. Ninguna alerta impone sanción automática.
      </PageHeader>
      <Table headers={['Regla', 'Sociedad', 'Nivel', 'Estado', 'Señal']}>
        {(data?.items ?? []).map((a: any) => (
          <tr key={a.id}>
            <td className="px-3 py-2">{a.rule}</td>
            <td className="px-3 py-2">{a.society}</td>
            <td className="px-3 py-2"><Badge tone={a.level === 'Alta' ? 'warn' : 'navy'}>{a.level}</Badge></td>
            <td className="px-3 py-2">{a.status}</td>
            <td className="px-3 py-2">
              <span className="text-muted">{a.trigger}</span>
              {a.status !== 'Revisada' && (
                <Button variant="plain" className="ml-2" onClick={() => api.post(`/api/alertas/${a.id}/revision`, { actionTaken: 'Revisión de supervisión', result: 'Se mantiene seguimiento' }).then(() => qc.invalidateQueries({ queryKey: ['alertas'] }))}>Registrar revisión</Button>
              )}
            </td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export function XbrlPage() {
  const [form, setForm] = useState({ societyId: '', period: '2026', assets: '1000', liabilities: '400', equity: '600', income: '200', result: '40' })
  const mut = useMutation({
    mutationFn: () => api.post('/api/xbrl', { ...form, assets: +form.assets, liabilities: +form.liabilities, equity: +form.equity, income: +form.income, result: +form.result, societyId: form.societyId }),
  })
  return (
    <div className="max-w-xl">
      <PageHeader kicker="XBRL propio" title="Cargue de reporte financiero">
        Validador demostrativo: la ecuación patrimonial debe cuadrar. No es un validador XBRL oficial.
      </PageHeader>
      <Card className="space-y-3">
        <Field label="Identificador de sociedad (demo)"><Input value={form.societyId} onChange={(e) => setForm({ ...form, societyId: e.target.value })} placeholder="GUID de la sociedad" /></Field>
        {(['period', 'assets', 'liabilities', 'equity', 'income', 'result'] as const).map((k) => (
          <Field key={k} label={k}><Input value={(form as any)[k]} onChange={(e) => setForm({ ...form, [k]: e.target.value })} /></Field>
        ))}
        <Button onClick={() => mut.mutate()}>Validar y cargar</Button>
        {mut.isSuccess && <p className="text-ok text-sm">Cargado como borrador. Un analista debe aprobarlo.</p>}
        {mut.isError && <p className="text-warn text-sm">{(mut.error as Error).message}</p>}
      </Card>
    </div>
  )
}
