#!/usr/bin/env node
const { getClient, arg } = require('./_agentmail_client');

(async () => {
  const from = arg('from', 'waddbot@agentmail.to');
  const to = arg('to');
  const subject = arg('subject', '(no subject)');
  const text = arg('text', '');

  if (!to) throw new Error('--to is required');

  const client = getClient();
  const res = await client.inboxes.messages.send(from, {
    to: [to],
    subject,
    text,
  });

  const out = {
    action: 'send',
    from,
    to,
    subject,
    messageId: res?.messageId || null,
    threadId: res?.threadId || null,
    security: 'No links/attachments auto-processed. Explicit send requested.',
  };
  console.log(JSON.stringify(out, null, 2));
})();
