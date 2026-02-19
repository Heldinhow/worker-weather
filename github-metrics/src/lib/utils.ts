import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"
import type { ParsedRepo } from "@/types/github"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function parseRepoList(input: string): ParsedRepo[] {
  if (!input.trim()) {
    return []
  }

  const repos = input.split(",").map((r) => r.trim()).filter(Boolean)

  return repos.map((repoString) => {
    const parts = repoString.split("/")
    if (parts.length !== 2 || !parts[0] || !parts[1]) {
      throw new Error(`Invalid repo format: ${repoString}. Expected: owner/repo`)
    }
    return {
      owner: parts[0],
      repo: parts[1],
      fullName: repoString,
    }
  })
}

export function formatNumber(num: number): string {
  return num.toLocaleString('en-US')
}

export function formatLines(num: number): string {
  const formatted = num.toLocaleString('en-US')
  return num >= 0 ? `+${formatted}` : formatted
}
