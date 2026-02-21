import './globals.css'
import type { ReactNode } from 'react'

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>
        <header className="p-6">
          <div className="max-w-6xl mx-auto">Blacksmith Clone</div>
        </header>
        <main>{children}</main>
        <footer className="p-6 mt-20 bg-gray-50">
          <div className="max-w-6xl mx-auto">© Blacksmith Clone</div>
        </footer>
      </body>
    </html>
  )
}
