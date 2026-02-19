import type {
  GitHubContributor,
  GitHubContributorStats,
  ParsedRepo,
} from '@/types/github';

const GITHUB_API_BASE = 'https://api.github.com';

function getHeaders(): HeadersInit {
  const headers: HeadersInit = {
    Accept: 'application/vnd.github.v3+json',
  };
  if (process.env.GITHUB_TOKEN) {
    headers.Authorization = `token ${process.env.GITHUB_TOKEN}`;
  }
  return headers;
}

function checkRateLimit(response: Response): void {
  const remaining = response.headers.get('x-ratelimit-remaining');
  const reset = response.headers.get('x-ratelimit-reset');
  if (remaining && parseInt(remaining, 10) === 0) {
    const resetDate = reset ? new Date(parseInt(reset, 10) * 1000) : 'unknown';
    throw new Error(`GitHub API rate limit exceeded. Resets at: ${resetDate}`);
  }
}

export function parseRepoString(repoString: string): ParsedRepo {
  const parts = repoString.split('/');
  if (parts.length !== 2 || !parts[0] || !parts[1]) {
    throw new Error(`Invalid repo string: ${repoString}. Expected format: owner/repo`);
  }
  return {
    owner: parts[0],
    repo: parts[1],
    fullName: repoString,
  };
}

export async function fetchContributors(
  owner: string,
  repo: string
): Promise<GitHubContributor[]> {
  const url = `${GITHUB_API_BASE}/repos/${owner}/${repo}/contributors?per_page=100`;
  const response = await fetch(url, { headers: getHeaders() });
  
  if (!response.ok) {
    throw new Error(`Failed to fetch contributors: ${response.status} ${response.statusText}`);
  }
  
  checkRateLimit(response);
  
  return response.json();
}

export async function fetchContributorStats(
  owner: string,
  repo: string,
  contributorLogin: string
): Promise<GitHubContributorStats | null> {
  const url = `${GITHUB_API_BASE}/repos/${owner}/${repo}/stats/contributors`;
  const response = await fetch(url, { headers: getHeaders() });
  
  if (!response.ok) {
    throw new Error(`Failed to fetch contributor stats: ${response.status} ${response.statusText}`);
  }
  
  checkRateLimit(response);
  
  const data: GitHubContributorStats[] = await response.json();
  return data.find((stat) => stat.author.login === contributorLogin) || null;
}
