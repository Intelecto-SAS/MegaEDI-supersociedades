import type { ReactNode } from 'react'
import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { AppShell, SearchPage } from '@/layout/AppShell'
import { LoginPage } from '@/pages/Login'
import { HomePage, WorkPage } from '@/pages/Home'
import { NewRecordPage, RecordDetailPage, RecordsPage } from '@/pages/Documental'
import { CaseDetailPage, CasesPage } from '@/pages/Cases'
import { AlertsPage, SocietiesPage, SocietyDetailPage, XbrlPage } from '@/pages/Supervision'
import { DemoPage } from '@/pages/Demo'
import { MigrationPage } from '@/pages/Migration'
import {
  ActivityPage, AnalyticsPage, AssignmentsPage, AuxPage, CarteraPage, ContractsPage,
  DashboardPage, IntegrationsPage, PagesWatchPage, ParamsPage, ProceduresPage,
  QualityPage, ReportDetailPage, ReportsPage, SanctionsPage, SecurityPage, ThirdPartiesPage,
} from '@/pages/More'

function Guard({ children }: { children: ReactNode }) {
  const { session, loading } = useAuth()
  if (loading) return <div className="min-h-screen grid place-items-center text-muted">Abriendo EDI 360…</div>
  if (!session) return <Navigate to="/entrar" replace />
  return children
}

function ReportRoute() {
  const { id } = useParams()
  return <ReportDetailPage id={id!} />
}

export default function App() {
  return (
    <Routes>
      <Route path="/entrar" element={<LoginPage />} />
      <Route element={<Guard><AppShell /></Guard>}>
        <Route path="/" element={<HomePage />} />
        <Route path="/trabajo" element={<WorkPage />} />
        <Route path="/documental/radicados" element={<RecordsPage />} />
        <Route path="/documental/radicados/:id" element={<RecordDetailPage />} />
        <Route path="/documental/nuevo" element={<NewRecordPage />} />
        <Route path="/justicia/expedientes" element={<CasesPage />} />
        <Route path="/justicia/expedientes/:id" element={<CaseDetailPage />} />
        <Route path="/justicia/insolvencia" element={<CasesPage type="Insolvencia" />} />
        <Route path="/justicia/auxiliares" element={<AuxPage />} />
        <Route path="/justicia/paginas" element={<PagesWatchPage />} />
        <Route path="/justicia/tramites" element={<ProceduresPage />} />
        <Route path="/supervision/sociedades" element={<SocietiesPage />} />
        <Route path="/supervision/sociedades/:id" element={<SocietyDetailPage />} />
        <Route path="/supervision/alertas" element={<AlertsPage />} />
        <Route path="/supervision/xbrl" element={<XbrlPage />} />
        <Route path="/datos/terceros" element={<ThirdPartiesPage />} />
        <Route path="/datos/asignaciones" element={<AssignmentsPage />} />
        <Route path="/datos/parametros" element={<ParamsPage />} />
        <Route path="/datos/cartera" element={<CarteraPage />} />
        <Route path="/datos/sanciones" element={<SanctionsPage />} />
        <Route path="/datos/calidad" element={<QualityPage />} />
        <Route path="/datos/analitica" element={<AnalyticsPage />} />
        <Route path="/datos/conexiones" element={<IntegrationsPage />} />
        <Route path="/tableros" element={<DashboardPage />} />
        <Route path="/informes" element={<ReportsPage />} />
        <Route path="/informes/:id" element={<ReportRoute />} />
        <Route path="/admin/contratos" element={<ContractsPage />} />
        <Route path="/admin/actividad" element={<ActivityPage />} />
        <Route path="/admin/seguridad" element={<SecurityPage />} />
        <Route path="/modernizacion" element={<MigrationPage />} />
        <Route path="/demo" element={<DemoPage />} />
        <Route path="/buscar" element={<SearchPage />} />
      </Route>
    </Routes>
  )
}
