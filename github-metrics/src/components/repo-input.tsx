"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import type { ContributorsApiResponse } from "@/types/github"

interface RepoInputProps {
  onData: (data: ContributorsApiResponse) => void
  onError: (error: string) => void
}

export function RepoInput({ onData, onError }: RepoInputProps) {
  const [repos, setRepos] = useState("")
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!repos.trim()) {
      onError("Please enter at least one repository")
      return
    }

    setLoading(true)
    onError("")

    try {
      const response = await fetch(`/api/contributors?repos=${encodeURIComponent(repos)}`)
      
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.error || `Failed to fetch contributors (${response.status})`)
      }

      const data: ContributorsApiResponse = await response.json()
      onData(data)
    } catch (err) {
      onError(err instanceof Error ? err.message : "An unexpected error occurred")
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex w-full max-w-2xl gap-3">
      <div className="relative flex-1">
        <Input
          type="text"
          value={repos}
          onChange={(e) => setRepos(e.target.value)}
          placeholder="owner/repo1, owner/repo2, ..."
          disabled={loading}
          className="h-11 bg-secondary/50 border-border text-foreground placeholder:text-muted-foreground focus:ring-2 focus:ring-primary/50 focus:border-primary pr-10"
        />
        {loading && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2">
            <svg
              className="animate-spin h-4 w-4 text-muted-foreground"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              />
            </svg>
          </div>
        )}
      </div>
      <Button
        type="submit"
        disabled={loading}
        className="h-11 px-6 bg-primary text-primary-foreground hover:bg-primary/90 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {loading ? "Loading..." : "Fetch"}
      </Button>
    </form>
  )
}
