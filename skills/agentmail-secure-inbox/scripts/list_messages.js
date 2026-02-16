#!/usr/bin/env node
const { getClient, arg, asInt } = require('./_agentmail_client');

(async () => {
  const inbox = arg('inbox', 'waddbot@agentmail.to');
  const limit = asInt(arg('limit', '10'), 10);

  const client = getClient();
  const page = await client.inboxes.messages.list(inbox, { limit });
  const items = page?.data || page?.items || [];

  const slim = items.slice(0, limit).map((m) => ({
    messageId: m.messageId || m.id || null,
    threadId: m.threadId || null,
    from: m.from || null,
    subject: m.subject || null,
    receivedAt: m.createdAt || m.receivedAt || null,
  }));

  console.log(JSON.stringify({ action: 'list', inbox, count: slim.length, messages: slim }, null, 2));
})();
