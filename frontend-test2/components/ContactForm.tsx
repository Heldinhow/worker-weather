import React, { useState } from 'react'

export default function ContactForm(){
  const [email, setEmail] = useState('')
  const [done, setDone] = useState(false)

  async function submit(e: React.FormEvent){
    e.preventDefault()
    try {
      const res = await fetch('/api/contact', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email })
      })
      if (res.ok) setDone(true)
    } catch (e) {
      console.error(e)
    }
  }

  if (done) return <div>Thanks!</div>

  return (
    <form onSubmit={submit} className="max-w-md mx-auto">
      <label className="block">Email</label>
      <input className="border p-2 w-full" value={email} onChange={e => setEmail(e.target.value)} />
      <button className="mt-4 bg-black text-white px-4 py-2 rounded" type="submit">Submit</button>
    </form>
  )
}
