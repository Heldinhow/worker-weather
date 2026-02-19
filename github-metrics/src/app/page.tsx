"use client"

import { useState } from "react"
import { RepoInput } from "@/components/repo-input"
import { ContributorTable } from "@/components/contributor-table"
import { Toaster } from "@/components/ui/toaster"
import { useToast } from "@/hooks/use-toast"
import type { ContributorsApiResponse } from "@/types/github"

export default function Home() {
  const [data, setData] = useState<ContributorsApiResponse | null>(null)
  const [expandedContributor, setExpandedContributor] = useState<string | null>(null)
  const { toast } = useToast()

  const handleData = (newData: ContributorsApiResponse) => {
    setData(newData)
    setExpandedContributor(null)
  }

  const handleError = (error: string) => {
    if (error) {
      toast({
        variant: "destructive",
        title: "Error",
        description: error,
      })
    }
  }

  const handleExpand = (login: string) => {
    setExpandedContributor(expandedContributor === login ? null : login)
  }

  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100">
      <div className="container mx-auto px-4 py-12 max-w-6xl">
        <header className="mb-12 text-center">
          <h1 className="text-4xl font-bold mb-3 bg-gradient-to-r from-zinc-100 to-zinc-500 bg-clip-text text-transparent">
            GitHub Contributors
          </h1>
          <p className="text-zinc-400 text-lg">
            Analyze contributor statistics across multiple repositories
          </p>
        </header>

        <div className="flex justify-center mb-12">
          <RepoInput onData={handleData} onError={handleError} />
        </div>

        {data && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <span className="text-zinc-400">
                  {data.totalRepos} repositories
                </span>
                <span className="text-zinc-600">|</span>
                <span className="text-zinc-400">
                  {data.totalContributors} contributors
                </span>
              </div>
            </div>
            <ContributorTable
              data={data.contributors}
              expandedContributor={expandedContributor}
              onExpand={handleExpand}
            />
          </div>
        )}
      </div>
      <Toaster />
    </div>
  )
}
