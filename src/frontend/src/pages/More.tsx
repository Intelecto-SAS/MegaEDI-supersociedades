import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '@/lib/api'
import { AssistBanner, Badge, Button, Card, PageHeader, Table } from '@/components/ui'
import { Bar, BarChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'

export function ReportsPage() {
  const { data } = useQuery({ queryKey: ['inf'], queryFn: () => api.get<any[]>('/api/informes') })
  return (
    <div>
      <PageHeader kicker="E7 · propuesta del informe" title="Informes especializados">
        Nueve informes del catálogo. El motor local arma el borrador; una persona revisa y firma. Módulo pendiente de validación institucional.
      </PageHeader>
      <Table headers={['N.°', 'Informe', 'Destinatario', 'Estado']}>
        {(data ?? []).map((r) => (
          <tr key={r.id}>
            <td className="px-3 py-2">{r.catalogNumber}</td>
            <td className="px-3 py-2"><Link className="text-navy font-medium" to={`/informes/${r.id}`}>{r.type}</Link></td>
            <td className="px-3 py-2">{r.audience}</td>
            <td className="px-3 py-2"><Badge>{r.status}</Badge></td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export function ReportDetailPage({ id }: { id: string }) {
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['inf', id], queryFn: () => api.get<any>(`/api/informes/${id}`) })
  if (!data) return null
  return (
    <div className="max-w-3xl">
      <PageHeader kicker={`Informe ${data.catalogNumber}`} title={data.type} />
      <AssistBanner />
      <Card className="mt-4 space-y-3">
        <p className="text-sm text-muted">{data.audience} · {data.period} · {data.status}</p>
        <pre className="whitespace-pre-wrap text-sm">{data.draft || 'Aún no hay borrador.'}</pre>
        {data.missingRequired && <p className="text-warn text-sm">Falta información obligatoria: {data.missingRequired}</p>}
        <p className="text-xs text-muted">Fuentes: {data.sources}</p>
        <div className="flex flex-wrap gap-2">
          <Button onClick={() => api.post(`/api/informes/${id}/borrador`).then(() => qc.invalidateQueries({ queryKey: ['inf', id] }))}>Generar borrador</Button>
          <Button variant="ghost" onClick={() => api.post(`/api/informes/${id}/revision`, { comment: 'Revisión humana', draft: data.draft + '\n\nRevisado por el responsable.' }).then(() => qc.invalidateQueries({ queryKey: ['inf', id] }))}>Registrar revisión</Button>
          <Button variant="gold" onClick={() => api.post(`/api/informes/${id}/aprobar`).then(() => qc.invalidateQueries({ queryKey: ['inf', id] }))}>Aprobar y firmar</Button>
          <Button variant="ghost" onClick={async () => {
            const blob = await api.blob(`/api/informes/${id}/pdf`)
            const url = URL.createObjectURL(blob)
            const a = document.createElement('a'); a.href = url; a.download = 'informe-edi360.pdf'; a.click()
          }}>Descargar PDF</Button>
        </div>
      </Card>
    </div>
  )
}

export function DashboardPage() {
  const { data } = useQuery({ queryKey: ['tab'], queryFn: () => api.get<any>('/api/tableros') })
  if (!data) return null
  return (
    <div>
      <PageHeader kicker="Tablero" title="Vista institucional">
        Indicadores calculados desde la base. Pulse una tarjeta para ir a los registros.
      </PageHeader>
      <div className="grid md:grid-cols-4 gap-3 mb-6">
        {data.kpis.map((k: any) => (
          <Link key={k.id} to={k.href || '#'}><Card><p className="text-xs text-muted uppercase">{k.label}</p><p className="serif text-3xl text-navy">{k.value}</p></Card></Link>
        ))}
      </div>
      <Card className="h-72">
        <h3 className="text-sm text-muted mb-2">Procesos por tipo</h3>
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data.byType}>
            <XAxis dataKey="name" tick={{ fontSize: 11 }} />
            <YAxis allowDecimals={false} />
            <Tooltip />
            <Bar dataKey="value" fill="#0B1F33" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </Card>
    </div>
  )
}

function SimpleList({ path, title, kicker, columns, rows }: { path: string; title: string; kicker: string; columns: string[]; rows: (r: any) => string[] }) {
  const { data } = useQuery({ queryKey: [path], queryFn: () => api.get<any>(path) })
  const list = Array.isArray(data) ? data : data?.incidencias || data?.parametros || data?.bySector || []
  return (
    <div>
      <PageHeader kicker={kicker} title={title} />
      <Table headers={columns}>
        {(Array.isArray(data) ? data : list).slice?.(0, 40).map((r: any, i: number) => (
          <tr key={r.id || i}>{rows(r).map((c, j) => <td key={j} className="px-3 py-2 text-sm">{c}</td>)}</tr>
        ))}
      </Table>
    </div>
  )
}

export function ThirdPartiesPage() {
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['tp'], queryFn: () => api.get<any>('/api/terceros?size=40') })
  return (
    <div>
      <PageHeader kicker="D2" title="Registro único de terceros">La fusión es humana. No se unen personas solo por nombres parecidos.</PageHeader>
      <Table headers={['Nombre', 'Documento', 'Duplicado']}>
        {(data?.items ?? []).map((t: any) => (
          <tr key={t.id}>
            <td className="px-3 py-2">{t.fullName}</td>
            <td className="px-3 py-2">{t.documentType} {t.documentNumber}</td>
            <td className="px-3 py-2">{t.possibleDuplicate ? <Badge tone="warn">Revisar</Badge> : '—'}</td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export function AuxPage() {
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['aux'], queryFn: () => api.get<any[]>('/api/auxiliares') })
  return (
    <div>
      <PageHeader kicker="J4" title="Auxiliares de la justicia" />
      <Table headers={['Nombre', 'Profesión', 'Tarjeta', 'Estado', 'Acciones']}>
        {(data ?? []).map((a) => (
          <tr key={a.id}>
            <td className="px-3 py-2">{a.name}</td>
            <td className="px-3 py-2">{a.profession}</td>
            <td className="px-3 py-2">{a.card}</td>
            <td className="px-3 py-2"><Badge tone={a.status === 'Bloqueado' ? 'warn' : 'ok'}>{a.status}</Badge></td>
            <td className="px-3 py-2">
              <Button variant="plain" onClick={() => api.post(`/api/auxiliares/${a.id}/tarjeta`, { card: 'TP-DEMO-CORREGIDA' }).then(() => qc.invalidateQueries({ queryKey: ['aux'] }))}>Corregir</Button>
              <Button variant="plain" onClick={() => api.post(`/api/auxiliares/${a.id}/validar`).then(() => qc.invalidateQueries({ queryKey: ['aux'] }))}>Validar</Button>
            </td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export function ContractsPage() {
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['ctr'], queryFn: () => api.get<any[]>('/api/contratos') })
  return (
    <div>
      <PageHeader kicker="E6 · propuesta" title="Gestión contractual">Ningún acta sin póliza verificada. Ningún pago sin requisitos. Pendiente de validación institucional.</PageHeader>
      <Table headers={['Contrato', 'Objeto', 'Estado', 'Póliza', 'Acciones']}>
        {(data ?? []).map((c) => (
          <tr key={c.id}>
            <td className="px-3 py-2">{c.number}</td>
            <td className="px-3 py-2">{c.object}</td>
            <td className="px-3 py-2">{c.status}</td>
            <td className="px-3 py-2">{c.policyVerified ? 'Verificada' : 'Pendiente'}</td>
            <td className="px-3 py-2">
              <Button variant="plain" onClick={() => api.post(`/api/contratos/${c.id}/poliza`).then(() => qc.invalidateQueries({ queryKey: ['ctr'] }))}>Verificar póliza</Button>
              <Button variant="plain" onClick={() => api.post(`/api/contratos/${c.id}/acta`).then(() => qc.invalidateQueries({ queryKey: ['ctr'] }))}>Acta de inicio</Button>
            </td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export function IntegrationsPage() {
  const { data } = useQuery({ queryKey: ['int'], queryFn: () => api.get<any[]>('/api/integraciones') })
  return (
    <div>
      <PageHeader kicker="D11" title="Conexiones con otras entidades">23 filas del informe. Cada consulta es un simulador controlado.</PageHeader>
      <Table headers={['#', 'Dato', 'Entidad', 'Tipo', 'Estado']}>
        {(data ?? []).map((s) => (
          <tr key={s.row}>
            <td className="px-3 py-2">{s.row}</td>
            <td className="px-3 py-2">{s.data}</td>
            <td className="px-3 py-2">{s.entity}</td>
            <td className="px-3 py-2">{s.kind}</td>
            <td className="px-3 py-2"><Badge tone={s.status === 'Disponible' ? 'ok' : 'warn'}>{s.status}</Badge></td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export const AssignmentsPage = () => <SimpleList path="/api/seguridad/asignaciones" kicker="D3" title="Asignaciones" columns={['Tipo', 'Estado', 'Dependencia', 'Funcionario']} rows={(r) => [r.subjectType, r.status, r.departamento, r.funcionario]} />
export const ParamsPage = () => {
  const { data } = useQuery({ queryKey: ['par'], queryFn: () => api.get<any>('/api/catalogos/parametros') })
  return (
    <div>
      <PageHeader kicker="D4" title="Calendario y parámetros" />
      <Card>
        <h3 className="font-medium mb-2">Festivos</h3>
        <p className="text-sm text-muted mb-3">Una sola tabla para todos los términos.</p>
        {(data?.festivos ?? []).map((h: any) => <span key={h.id} className="inline-block text-xs border border-line rounded px-2 py-1 mr-1 mb-1">{h.date} {h.name}</span>)}
      </Card>
    </div>
  )
}
export const CarteraPage = () => <SimpleList path="/api/seguridad/cartera" kicker="D7" title="Contribución y cartera" columns={['Sociedad', 'Año', 'Estado']} rows={(r) => [r.sociedad, String(r.year), r.status]} />
export const SanctionsPage = () => <SimpleList path="/api/seguridad/sanciones" kicker="D8" title="Sanciones" columns={['Número', 'Tipo', 'Estado']} rows={(r) => [r.number, r.type, r.status]} />
export const QualityPage = () => {
  const { data } = useQuery({ queryKey: ['cal'], queryFn: () => api.get<any>('/api/seguridad/calidad') })
  return (
    <div>
      <PageHeader kicker="D9" title="Calidad de información" />
      <Table headers={['Incidencia', 'Estado']}>{(data?.incidencias ?? []).map((i: any) => <tr key={i.id}><td className="px-3 py-2">{i.description}</td><td className="px-3 py-2">{i.status}</td></tr>)}</Table>
    </div>
  )
}
export const AnalyticsPage = () => {
  const { data } = useQuery({ queryKey: ['an'], queryFn: () => api.get<any>('/api/seguridad/analitica') })
  return (
    <div>
      <PageHeader kicker="D10" title="Analítica interna">{data?.nota}</PageHeader>
      <Card className="h-72">
        <ResponsiveContainer>
          <BarChart data={data?.bySector || []}>
            <XAxis dataKey="sector" tick={{ fontSize: 10 }} />
            <YAxis />
            <Tooltip />
            <Bar dataKey="n" fill="#0B1F33" />
          </BarChart>
        </ResponsiveContainer>
      </Card>
    </div>
  )
}
export const ActivityPage = () => <SimpleList path="/api/seguridad/eventos" kicker="Plataforma" title="Hechos del proceso" columns={['Hecho', 'Origen', 'Estado']} rows={(r) => [r.type, r.source, r.processingStatus]} />
export const SecurityPage = () => <SimpleList path="/api/seguridad/auditoria" kicker="E4" title="Auditoría" columns={['Quién', 'Acción', 'Detalle']} rows={(r) => [r.userName, r.action, r.detail]} />
export const PagesWatchPage = () => <SimpleList path="/api/seguridad/paginas" kicker="J5" title="Páginas de los procesos" columns={['Página', 'Estado', 'Hallazgo']} rows={(r) => [r.title, r.status, r.findings]} />
export const ProceduresPage = () => {
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['trm'], queryFn: () => api.get<any[]>('/api/seguridad/tramites') })
  return (
    <div>
      <PageHeader kicker="J6" title="Trámites societarios" />
      <Table headers={['Número', 'Tipo', 'Sociedad', 'Paso', '']}>
        {(data ?? []).map((p) => (
          <tr key={p.id}>
            <td className="px-3 py-2">{p.number}</td>
            <td className="px-3 py-2">{p.type}</td>
            <td className="px-3 py-2">{p.society}</td>
            <td className="px-3 py-2">{p.approvalStep} / 3</td>
            <td className="px-3 py-2"><Button variant="plain" onClick={() => api.post(`/api/seguridad/tramites/${p.id}/aprobar`).then(() => qc.invalidateQueries({ queryKey: ['trm'] }))}>Aprobar paso</Button></td>
          </tr>
        ))}
      </Table>
    </div>
  )
}
