import React from 'react'

export default function FeatureCard({ title, desc }:{ title: string, desc: string }){
  return (
    <div className="p-6 border rounded-md">
      <h3 className="font-semibold text-lg">{title}</h3>
      <p className="mt-2 text-gray-600">{desc}</p>
    </div>
  )
}
