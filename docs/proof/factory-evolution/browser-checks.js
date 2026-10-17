// Run in the CUA browser REPL with an authorized loopback mockup tab.
// This checks the HTML exploration, not native WPF or live agent behavior.
async function checkFactoryExploration(tab) {
  const results = [];
  const check = (name, condition) => {
    if (!condition) throw new Error(name);
    results.push({ name, passed: true });
  };
  await tab.reload();
  await tab.playwright.locator('#reset').click();
  await tab.playwright.locator('#scope').selectOption('change');
  await tab.playwright.locator('[data-nav="review"]').click();
  await tab.playwright.locator('#prQuestion').fill('How is encryption implemented?');
  await tab.playwright.getByRole('button', { name: 'Ask this PR', exact: true }).click();
  check('Unsupported PR question is not invented', (await tab.playwright.locator('#questionAnswers').innerText()).includes('Not established in available evidence'));
  await tab.playwright.locator('#scope').selectOption('repo');
  await tab.playwright.locator('[data-action="question-evidence:0"]').click();
  check('Old answer retains original scope', (await tab.playwright.locator('#drawerBody').innerText()).includes('Current change'));
  await tab.playwright.locator('#closeDrawer').click();
  check('Evidence closes to its opener', await tab.playwright.evaluate(() => document.activeElement.getAttribute('data-action') === 'question-evidence:0'));
  await tab.playwright.locator('#state').selectOption('stale');
  check('Stale answer cannot run', !(await tab.playwright.getByRole('button', { name: 'Ask this PR', exact: true }).isEnabled()));
  await tab.playwright.locator('#state').selectOption('ready');
  await tab.playwright.locator('[data-nav="understand"]').click();
  await tab.playwright.locator('#diagramType').selectOption('sequence');
  check('Sequence uses four lifelines and five ordered interactions', await tab.playwright.evaluate(() => document.querySelectorAll('svg .node').length === 4 && document.querySelectorAll('#content details li').length === 5));
  await tab.playwright.locator('#diagramType').selectOption('data');
  check('ERM and accessible table agree', await tab.playwright.evaluate(() => document.querySelector('svg title').textContent.includes('Proposed conceptual ERM') && document.querySelectorAll('#content details tbody tr').length === 6));
  await tab.playwright.locator('[data-nav="factory"]').click();
  await tab.playwright.getByRole('button', { name: 'Prompt editor', exact: true }).click();
  await tab.playwright.locator('#findPrompt').fill('unknown delivery state');
  await tab.playwright.locator('#replacePrompt').fill('explicitly unresolved state');
  await tab.playwright.getByRole('button', { name: 'Preview multi-edit', exact: true }).click();
  check('Formatted-boundary preview counts one match', (await tab.playwright.locator('#drawerBody').innerText()).includes('1 matches'));
  await tab.playwright.getByRole('button', { name: 'Apply previewed edit', exact: true }).click();
  check('Apply uses the same formatted-boundary match', (await tab.playwright.locator('#richPrompt').innerText()).includes('explicitly unresolved state'));
  check('Editor operation exposes undo', await tab.playwright.getByRole('button', { name: 'Undo editor operation', exact: true }).isEnabled());
  await tab.playwright.getByRole('button', { name: 'Undo editor operation', exact: true }).click();
  check('Undo restores original phrase', (await tab.playwright.locator('#richPrompt').innerText()).includes('unknown delivery state'));
  await tab.playwright.locator('#richPrompt').fill('Unsent draft must survive stack navigation.');
  await tab.playwright.getByRole('button', { name: '← Back', exact: true }).click();
  await tab.playwright.getByRole('button', { name: 'Forward →', exact: true }).click();
  check('Back and forward retain scratch draft', (await tab.playwright.locator('#richPrompt').innerText()).includes('Unsent draft must survive'));
  check('No send is enabled by editing or navigation', !(await tab.playwright.getByRole('button', { name: 'Send · unavailable in demo', exact: true }).isEnabled()));
  return results;
}
