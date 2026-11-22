import { useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Badge, Button, Card, PageHeader, Table } from '@/components/ui'
import { useState } from 'react'

export function MigrationPage() {
  const qc = useQueryClient()
  const { data } = useQuery({ queryKey: ['mig'], queryFn: () => api.get<any>('/api/modernizacion') })
  const { data: legacy } = useQuery({ queryKey: ['leg'], queryFn: () => api.get<any[]>('/api/modernizacion/heredado') })
  const [compare, setCompare] = useState<any>(null)
  const batchId = data?.batches?.[0]?.id
  const go = async (path: string, body?: unknown) => {
    await api.post(path, body)
    await qc.invalidateQueries({ queryKey: ['mig'] })
  }
  return (
    <div>
      <PageHeader kicker="Transición operativa" title="Modernización GeDeSS">
        {data?.disclaimer}
      </PageHeader>
      <div className="grid md:grid-cols-3 xl:grid-cols-6 gap-3 mb-6">
        {[
          ['Heredados', data?.legacyVolume],
          ['Migrados', data?.migrated],
          ['Pendientes', data?.pending],
          ['Rechazados', data?.rejected],
          ['Duplicados', data?.duplicates],
          ['Conciliados', data?.reconciled],
        ].map(([l, v]) => (
          <Card key={String(l)}><p className="text-[11px] uppercase text-muted">{l}</p><p className="serif text-2xl text-navy">{v ?? '—'}</p></Card>
        ))}
      </div>
      <div className="flex flex-wrap gap-2 mb-6">
        {batchId && <>
          <Button onClick={() => go(`/api/modernizacion/${batchId}/inventario`)}>1. Inventariar lote</Button>
          <Button onClick={() => go(`/api/modernizacion/${batchId}/migrar`)}>2. Migración de prueba</Button>
          <Button onClick={() => go(`/api/modernizacion/${batchId}/conciliar`)}>3. Conciliar</Button>
          <Button variant="gold" onClick={() => go('/api/modernizacion/traslado', { capability: 'Expedientes' })}>4. Trasladar expedientes a EDI</Button>
        </>}
      </div>
      <div className="grid lg:grid-cols-2 gap-4 mb-6">
        <Card>
          <h2 className="serif text-xl mb-2">Capacidades</h2>
          <p className="text-sm text-muted mb-3">No todo GeDeSS cambia al mismo tiempo. Cada capacidad tiene un responsable operativo.</p>
          <Table headers={['Capacidad', 'Opera en', 'Estado']}>
            {(data?.capabilities ?? []).map((c: any) => (
              <tr key={c.capability}>
                <td className="px-3 py-2">{c.capability}</td>
                <td className="px-3 py-2"><Badge tone={c.owner === 'EDI' ? 'ok' : 'gold'}>{c.owner}</Badge></td>
                <td className="px-3 py-2 text-xs">{c.state}</td>
              </tr>
            ))}
          </Table>
        </Card>
        <Card>
          <h2 className="serif text-xl mb-2">Lotes</h2>
          {(data?.batches ?? []).map((b: any) => (
            <div key={b.id} className="border-b border-line py-2 text-sm">
              <p className="font-medium">{b.name}</p>
              <p className="text-muted">{b.status} · migrados {b.migratedCount} / {b.legacyCount}</p>
            </div>
          ))}
          <h3 className="mt-4 text-xs uppercase text-muted">Trazabilidad</h3>
          {(data?.history ?? []).map((h: any, i: number) => <p key={i} className="text-sm py-1">{h.title} · {h.detail}</p>)}
        </Card>
      </div>
      <Card>
        <h2 className="serif text-xl mb-2">GeDeSS heredado simulado</h2>
        <Table headers={['Número', 'Asunto', 'Expediente', 'Alertas', 'Comparar']}>
          {(legacy ?? []).slice(0, 15).map((r: any) => (
            <tr key={r.id}>
              <td className="px-3 py-2">{r.legacyNumber}</td>
              <td className="px-3 py-2">{r.subject}</td>
              <td className="px-3 py-2">{r.hasCase ? 'Sí' : 'No'}</td>
              <td className="px-3 py-2 text-xs">{r.qualityFlags}</td>
              <td className="px-3 py-2"><Button variant="plain" onClick={() => api.get(`/api/modernizacion/comparar/${r.id}`).then(setCompare)}>Antes / después</Button></td>
            </tr>
          ))}
        </Table>
        {compare && (
          <div className="grid md:grid-cols-2 gap-3 mt-4 text-sm">
            <div className="rounded-md bg-sand p-3">
              <p className="text-xs uppercase text-muted mb-1">Heredado</p>
              <pre className="whitespace-pre-wrap text-xs">{JSON.stringify(compare.legacy, null, 2)}</pre>
            </div>
            <div className="rounded-md bg-white border border-line p-3">
              <p className="text-xs uppercase text-muted mb-1">EDI Documental</p>
              <pre className="whitespace-pre-wrap text-xs">{JSON.stringify(compare.edi, null, 2)}</pre>
              <ul className="mt-2 text-warn">{compare.differences?.map((d: string) => <li key={d}>{d}</li>)}</ul>
            </div>
          </div>
        )}
      </Card>
    </div>
  )
}
