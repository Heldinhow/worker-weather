import { NextRequest, NextResponse } from 'next/server';
import { parseRepoList } from '@/lib/utils';
import { fetchContributors, fetchContributorStats } from '@/lib/github';
import type { ContributorWithStats, RepoStats, ContributorsApiResponse } from '@/types/github';

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const reposParam = searchParams.get('repos');

  if (!reposParam) {
    return NextResponse.json(
      { error: 'Missing required query parameter: repos' },
      { status: 400 }
    );
  }

  let parsedRepos;
  try {
    parsedRepos = parseRepoList(reposParam);
  } catch (error) {
    return NextResponse.json(
      { error: error instanceof Error ? error.message : 'Invalid repos parameter' },
      { status: 400 }
    );
  }

  if (parsedRepos.length === 0) {
    return NextResponse.json(
      { error: 'No valid repos provided' },
      { status: 400 }
    );
  }

  const contributorsMap = new Map<string, ContributorWithStats>();
  const processedRepos: string[] = [];

  for (const parsedRepo of parsedRepos) {
    try {
      const contributors = await fetchContributors(parsedRepo.owner, parsedRepo.repo);

      for (const contributor of contributors) {
        const stats = await fetchContributorStats(
          parsedRepo.owner,
          parsedRepo.repo,
          contributor.login
        );

        const commits = stats?.total || 0;
        let additions = 0;
        let deletions = 0;

        if (stats?.weeks) {
          for (const week of stats.weeks) {
            additions += week.a;
            deletions += week.d;
          }
        }

        const repoStats: RepoStats = {
          repo: parsedRepo.repo,
          owner: parsedRepo.owner,
          commits,
          additions,
          deletions,
        };

        const existing = contributorsMap.get(contributor.login);
        if (existing) {
          existing.totalCommits += commits;
          existing.totalAdditions += additions;
          existing.totalDeletions += deletions;
          existing.repos.push(repoStats);
        } else {
          contributorsMap.set(contributor.login, {
            login: contributor.login,
            id: contributor.id,
            avatar_url: contributor.avatar_url,
            html_url: contributor.html_url,
            totalCommits: commits,
            totalAdditions: additions,
            totalDeletions: deletions,
            repos: [repoStats],
          });
        }
      }

      processedRepos.push(parsedRepo.fullName);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      
      if (errorMessage.includes('rate limit')) {
        return NextResponse.json(
          { error: 'GitHub API rate limit exceeded. Please try again later.' },
          { status: 429 }
        );
      }

      return NextResponse.json(
        { error: `Failed to fetch data for ${parsedRepo.fullName}: ${errorMessage}` },
        { status: 500 }
      );
    }
  }

  const contributors = Array.from(contributorsMap.values()).sort(
    (a, b) => b.totalCommits - a.totalCommits
  );

  const response: ContributorsApiResponse = {
    contributors,
    repos: processedRepos,
    totalRepos: processedRepos.length,
    totalContributors: contributors.length,
  };

  return NextResponse.json(response);
}
