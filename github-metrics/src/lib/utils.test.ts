import { describe, it, expect } from 'vitest'
import { parseRepoList, formatNumber, formatLines } from './utils'

describe('parseRepoList', () => {
  it('parses single repo', () => {
    const result = parseRepoList('facebook/react')
    expect(result).toHaveLength(1)
    expect(result[0]).toEqual({
      owner: 'facebook',
      repo: 'react',
      fullName: 'facebook/react'
    })
  })

  it('parses multiple repos', () => {
    const result = parseRepoList('facebook/react, vercel/next.js')
    expect(result).toHaveLength(2)
    expect(result[0]).toEqual({ owner: 'facebook', repo: 'react', fullName: 'facebook/react' })
    expect(result[1]).toEqual({ owner: 'vercel', repo: 'next.js', fullName: 'vercel/next.js' })
  })

  it('handles whitespace', () => {
    const result = parseRepoList(' facebook/react ,  vercel/next.js ')
    expect(result).toHaveLength(2)
  })

  it('throws on invalid format', () => {
    expect(() => parseRepoList('invalid')).toThrow()
    expect(() => parseRepoList('owner/')).toThrow()
    expect(() => parseRepoList('/repo')).toThrow()
  })

  it('handles empty input', () => {
    expect(parseRepoList('')).toEqual([])
    expect(parseRepoList('   ')).toEqual([])
  })
})

describe('formatNumber', () => {
  it('formats numbers with commas', () => {
    expect(formatNumber(1000)).toBe('1,000')
    expect(formatNumber(1000000)).toBe('1,000,000')
    expect(formatNumber(0)).toBe('0')
  })
})

describe('formatLines', () => {
  it('adds + prefix for positive numbers', () => {
    expect(formatLines(100)).toBe('+100')
    expect(formatLines(1000)).toBe('+1,000')
  })

  it('keeps - prefix for negative numbers', () => {
    expect(formatLines(-100)).toBe('-100')
    expect(formatLines(-1000)).toBe('-1,000')
  })
})
