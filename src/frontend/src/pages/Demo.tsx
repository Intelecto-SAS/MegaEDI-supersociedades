import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Badge, Button, Card, PageHeader, Table } from '@/components/ui'
import { useAuth } from '@/lib/auth'

const scenarios = [
  { id: 'radicacion', title: 'Radicación inteligente', text: 'Clasificación sugerida, confirmación, radicado, reparto y respuesta.' },
  { id: 'tutela', title: 'Tutela urgente', text: 'Plazo corto, abogado, borrador y notificación simulada.' },
  { id: 'expediente', title: 'Expediente único', text: 'El radicado entra al expediente y actualiza la línea de tiempo.' },
  { id: 'insolvencia', title: 'Reorganización', text: 'Acuerdo, sociedad, alertas e indicadores se actualizan juntos.' },
  { id: 'supervision', title: 'Supervisión por riesgo', text: 'Ficha 360, cifras, reglas y actuación del supervisor.' },
  { id: 'auxiliar', title: 'Designación de auxiliar', text: 'Inconsistencia, corrección, validación y designación.' },
  { id: 'gedess', title: 'Migración gradual de GeDeSS', text: 'Inventario, prueba, conciliación y traslado de una capacidad.' },
  { id: 'informe', title: 'Informe para dirección', text: 'Borrador local, revisión humana, firma y PDF.' },
]

export function DemoPage() {
  const qc = useQueryClient()
  const { session } = useAuth()
  const { data } = useQuery({ queryKey: ['demo'], queryFn: () => api.get<any>('/api/demo/estado') })
  const refresh = () => qc.invalidateQueries()
  return (
    <div>
      <PageHeader kicker="Solo presentador" title="Centro de demostración">
        Prepare escenarios, cambie de funcionario y restablezca el entorno. Cada cambio queda en el servidor.
      </PageHeader>
      <div className="grid lg:grid-cols-3 gap-4 mb-6">
        <Card>
          <p className="text-xs uppercase tracking-wider text-muted">Reloj de negocio</p>
          <p className="serif text-2xl">{data ? new Date(data.businessClock).toLocaleString('es-CO') : '—'}</p>
          <div className="flex gap-2 mt-3">
            <Button variant="ghost" onClick={() => api.post('/api/demo/reloj', { days: 1 }).then(refresh)}>+1 día</Button>
            <Button variant="ghost" onClick={() => api.post('/api/demo/reloj', { days: 7 }).then(refresh)}>+7 días</Button>
          </div>
        </Card>
        <Card>
          <p className="text-xs uppercase tracking-wider text-muted">Escenario activo</p>
          <p className="serif text-2xl">{data?.scenario || 'Ninguno'}</p>
          <Button className="mt-3" variant="danger" onClick={() => { if (confirm('¿Restablecer todos los datos ficticios?')) api.post('/api/demo/reset').then(() => location.reload()) }}>Restablecer entorno</Button>
        </Card>
        <Card>
          <p className="text-xs uppercase tracking-wider text-muted">Presentando como</p>
          <p className="serif text-xl">{session?.displayName}</p>
          <p className="text-sm text-muted">{session?.position}</p>
        </Card>
      </div>
      <h2 className="serif text-2xl text-navy mb-3">Recorridos guiados</h2>
      <div className="grid md:grid-cols-2 gap-3 mb-8">
        {scenarios.map((s) => (
          <Card key={s.id}>
            <h3 className="font-medium text-navy">{s.title}</h3>
            <p className="text-sm text-muted mt-1">{s.text}</p>
            <Button className="mt-3" onClick={() => api.post('/api/demo/escenario', { scenario: s.id }).then(refresh)}>Preparar escenario</Button>
          </Card>
        ))}
      </div>
      <h2 className="serif text-2xl text-navy mb-3">Cambiar de funcionario</h2>
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-2 mb-8">
        {(data?.officials ?? []).slice(0, 24).map((o: any) => (
          <button key={o.id} className="text-left rounded-lg border border-line bg-white px-3 py-2 hover:border-brand" onClick={() => api.post('/api/demo/impersonar', { userId: o.id }).then(() => location.reload())}>
            <p className="text-sm font-medium">{o.displayName}</p>
            <p className="text-xs text-muted">{o.position}</p>
          </button>
        ))}
      </div>
      <div className="grid lg:grid-cols-2 gap-4">
        <Card>
          <h3 className="serif text-lg mb-2">Estado de módulos</h3>
          <Table headers={['Código', 'Nombre', 'Origen', 'Estado']}>
            {(data?.modules ?? []).map((m: any) => (
              <tr key={m.code}><td className="px-3 py-2">{m.code}</td><td className="px-3 py-2">{m.name}</td><td className="px-3 py-2">{m.source}</td><td className="px-3 py-2"><Badge>{m.status}</Badge></td></tr>
            ))}
          </Table>
        </Card>
        <Card>
          <h3 className="serif text-lg mb-2">Actividad reciente</h3>
          {(data?.recentAudit ?? []).map((a: any, i: number) => <p key={i} className="text-sm py-1 border-b border-line">{a.title} · {a.detail}</p>)}
          <h3 className="serif text-lg mt-4 mb-2">Hechos del proceso</h3>
          {(data?.recentEvents ?? []).map((a: any, i: number) => <p key={i} className="text-sm py-1 border-b border-line">{a.title} · {a.detail}</p>)}
        </Card>
      </div>
      <Card className="mt-4">
        <h3 className="serif text-lg mb-2">Falla simulada de una conexión</h3>
        <div className="flex flex-wrap gap-2">
          <Button variant="ghost" onClick={() => api.post('/api/demo/falla-integracion', { matrixRow: 5, mode: 'Indisponible' }).then(refresh)}>Registraduría indisponible</Button>
          <Button variant="ghost" onClick={() => api.post('/api/demo/falla-integracion', { matrixRow: 20, mode: 'Error' }).then(refresh)}>Pólizas con error</Button>
          <Button variant="ghost" onClick={() => api.post('/api/demo/restablecer-integracion/5').then(refresh)}>Restablecer identidad</Button>
        </div>
      </Card>
    </div>
  )
}
