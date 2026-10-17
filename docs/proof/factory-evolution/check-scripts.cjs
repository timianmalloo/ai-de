// Syntax-only: compile inline scripts without executing repository or browser code.
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../../..');
const files = ['docs/mockups/factory-workbench.html', 'docs/proposals/local-software-factory-interactive.html'];
let count = 0;
for (const relative of files) {
  const html = fs.readFileSync(path.join(root, relative), 'utf8');
  for (const script of html.matchAll(/<script>([\s\S]*?)<\/script>/g)) {
    new Function(script[1]);
    count++;
  }
}
if (count !== 2) throw new Error('Expected two nonempty inline application scripts');
console.log(`${count} inline application scripts parse; none executed by this check.`);
