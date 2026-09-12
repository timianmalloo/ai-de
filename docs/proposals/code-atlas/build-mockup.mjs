import {readFileSync, writeFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {dirname, join} from 'node:path';
import {performance} from 'node:perf_hooks';

const started = performance.now();
const root = dirname(fileURLToPath(import.meta.url));
const data = JSON.parse(readFileSync(join(root, 'terrace-evidence.json'), 'utf8'));
const template = readFileSync(join(root, 'mockup.template.html'), 'utf8');
if (!data.meta?.revision || data.files.length !== data.meta.counts.trackedFiles) {
  throw new Error('ATLAS_FIXTURE_INVALID: revision or inventory count is inconsistent');
}
if (template.split('@@TERRACE_DATA@@').length !== 2) {
  throw new Error('ATLAS_TEMPLATE_INVALID: expected exactly one fixture slot');
}
const html = template.replace('@@TERRACE_DATA@@', JSON.stringify(data).replaceAll('<', '\\u003c'));
writeFileSync(join(root, 'mockup.html'), html, 'utf8');
console.log(JSON.stringify({
  event: 'atlas.prototype.built', revision: data.meta.revision,
  tracked_files: data.files.length, embedded_sources: Object.keys(data.sources).length,
  output_bytes: Buffer.byteLength(html), duration_ms: Math.round(performance.now() - started)
}));
