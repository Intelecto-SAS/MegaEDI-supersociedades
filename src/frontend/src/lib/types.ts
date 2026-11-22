export type Session = {
  userId: string
  userName: string
  displayName: string
  position: string
  department: string
  profileCode: string
  homeExperience: string
  regionalScope?: string | null
  roles: string[]
  permissions: string[]
  impersonatedBy?: string | null
  businessClock: string
}

export type Official = {
  id: string
  displayName: string
  position: string
  department: string
  profileCode: string
  homeExperience: string
  userName: string
}

export type Kpi = { id: string; label: string; value: string; hint: string; href?: string | null; tone: string }
export type WorkItem = { id: string; kind: string; title: string; status: string; priority: string; dueAt?: string | null; number?: string | null; caseId?: string | null; recordId?: string | null }
export type Notice = { id: string; title: string; detail: string; at: string; level: string }
export type Activity = { title: string; detail: string; at: string }
export type QuickAction = { label: string; href: string }

export type Home = {
  experience: string
  greeting: string
  kpis: Kpi[]
  pending: WorkItem[]
  cases: WorkItem[]
  deadlines: WorkItem[]
  notices: Notice[]
  activity: Activity[]
  actions: QuickAction[]
}

export type Page<T> = { items: T[]; total: number; page: number; pageSize: number }
