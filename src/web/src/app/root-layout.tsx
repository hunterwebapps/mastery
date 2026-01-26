import { Outlet } from 'react-router-dom'

export function RootLayout() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header className="border-b border-slate-800 bg-slate-900">
        <nav className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4">
          <a href="/" className="text-xl font-bold text-white">
            Mastery
          </a>
          <div className="flex gap-6">
            <a href="/" className="text-slate-300 hover:text-white">
              Dashboard
            </a>
            <a href="/goals" className="text-slate-300 hover:text-white">
              Goals
            </a>
            <a href="/habits" className="text-slate-300 hover:text-white">
              Habits
            </a>
            <a href="/check-in" className="text-slate-300 hover:text-white">
              Check-in
            </a>
          </div>
        </nav>
      </header>
      <main className="mx-auto max-w-7xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}
