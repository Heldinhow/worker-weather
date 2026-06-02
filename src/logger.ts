export function log(tag: string, msg: string): void {
  process.stdout.write(`${new Date().toISOString()} [${tag}] ${msg}\n`);
}
