import { spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const result = spawnSync('dotnet', ['list', 'GodForge-BE/GodForge.sln', 'package', '--vulnerable', '--include-transitive', '--format', 'json', '--output-version', '1'], { cwd: root, encoding: 'utf8' });
if (result.error) throw result.error;
if (result.status !== 0) throw new Error(`Dependency audit failed: dotnet exited with ${result.status}.`);
const report = JSON.parse(result.stdout);
if (report.version !== 1 || !Array.isArray(report.projects) || report.projects.length === 0 || report.logs?.some(entry => entry.level?.toLowerCase() === 'error')) {
  throw new Error('Dependency audit returned an incomplete or failed report.');
}
const findings = report.projects.flatMap(project => (project.frameworks ?? []).flatMap(framework =>
  [...(framework.topLevelPackages ?? []), ...(framework.transitivePackages ?? [])].flatMap(pkg =>
    (pkg.vulnerabilities ?? []).map(vulnerability => ({ package: pkg.id, severity: vulnerability.severity })))));
const blocking = findings.filter(finding => ['high', 'critical'].includes(finding.severity?.toLowerCase()));
if (blocking.length) {
  console.error(JSON.stringify(blocking));
  throw new Error(`Dependency audit found ${blocking.length} High/Critical findings.`);
}
console.log(`Dependency audit passed (${report.projects.length} projects; ${findings.length} reported findings, no High/Critical findings).`);
