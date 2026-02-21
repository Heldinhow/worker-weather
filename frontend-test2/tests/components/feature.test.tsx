import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import FeatureGrid from '../../components/FeatureGrid'

describe('FeatureGrid', () => {
  it('renders feature titles', () => {
    render(<FeatureGrid />)
    expect(screen.getByText(/Fast build/i)).toBeInTheDocument()
    expect(screen.getByText(/Reusable components/i)).toBeInTheDocument()
  })
})
