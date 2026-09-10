import { readdirSync, existsSync, writeFileSync } from "fs";
import { join } from "path";

const base = "node_modules";
const overrides = { "style-mod": "src/style-mod.js", "w3c-keyname": "index.js", crelt: "index.js" };
const imports = {};

function add(name, rel) {
  const p = join(base, ...name.split("/"), rel);
  if (existsSync(p)) imports[name] = "./" + p.split("\\").join("/");
}

for (const scope of ["@codemirror", "@lezer"]) {
  for (const pkg of readdirSync(join(base, scope))) {
    add(`${scope}/${pkg}`, "dist/index.js");
  }
}
for (const [name, rel] of Object.entries(overrides)) add(name, rel);
add("codemirror", "dist/index.js");

writeFileSync("importmap.json", JSON.stringify({ imports }, null, 2));
console.log(Object.keys(imports).length, "entries written");
