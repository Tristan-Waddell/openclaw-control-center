#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const { AgentMailClient } = require('agentmail');

function loadEnvFromWorkspace() {
  const envPath = '/root/.openclaw/workspace/.env';
  if (!fs.existsSync(envPath)) return;
  const lines = fs.readFileSync(envPath, 'utf8').split(/\r?\n/);
  for (const line of lines) {
    if (!line || line.trim().startsWith('#') || !line.includes('=')) continue;
    const i = line.indexOf('=');
    const k = line.slice(0, i).trim();
    const v = line.slice(i + 1).trim();
    if (!(k in process.env)) process.env[k] = v;
  }
}

function getClient() {
  loadEnvFromWorkspace();
  const key = process.env.AGENTMAIL_API_KEY;
  if (!key) {
    throw new Error('AGENTMAIL_API_KEY not found. Add it to /root/.openclaw/workspace/.env');
  }
  return new AgentMailClient({ apiKey: key });
}

function arg(name, fallback = undefined) {
  const idx = process.argv.indexOf(`--${name}`);
  if (idx === -1) return fallback;
  return process.argv[idx + 1];
}

function asInt(v, dflt) {
  const n = parseInt(v, 10);
  return Number.isFinite(n) ? n : dflt;
}

module.exports = { getClient, arg, asInt };
