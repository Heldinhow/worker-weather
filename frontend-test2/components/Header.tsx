import React from 'react'
import Link from 'next/link'

export default function Header() {
  return (
    <header className="py-4">
      <div className="max-w-6xl mx-auto flex items-center justify-between px-6">
        <Link href="/" className="text-xl font-bold">
          Blacksmith
        </Link>
        <nav>
          <ul className="flex gap-6">
            <li><Link href="#">Product</Link></li>
            <li><Link href="#">Docs</Link></li>
            <li><Link href="#">Contact</Link></li>
          </ul>
        </nav>
      </div>
    </header>
  )
}
