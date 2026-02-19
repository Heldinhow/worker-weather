"use client"

import { useState, useMemo } from "react"
import { ArrowUpDown } from "lucide-react"
import type { ContributorWithStats } from "@/types/github"
import { formatNumber, formatLines } from "@/lib/utils"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Input } from "@/components/ui/input"

type SortKey = "login" | "totalCommits" | "totalAdditions" | "totalDeletions"
type SortDirection = "asc" | "desc"

interface ContributorTableProps {
  data: ContributorWithStats[]
  expandedContributor: string | null
  onExpand: (login: string) => void
}

export function ContributorTable({
  data,
  expandedContributor,
  onExpand,
}: ContributorTableProps) {
  const [search, setSearch] = useState("")
  const [sortKey, setSortKey] = useState<SortKey>("totalCommits")
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc")

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc")
    } else {
      setSortKey(key)
      setSortDirection("desc")
    }
  }

  const filteredAndSortedData = useMemo(() => {
    let result = [...data]

    if (search) {
      result = result.filter((contributor) =>
        contributor.login.toLowerCase().includes(search.toLowerCase())
      )
    }

    result.sort((a, b) => {
      let aValue: string | number
      let bValue: string | number

      switch (sortKey) {
        case "login":
          aValue = a.login
          bValue = b.login
          break
        case "totalCommits":
          aValue = a.totalCommits
          bValue = b.totalCommits
          break
        case "totalAdditions":
          aValue = a.totalAdditions
          bValue = b.totalAdditions
          break
        case "totalDeletions":
          aValue = a.totalDeletions
          bValue = b.totalDeletions
          break
      }

      if (typeof aValue === "string" && typeof bValue === "string") {
        return sortDirection === "asc"
          ? aValue.localeCompare(bValue)
          : bValue.localeCompare(aValue)
      }

      return sortDirection === "asc"
        ? (aValue as number) - (bValue as number)
        : (bValue as number) - (aValue as number)
    })

    return result
  }, [data, search, sortKey, sortDirection])

  const SortIcon = ({ column }: { column: SortKey }) => {
    if (sortKey !== column) return <ArrowUpDown className="ml-2 h-4 w-4 opacity-50" />
    return (
      <ArrowUpDown
        className={`ml-2 h-4 w-4 ${sortDirection === "asc" ? "rotate-180" : ""}`}
      />
    )
  }

  return (
    <div className="space-y-4">
      <Input
        placeholder="Search by login..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="max-w-sm bg-zinc-900 border-zinc-700 text-zinc-100 placeholder:text-zinc-500"
      />
      <div className="rounded-md border border-zinc-800">
        <Table>
          <TableHeader>
            <TableRow className="bg-zinc-900 hover:bg-zinc-900 border-zinc-800">
              <TableHead className="w-[50px]"></TableHead>
              <TableHead className="text-zinc-400">
                <button
                  onClick={() => handleSort("login")}
                  className="flex items-center hover:text-zinc-100 transition-colors"
                >
                  Login
                  <SortIcon column="login" />
                </button>
              </TableHead>
              <TableHead className="text-zinc-400">
                <button
                  onClick={() => handleSort("totalCommits")}
                  className="flex items-center hover:text-zinc-100 transition-colors"
                >
                  Total Commits
                  <SortIcon column="totalCommits" />
                </button>
              </TableHead>
              <TableHead className="text-zinc-400">
                <button
                  onClick={() => handleSort("totalAdditions")}
                  className="flex items-center hover:text-zinc-100 transition-colors"
                >
                  +Lines
                  <SortIcon column="totalAdditions" />
                </button>
              </TableHead>
              <TableHead className="text-zinc-400">
                <button
                  onClick={() => handleSort("totalDeletions")}
                  className="flex items-center hover:text-zinc-100 transition-colors"
                >
                  -Lines
                  <SortIcon column="totalDeletions" />
                </button>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredAndSortedData.map((contributor) => (
              <>
                <TableRow
                  key={contributor.login}
                  className="border-zinc-800 hover:bg-zinc-800/50 cursor-pointer"
                  onClick={() => onExpand(contributor.login)}
                >
                  <TableCell className="w-[50px]">
                    <Avatar className="h-8 w-8">
                      <AvatarImage src={contributor.avatar_url} alt={contributor.login} />
                      <AvatarFallback>{contributor.login.slice(0, 2).toUpperCase()}</AvatarFallback>
                    </Avatar>
                  </TableCell>
                  <TableCell className="font-medium text-zinc-100">
                    <a
                      href={contributor.html_url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="hover:underline"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {contributor.login}
                    </a>
                  </TableCell>
                  <TableCell className="text-zinc-300">{formatNumber(contributor.totalCommits)}</TableCell>
                  <TableCell className="text-green-400">{formatLines(contributor.totalAdditions)}</TableCell>
                  <TableCell className="text-red-400">{formatLines(contributor.totalDeletions)}</TableCell>
                </TableRow>
                {expandedContributor === contributor.login && (
                  <TableRow className="border-zinc-800 bg-zinc-900/50">
                    <TableCell colSpan={5} className="p-4">
                      <div className="space-y-2">
                        <p className="text-sm font-medium text-zinc-400">Per-repo breakdown:</p>
                        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
                          {contributor.repos.map((repo) => (
                            <div
                              key={`${repo.owner}/${repo.repo}`}
                              className="rounded bg-zinc-800 p-3 text-sm"
                            >
                              <p className="font-medium text-zinc-200">{repo.repo}</p>
                              <div className="mt-1 flex gap-3 text-zinc-400">
                                <span>{repo.commits} commits</span>
                                <span className="text-green-400">+{formatNumber(repo.additions)}</span>
                                <span className="text-red-400">-{formatNumber(repo.deletions)}</span>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    </TableCell>
                  </TableRow>
                )}
              </>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
