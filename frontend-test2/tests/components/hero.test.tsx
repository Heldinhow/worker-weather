import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import Hero from '../../components/Hero'

describe('Hero', () => {
  it('renders headline and CTA', () => {
    render(<Hero />)
    expect(screen.getByText(/Build faster with Blacksmith/i)).toBeInTheDocument()
    expect(screen.getByText(/Get started/i)).toBeInTheDocument()
  })
})
