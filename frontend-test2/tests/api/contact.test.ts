import handler from '../../pages/api/contact'
import { createMocks } from 'node-mocks-http'

describe('POST /api/contact', () => {
  it('returns 200 for valid email', async () => {
    const { req, res } = createMocks({ method: 'POST', body: { email: 'a@b.com' } })
    await handler(req as any, res as any)
    expect(res._getStatusCode()).toBe(200)
  })
  it('returns 400 when missing email', async () => {
    const { req, res } = createMocks({ method: 'POST', body: {} })
    await handler(req as any, res as any)
    expect(res._getStatusCode()).toBe(400)
  })
})
