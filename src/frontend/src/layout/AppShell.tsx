import { NavLink, Outlet, useNavigate, useSearchParams } from 'react-router-dom'
import { useState } from 'react'
import {
  Building2, FileText, Gavel, LayoutDashboard, LogOut, Search, Shield,
  Briefcase, Landmark, BarChart3, Sparkles, FolderSync, ChevronLeft
} from 'lucide-react'
import { useAuth } from '@/lib/auth'
import { BrandMark, Button, FlagStripe, Input } from '@/components/ui'
import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'

type Item = { to: string; label: string; perm?: string; icon?: typeof LayoutDashboard }

const groups: { title: string; items: Item[] }[] = [
  { title: 'Inicio', items: [
    { to: '/', label: 'Inicio', icon: LayoutDashboard },
    { to: '/trabajo', label: 'Mi trabajo', icon: Briefcase },
  ]},
  { title: 'Gestión documental', items: [
    { to: '/documental/radicados', label: 'Bandeja de radicados', perm: 'J1.VER', icon: FileText },
    { to: '/documental/nuevo', label: 'Nueva comunicación', perm: 'J1.RADICAR' },
    { to: '/justicia/expedientes', label: 'Expedientes', perm: 'J2.VER' },
  ]},
  { title: 'Justicia digital', items: [
    { to: '/justicia/insolvencia', label: 'Insolvencia', perm: 'J3.VER', icon: Gavel },
    { to: '/justicia/auxiliares', label: 'Auxiliares', perm: 'J4.VER' },
    { to: '/justicia/paginas', label: 'Páginas de procesos', perm: 'J5.VER' },
    { to: '/justicia/tramites', label: 'Trámites societarios', perm: 'J6.VER' },
  ]},
  { title: 'Supervisión', items: [
    { to: '/supervision/sociedades', label: 'Sociedades', perm: 'D1.VER', icon: Building2 },
    { to: '/supervision/alertas', label: 'Alertas', perm: 'D6.VER' },
    { to: '/supervision/xbrl', label: 'Reportes financieros', perm: 'E5.CARGAR' },
  ]},
  { title: 'Gobierno de datos', items: [
    { to: '/datos/terceros', label: 'Terceros', perm: 'D2.VER' },
    { to: '/datos/asignaciones', label: 'Asignaciones', perm: 'D3.VER' },
    { to: '/datos/parametros', label: 'Calendario y parámetros', perm: 'D4.VER' },
    { to: '/datos/cartera', label: 'Contribución y cartera', perm: 'D7.VER' },
    { to: '/datos/sanciones', label: 'Sanciones', perm: 'D8.VER' },
    { to: '/datos/calidad', label: 'Calidad de información', perm: 'D9.VER' },
    { to: '/datos/analitica', label: 'Analítica interna', perm: 'D10.VER' },
    { to: '/datos/conexiones', label: 'Conexiones con entidades', perm: 'D11.VER' },
  ]},
  { title: 'Informes', items: [
    { to: '/tableros', label: 'Tablero institucional', perm: 'E2.VER', icon: BarChart3 },
    { to: '/informes', label: 'Informes especializados', perm: 'E7.VER' },
  ]},
  { title: 'Administración', items: [
    { to: '/admin/contratos', label: 'Contratos', perm: 'E6.VER', icon: Landmark },
    { to: '/admin/actividad', label: 'Actividad de la plataforma', perm: 'E3.VER' },
    { to: '/admin/seguridad', label: 'Seguridad y auditoría', perm: 'E4.VER', icon: Shield },
  ]},
]

