// BRT = UTC-3, fixed (Brazil abolished DST in 2019)
export function formatBrt(date: Date): string {
  const brt = new Date(date.getTime() - 3 * 3600_000);
  return brt.toISOString().replace("T", " ").slice(0, 23) + " BRT";
}

export function log(tag: string, msg: string): void {
  process.stdout.write(`${formatBrt(new Date())} [${tag}] ${msg}\n`);
}
