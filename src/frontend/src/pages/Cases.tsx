import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { api } from '@/lib/api'
import { AssistBanner, Badge, Button, Card, Field, Input, PageHeader, Table, Textarea } from '@/components/ui'
import { useState } from 'react'

export function CasesPage({ type }: { type?: string }) {
  const { data } = useQuery({
    queryKey: ['expedientes', type],
    queryFn: () => api.get<any>(`/api/expedientes?type=${type || ''}&size=40`),
  })
  return (
    <div>
      <PageHeader kicker="Justicia digital" title={type === 'Insolvencia' ? 'Insolvencia' : 'Expedientes'}>
        Cada caso tiene responsable, etapa y desenlace. Los reservados no aparecen en la búsqueda general.
      </PageHeader>
      <Table headers={['Número', 'Asunto', 'Tipo', 'Etapa', 'Estado', 'Plazo']}>
        {(data?.items ?? []).map((c: any) => (
          <tr key={c.id}>
            <td className="px-3 py-2"><Link className="text-navy font-medium" to={`/justicia/expedientes/${c.id}`}>{c.number}</Link></td>
            <td className="px-3 py-2">{c.title}</td>
            <td className="px-3 py-2">{c.type}</td>
            <td className="px-3 py-2">{c.stage}</td>
            <td className="px-3 py-2"><Badge>{c.status}</Badge></td>
            <td className="px-3 py-2 text-muted">{c.dueAt ? new Date(c.dueAt).toLocaleDateString('es-CO') : '—'}</td>
          </tr>
        ))}
      </Table>
    </div>
  )
}

export function CaseDetailPage() {
  const { id } = useParams()
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['exp', id], queryFn: () => api.get<any>(`/api/expedientes/${id}`) })
  const [title, setTitle] = useState('Actuación de trámite')
  const [body, setBody] = useState('Registro demostrativo.')
  const [amount, setAmount] = useState('120000000')
  const run = async (path: string, payload?: unknown) => {
    await api.post(`/api/expedientes/${id}/${path}`, payload)
    await qc.invalidateQueries({ queryKey: ['exp', id] })
  }
  if (!data) return null
  return (
    <div>
      <PageHeader kicker={data.number} title={data.title} actions={<Badge tone={data.classification === 'Reservado' ? 'warn' : 'navy'}>{data.status}</Badge>}>
        {data.society} · {data.department} · {data.assignee || 'Pendiente de funcionario'}
      </PageHeader>
      <div className="grid lg:grid-cols-3 gap-4">
        <Card className="lg:col-span-2">
          <dl className="grid sm:grid-cols-3 gap-3 text-sm mb-4">
            <div><dt className="text-muted">Etapa</dt><dd>{data.stage}</dd></div>
            <div><dt className="text-muted">Prioridad</dt><dd>{data.priority}</dd></div>
            <div><dt className="text-muted">Desenlace</dt><dd>{data.outcome || 'Abierto'}</dd></div>
          </dl>
          <p className="text-sm mb-4">{data.summary}</p>
          <h3 className="serif text-lg mb-2">Línea de tiempo</h3>
          <ul className="space-y-2 text-sm">
            {data.timeline?.map((t: any, i: number) => (
              <li key={i} className="border-l-2 border-brand pl-3">{new Date(t.at).toLocaleDateString('es-CO')} · {t.title}</li>
            ))}
          </ul>
          <h3 className="serif text-lg mt-6 mb-2">Documentos</h3>
          {data.documents?.map((d: any) => (
            <p key={d.id} className="text-sm">{d.title} · v{d.version}</p>
          ))}
        </Card>
        <div className="space-y-4">
          <Card>
            <h3 className="serif text-lg mb-2">Resumen asistido</h3>
            <pre className="whitespace-pre-wrap text-xs text-muted">{data.assistanceSummary}</pre>
            <div className="mt-2"><AssistBanner /></div>
          </Card>
          <Card className="space-y-2">
            <h3 className="serif text-lg">Actuaciones autorizadas</h3>
            <Field label="Título"><Input value={title} onChange={(e) => setTitle(e.target.value)} /></Field>
            <Field label="Nota"><Textarea value={body} onChange={(e) => setBody(e.target.value)} /></Field>
            {data.allowedActions?.includes('registrar-actuacion') && (
              <Button variant="ghost" className="w-full" onClick={() => run('actuaciones', { type: 'Actuación', title, body })}>Registrar actuación</Button>
            )}
            {data.allowedActions?.includes('providencia') && (
              <Button variant="ghost" className="w-full" onClick={() => run('actuaciones', { type: 'Providencia', title, body })}>Registrar providencia</Button>
            )}
            {data.allowedActions?.includes('confirmar-acuerdo') && (
              <>
                <Field label="Valor del acuerdo"><Input value={amount} onChange={(e) => setAmount(e.target.value)} /></Field>
                <Button className="w-full" onClick={() => run('acuerdo', { totalAmount: Number(amount), installments: 6, notes: body })}>Confirmar acuerdo</Button>
              </>
            )}
            {data.allowedActions?.includes('cerrar') && (
              <Button variant="gold" className="w-full" onClick={() => run('cierre', { outcome: 'Recuperación', notes: body })}>Cerrar con desenlace</Button>
            )}
            {data.type === 'Insolvencia' && (
              <Button variant="ghost" className="w-full" onClick={() => run('incumplimiento')}>Registrar incumplimiento</Button>
            )}
            {data.societyId && <Link className="text-sm underline text-navy" to={`/supervision/sociedades/${data.societyId}`}>Ver ficha 360</Link>}
          </Card>
        </div>
      </div>
    </div>
  )
}
