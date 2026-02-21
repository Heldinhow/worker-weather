import React from 'react'
import FeatureCard from './FeatureCard'

export default function FeatureGrid(){
  const features = [
    { title: 'Fast build', desc: 'Preconfigured build pipelines.' },
    { title: 'Reusable components', desc: 'Design system ready.' },
    { title: 'Integrations', desc: 'Connect to popular tools.' },
  ]

  return (
    <section className="py-16">
      <div className="max-w-6xl mx-auto px-6 grid grid-cols-1 md:grid-cols-3 gap-6">
        {features.map(f => (
          <FeatureCard key={f.title} title={f.title} desc={f.desc} />
        ))}
      </div>
    </section>
  )
}
