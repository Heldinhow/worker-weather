import type { NextApiRequest, NextApiResponse } from 'next'

export default function handler(req: NextApiRequest, res: NextApiResponse){
  if (req.method !== 'POST') return res.status(405).end()
  const { email } = req.body || {}
  if (!email) return res.status(400).json({ ok: false, error: 'email required' })
  return res.status(200).json({ ok: true })
}
