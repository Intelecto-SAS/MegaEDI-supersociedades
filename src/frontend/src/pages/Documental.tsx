import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useState } from 'react'
import { api } from '@/lib/api'
import { AssistBanner, Badge, Button, Card, Field, Input, PageHeader, Select, Table, Textarea } from '@/components/ui'

export function RecordsPage() {
  const [q, setQ] = useState('')
  const { data } = useQuery({
    queryKey: ['radicados', q],
    queryFn: () => api.get<any>(`/api/radicados?q=${encodeURIComponent(q)}&page=1&size=30`),
  })
  return (
    <div>
      <PageHeader kicker="EDI Documental" title="Bandeja de radicados" actions={<Link to="/documental/nuevo"><Button>Nueva comunicación</Button></Link>}>
        Entrada única de correo, ventanilla, mensajería y formularios internos. No hay portal público.
      </PageHeader>
      <Input placeholder="Buscar por número o asunto" value={q} onChange={(e) => setQ(e.target.value)} className="mb-4 max-w-md" />
      <Table headers={['Número', 'Asunto', 'Canal', 'Estado', 'Prioridad', 'Plazo']}>
        {(data?.items ?? []).map((r: any) => (
          <tr key={r.id} className="hover:bg-sand/40">
            <td className="px-3 py-2"><Link className="text-navy font-medium" to={`/documental/radicados/${r.id}`}>{r.number}</Link></td>
            <td className="px-3 py-2">{r.subject}</td>
            <td className="px-3 py-2">{r.channel}</td>
            <td className="px-3 py-2"><Badge>{r.status}</Badge></td>
            <td className="px-3 py-2">{r.isTutela ? <Badge tone="warn">Tutela</Badge> : r.priority}</td>
            <td className="px-3 py-2 text-muted">{r.dueAt ? new Date(r.dueAt).toLocaleDateString('es-CO') : '—'}</td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export function NewRecordPage() {
  const nav = useNavigate()
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [channel, setChannel] = useState('Correo')
  const [suggestion, setSuggestion] = useState<any>(null)
  const classify = useMutation({
    mutationFn: () => api.post('/api/radicados/clasificar', { subject, body }),
    onSuccess: setSuggestion,
  })
  const create = useMutation({
    mutationFn: () => api.post<any>('/api/radicados', { channel, subject, body }),
    onSuccess: (r) => nav(`/documental/radicados/${r.id}`),
  })
  return (
    <div className="max-w-3xl">
      <PageHeader kicker="Radicación" title="Nueva comunicación" />
      <AssistBanner />
      <Card className="mt-4 space-y-4">
        <Field label="Canal de origen">
          <Select value={channel} onChange={(e) => setChannel(e.target.value)}>
            <option>Correo</option><option>Ventanilla</option><option>Mensajería</option><option>Formulario</option>
          </Select>
        </Field>
        <Field label="Asunto"><Input value={subject} onChange={(e) => setSubject(e.target.value)} /></Field>
        <Field label="Contenido"><Textarea value={body} onChange={(e) => setBody(e.target.value)} /></Field>
        <div className="flex gap-2">
          <Button variant="ghost" onClick={() => classify.mutate()} disabled={!subject}>Pedir sugerencia</Button>
          <Button onClick={() => create.mutate()} disabled={!subject || create.isPending}>Radicar</Button>
        </div>
        {suggestion && (
          <div className="rounded-md bg-sand p-3 text-sm">
            <p className="font-medium text-navy">Sugerencia: {suggestion.name} · {suggestion.priority}</p>
            <p className="text-muted mt-1">{suggestion.explanation}</p>
            <p className="text-xs mt-2">{suggestion.banner}</p>
          </div>
        )}
        {create.error && <p className="text-warn text-sm">{(create.error as Error).message}</p>}
      </Card>
    </div>
  )
}

export function RecordDetailPage() {
  const { id } = useParams()
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['radicado', id], queryFn: () => api.get<any>(`/api/radicados/${id}`) })
  const [reply, setReply] = useState('Respuesta institucional de demostración.')
  const act = async (path: string, body?: unknown) => {
    await api.post(`/api/radicados/${id}/${path}`, body)
    await qc.invalidateQueries({ queryKey: ['radicado', id] })
  }
  if (!data) return null
  return (
    <div>
      <PageHeader kicker="Radicado" title={data.number} actions={<Badge tone={data.isTutela ? 'warn' : 'navy'}>{data.status}</Badge>}>
        {data.subject}
      </PageHeader>
      <div className="grid lg:grid-cols-3 gap-4">
        <Card className="lg:col-span-2 space-y-3">
          <dl className="grid sm:grid-cols-2 gap-3 text-sm">
            <div><dt className="text-muted">Recepción</dt><dd>{new Date(data.receivedAt).toLocaleString('es-CO')}</dd></div>
            <div><dt className="text-muted">Radicación</dt><dd>{new Date(data.filedAt).toLocaleString('es-CO')}</dd></div>
            <div><dt className="text-muted">Ingreso al expediente</dt><dd>{data.incorporatedAt ? new Date(data.incorporatedAt).toLocaleString('es-CO') : 'Pendiente'}</dd></div>
            <div><dt className="text-muted">Clasificación</dt><dd>{data.classificationName}</dd></div>
            <div><dt className="text-muted">Remitente</dt><dd>{data.senderName || '—'}</dd></div>
            <div><dt className="text-muted">Sociedad</dt><dd>{data.societyName || '—'}</dd></div>
            <div><dt className="text-muted">Responsable</dt><dd>{data.assignee || data.department}</dd></div>
            <div><dt className="text-muted">Plazo</dt><dd>{data.dueAt ? new Date(data.dueAt).toLocaleDateString('es-CO') : '—'}</dd></div>
          </dl>
          <p className="text-sm leading-relaxed">{data.body}</p>
          {data.suggestionExplanation && (
            <div className="rounded-md bg-gold/10 p-3 text-sm">
              <p className="font-medium">Sugerencia del motor local</p>
              <p>{data.suggestionExplanation}</p>
              <p className="text-xs mt-1">Decisión humana: {data.suggestionDecision}</p>
            </div>
          )}
          <AssistBanner />
        </Card>
        <Card className="space-y-2">
          <h2 className="serif text-lg">Actuaciones</h2>
          <Button variant="ghost" className="w-full" onClick={() => act('clasificacion', { classificationCode: data.classificationCode })}>Confirmar clasificación</Button>
          <Button variant="ghost" className="w-full" onClick={() => act('respuesta', { subject: 'Respuesta a ' + data.number, body: reply })}>Preparar respuesta</Button>
          <Textarea value={reply} onChange={(e) => setReply(e.target.value)} />
          <Button className="w-full" onClick={() => act('aprobar')}>Aprobar y notificar</Button>
          <Button variant="ghost" className="w-full" onClick={() => act('expediente', { title: data.subject })}>Vincular a expediente</Button>
          {data.caseId && <Link className="text-sm text-navy underline" to={`/justicia/expedientes/${data.caseId}`}>Abrir expediente</Link>}
        </Card>
      </div>
    </div>
  )
}
