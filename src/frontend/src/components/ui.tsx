import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'
import { clsx } from 'clsx'

export function cn(...parts: Array<string | false | null | undefined>) {
  return clsx(parts)
}

export function Button({
  variant = 'primary',
  className,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'ghost' | 'gold' | 'danger' | 'plain' }) {
  const styles = {
    primary: 'bg-navy text-white hover:bg-navy-2',
    gold: 'bg-gold text-navy hover:brightness-105',
    ghost: 'bg-white border border-line text-ink hover:bg-sand',
    danger: 'bg-warn text-white hover:brightness-110',
    plain: 'text-navy hover:underline',
  }[variant]
  return (
    <button
      className={cn('inline-flex items-center justify-center gap-2 rounded-md px-3.5 py-2 text-sm font-medium disabled:opacity-50', styles, className)}
      {...props}
    />
  )
}

export function Input(props: InputHTMLAttributes<HTMLInputElement>) {
  return <input {...props} className={cn('w-full rounded-md border border-line bg-white px-3 py-2 text-sm', props.className)} />
}

export function Textarea(props: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea {...props} className={cn('w-full rounded-md border border-line bg-white px-3 py-2 text-sm min-h-28', props.className)} />
}

export function Select(props: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select {...props} className={cn('w-full rounded-md border border-line bg-white px-3 py-2 text-sm', props.className)} />
}

export function Card({ children, className }: { children: ReactNode; className?: string }) {
  return <section className={cn('rounded-xl border border-line bg-white p-5 shadow-[0_1px_0_rgba(11,31,51,0.04)]', className)}>{children}</section>
}

export function Badge({ children, tone = 'navy' }: { children: ReactNode; tone?: string }) {
  const map: Record<string, string> = {
    navy: 'bg-navy/10 text-navy',
    gold: 'bg-gold/15 text-navy',
    warn: 'bg-warn/10 text-warn',
    ok: 'bg-ok/10 text-ok',
    muted: 'bg-sand text-muted',
  }
  return <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium uppercase tracking-wide', map[tone] || map.navy)}>{children}</span>
}

export function PageHeader({ kicker, title, actions, children }: { kicker?: string; title: string; actions?: ReactNode; children?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        {kicker && <p className="text-[11px] uppercase tracking-[0.18em] text-gold mb-1">{kicker}</p>}
        <h1 className="serif text-3xl text-navy">{title}</h1>
        {children && <p className="mt-1 text-muted max-w-2xl">{children}</p>}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  )
}

export function Table({ headers, children }: { headers: string[]; children: ReactNode }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-line bg-white">
      <table className="w-full text-sm">
        <thead className="bg-sand/60 text-left text-[11px] uppercase tracking-wider text-muted">
          <tr>{headers.map((h) => <th key={h} className="px-3 py-2.5 font-medium">{h}</th>)}</tr>
        </thead>
        <tbody className="divide-y divide-line">{children}</tbody>
      </table>
    </div>
  )
}

export function Empty({ children }: { children: ReactNode }) {
  return <p className="text-sm text-muted py-8 text-center">{children}</p>
}

export function AssistBanner() {
  return (
    <p className="rounded-md border border-gold/40 bg-gold/10 px-3 py-2 text-xs text-navy">
      Motor local de asistencia — simulación basada en reglas y plantillas. Una persona confirma cada sugerencia.
    </p>
  )
}

export function StatusDot({ status }: { status: string }) {
  const tone = status.toLowerCase().includes('cerr') || status.toLowerCase().includes('aprob')
    ? 'ok'
    : status.toLowerCase().includes('crít') || status.toLowerCase().includes('mora') || status.toLowerCase().includes('reserv')
      ? 'warn'
      : 'navy'
  return <Badge tone={tone}>{status}</Badge>
}

export function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block text-muted">{label}</span>
      {children}
    </label>
  )
}
