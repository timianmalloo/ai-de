#!/usr/bin/env node
// A minimal MCP stdio JSON-RPC server for the PD-5 pin spike's fixture repository.
//
// One tool, `write_note`, which writes a file when called — so the fixture has a live `mcp__*`
// tool name for the pin's assertion (ADR-0035's "not closed here, named: mcp__* tools the
// adapter loads from settings ... independently of the frame's mcpServers: []"). Every inbound
// JSON-RPC message is logged to `mcp-calls.jsonl` next to this file, so "the pin held" can be
// measured as "this log stayed empty" rather than inferred from silence.
//
// Deliberately dependency-free (stdlib only) — this is throwaway spike scaffolding, not product
// code, and the Solution-Selection Ladder says a stdio JSON-RPC loop this small does not earn a
// framework.
"use strict";

const fs = require("fs");
const path = require("path");

const LOG_PATH = path.join(__dirname, "mcp-calls.jsonl");
const NOTE_PATH = path.join(__dirname, "note-from-mcp.txt");

function logCall(direction, message) {
  const line = JSON.stringify({ at: new Date().toISOString(), direction, message }) + "\n";
  fs.appendFileSync(LOG_PATH, line);
}

function send(message) {
  const line = JSON.stringify(message);
  logCall("sent", message);
  process.stdout.write(line + "\n");
}

const TOOLS = [
  {
    name: "write_note",
    description: "Writes a short note to a file in the fixture repository. Used only to prove an mcp__ tool call would be observed if the pin did not hold.",
    inputSchema: {
      type: "object",
      properties: {
        text: { type: "string", description: "The note text to write." },
      },
      required: ["text"],
    },
  },
];

function handleRequest(msg) {
  const { id, method, params } = msg;

  if (method === "initialize") {
    send({
      jsonrpc: "2.0",
      id,
      result: {
        protocolVersion: (params && params.protocolVersion) || "2024-11-05",
        capabilities: { tools: {} },
        serverInfo: { name: "pd5-fixture-mcp-server", version: "0.0.1" },
      },
    });
    return;
  }

  if (method === "tools/list") {
    send({ jsonrpc: "2.0", id, result: { tools: TOOLS } });
    return;
  }

  if (method === "tools/call") {
    const name = params && params.name;
    const args = (params && params.arguments) || {};

    if (name === "write_note") {
      const text = typeof args.text === "string" ? args.text : "";
      fs.writeFileSync(NOTE_PATH, text);
      send({
        jsonrpc: "2.0",
        id,
        result: { content: [{ type: "text", text: `wrote ${NOTE_PATH}` }], isError: false },
      });
      return;
    }

    send({
      jsonrpc: "2.0",
      id,
      result: { content: [{ type: "text", text: `unknown tool: ${String(name)}` }], isError: true },
    });
    return;
  }

  // Any other request this stub does not implement — answer with a benign empty result rather
  // than hanging the caller, but still logged (the log is the assertion's source of truth).
  if (id !== undefined) {
    send({ jsonrpc: "2.0", id, result: {} });
  }
}

let buf = "";
process.stdin.on("data", (chunk) => {
  buf += chunk.toString("utf8");
  let idx;
  while ((idx = buf.indexOf("\n")) >= 0) {
    const line = buf.slice(0, idx);
    buf = buf.slice(idx + 1);
    if (!line.trim()) continue;

    let msg;
    try {
      msg = JSON.parse(line);
    } catch (err) {
      logCall("recv-unparsed", { raw: line, error: String(err) });
      continue;
    }

    logCall("recv", msg);

    // A notification (no `id`) — e.g. notifications/initialized — gets no reply.
    if (msg.id === undefined && msg.method) {
      continue;
    }

    handleRequest(msg);
  }
});

process.stdin.on("end", () => process.exit(0));
