import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import Header from '../../components/Header'

describe('Header', () => {
  it('renders site title and nav links', () => {
    render(<Header />)
    expect(screen.getByText(/Blacksmith/i)).toBeInTheDocument()
    expect(screen.getByText(/Product/i)).toBeInTheDocument()
  })
})
