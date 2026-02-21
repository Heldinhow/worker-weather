import React from 'react'

export default function Hero() {
  return (
    <section className="bg-gray-50">
      <div className="max-w-6xl mx-auto py-24 px-6 text-center">
        <h2 className="text-5xl font-extrabold">Build faster with Blacksmith</h2>
        <p className="mt-4 text-lg text-gray-600">An opinionated toolkit for modern teams.</p>
        <div className="mt-8">
          <a className="inline-block bg-black text-white px-6 py-3 rounded-md" href="#">Get started</a>
        </div>
      </div>
    </section>
  )
}