export function AppShell() {
  const { session, logout, can } = useAuth()
  const [collapsed, setCollapsed] = useState(false)
  const [q, setQ] = useState('')
  const nav = useNavigate()
  const clock = session ? new Date(session.businessClock) : new Date()

  return (
    <div className="min-h-screen flex flex-col bg-paper">
      <FlagStripe />
      <div className="flex flex-1 min-h-0">
      <aside className={`${collapsed ? 'w-[72px]' : 'w-[272px]'} shrink-0 bg-wine text-white flex flex-col min-h-0`}>
        <div className="px-4 py-4 border-b border-white/10 flex items-center gap-3">
          <div className="h-10 w-10 rounded-md bg-white grid place-items-center p-0.5 shrink-0">
            <BrandMark compact className="h-8 w-8" />
          </div>
          {!collapsed && (
            <div className="min-w-0">
              <p className="serif text-lg leading-none">EDI 360</p>
              <p className="text-[10px] uppercase tracking-[0.14em] text-white/55 mt-1 leading-tight">Superintendencia de Sociedades</p>
            </div>
          )}
        </div>
        <nav className="flex-1 overflow-y-auto sidebar-scroll py-3 px-2">
          {groups.map((g) => {
            const items = g.items.filter((i) => !i.perm || can(i.perm))
            if (!items.length) return null
            return (
              <div key={g.title} className="mb-3">
                {!collapsed && <p className="px-3 mb-1 text-[10px] uppercase tracking-[0.18em] text-white/40">{g.title}</p>}
                {items.map((i) => (
                  <NavLink
                    key={i.to}
                    to={i.to}
                    end={i.to === '/'}
                    className={({ isActive }) =>
                      `flex items-center gap-2 rounded-md px-3 py-1.5 text-[13px] mb-0.5 ${isActive ? 'bg-white/10 text-white' : 'text-white/70 hover:bg-white/5 hover:text-white'}`
                    }
                  >
                    {i.icon ? <i.icon size={16} /> : <span className="w-4" />}
                    {!collapsed && i.label}
                  </NavLink>
                ))}
              </div>
            )
          })}
          {can('GD.MIGRAR') && (
            <NavLink to="/modernizacion" className={({ isActive }) => `flex items-center gap-2 rounded-md px-3 py-1.5 text-[13px] ${isActive ? 'bg-white/15 text-white' : 'text-white/80 hover:bg-white/5 hover:text-white'}`}>
              <FolderSync size={16} />
              {!collapsed && 'Modernización GeDeSS'}
            </NavLink>
          )}
          {can('DEMO.CONTROL') && (
            <NavLink to="/demo" className={({ isActive }) => `mt-1 flex items-center gap-2 rounded-md px-3 py-1.5 text-[13px] ${isActive ? 'bg-brand text-white' : 'text-white/80 hover:bg-white/5'}`}>
              <Sparkles size={16} />
              {!collapsed && 'Centro de demostración'}
            </NavLink>
          )}
        </nav>
        <button className="p-3 text-white/50 hover:text-white" onClick={() => setCollapsed(!collapsed)} aria-label="Plegar menú">
          <ChevronLeft className={collapsed ? 'rotate-180' : ''} size={16} />
        </button>
      </aside>
      <div className="flex-1 min-w-0 flex flex-col">
        <header className="h-16 border-b border-line bg-white/80 backdrop-blur px-6 flex items-center gap-4">
          <form
            className="flex-1 max-w-xl flex items-center gap-2"
            onSubmit={(e) => { e.preventDefault(); if (q) nav(`/buscar?q=${encodeURIComponent(q)}`) }}
          >
            <Search size={16} className="text-muted" />
            <Input placeholder="Buscar radicado, expediente o sociedad" value={q} onChange={(e) => setQ(e.target.value)} className="border-0 shadow-none bg-transparent" />
          </form>
          <div className="text-right">
            <p className="text-sm font-medium text-navy">{session?.displayName}</p>
            <p className="text-[11px] text-muted">{session?.position} · {session?.department}</p>
          </div>
          <div className="text-[11px] text-muted border-l border-line pl-4">
            Reloj de la demostración<br />
            {clock.toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' })}
          </div>
          <Button variant="ghost" onClick={() => logout().then(() => nav('/entrar'))} aria-label="Salir">
            <LogOut size={16} />
          </Button>
        </header>
        <main className="flex-1 p-6 lg:p-8 overflow-auto">
          {session?.impersonatedBy && (
            <div className="mb-4 rounded-md bg-brand/10 border border-brand/25 px-3 py-2 text-sm text-navy">
              Está presentando como {session.displayName}. El cambio de funcionario quedó registrado.
            </div>
          )}
          <Outlet />
        </main>
      </div>
      </div>
    </div>
  )
}

export function SearchPage() {
  const [params] = useSearchParams()
  const q = params.get('q') || ''
  const { data } = useQuery({
    queryKey: ['search', q],
    queryFn: () => api.get<any>(`/api/seguridad/busqueda?q=${encodeURIComponent(q)}`),
    enabled: q.length > 1,
  })
  return (
    <div>
      <h1 className="serif text-2xl text-navy mb-4">Resultados para “{q}”</h1>
      {!data && <p className="text-muted">Escriba al menos dos caracteres.</p>}
      {data && (
        <div className="grid gap-4">
          {['radicados', 'expedientes', 'sociedades'].map((k) => (
            <div key={k}>
              <h2 className="text-sm uppercase tracking-wider text-muted mb-2">{k}</h2>
              {(data[k] || []).map((r: any) => (
                <p key={r.id} className="text-sm py-1">{r.titulo} · {r.subject}</p>
              ))}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
