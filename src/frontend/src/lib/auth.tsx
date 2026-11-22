import { createContext, useContext, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from './api'
import type { Session } from './types'

type AuthCtx = {
  session?: Session
  loading: boolean
  login: (userName: string) => Promise<void>
  logout: () => Promise<void>
  can: (code: string) => boolean
}

const Ctx = createContext<AuthCtx | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const qc = useQueryClient()
  const me = useQuery({
    queryKey: ['me'],
    queryFn: () => api.get<Session>('/api/auth/me'),
    retry: false,
  })

  const loginMut = useMutation({
    mutationFn: (userName: string) => api.post<Session>('/api/auth/login', { userName, password: 'demo' }),
    onSuccess: (s) => qc.setQueryData(['me'], s),
  })
  const logoutMut = useMutation({
    mutationFn: () => api.post('/api/auth/logout'),
    onSuccess: () => {
      qc.clear()
    },
  })

  const value: AuthCtx = {
    session: me.data,
    loading: me.isLoading,
    login: async (userName) => { await loginMut.mutateAsync(userName) },
    logout: async () => { await logoutMut.mutateAsync() },
    can: (code) => !!me.data?.permissions.includes(code),
  }
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>
}

export function useAuth() {
  const ctx = useContext(Ctx)
  if (!ctx) throw new Error('auth')
  return ctx
}
