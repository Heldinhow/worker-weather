// GitHub API Types
export interface GitHubContributor {
  login: string;
  id: number;
  node_id: string;
  avatar_url: string;
  gravatar_id: string | null;
  url: string;
  html_url: string;
  followers_url: string;
  following_url: string;
  gists_url: string;
  starred_url: string;
  subscriptions_url: string;
  organizations_url: string;
  repos_url: string;
  events_url: string;
  received_events_url: string;
  type: string;
  site_admin: boolean;
  contributions: number;
}

export interface GitHubContributorStatsWeek {
  w: number;
  a: number;
  d: number;
  c: number;
}

export interface GitHubContributorStatsAuthor {
  login: string;
  id: number;
  node_id: string;
  avatar_url: string;
  html_url: string;
  type: string;
}

export interface GitHubContributorStats {
  author: GitHubContributorStatsAuthor;
  total: number;
  weeks: GitHubContributorStatsWeek[];
}

export interface ParsedRepo {
  owner: string;
  repo: string;
  fullName: string;
}

export interface RepoStats {
  repo: string;
  owner: string;
  commits: number;
  additions: number;
  deletions: number;
}

export interface ContributorWithStats {
  login: string;
  id: number;
  avatar_url: string;
  html_url: string;
  totalCommits: number;
  totalAdditions: number;
  totalDeletions: number;
  repos: RepoStats[];
}

export interface ContributorsApiResponse {
  contributors: ContributorWithStats[];
  repos: string[];
  totalRepos: number;
  totalContributors: number;
}
