---
name: lead-finder-general
description: Find and qualify B2B/B2G sales leads from public web sources, then produce a clean outreach-ready lead sheet with source links and confidence notes. Use when the user asks for prospect lists, lead scraping, TAM seed lists, buyer contact discovery, or role-based account targeting across industries.
---

# Lead Finder (General)

Build practical lead lists quickly and transparently.

## Workflow
1. Confirm targeting inputs: ICP, geography, role titles, industry, minimum lead count, and required columns.
2. Build source map before scraping:
   - Industry directories
   - Membership organizations
   - Government/public registries
   - Company/team/contact pages
3. Collect leads with source URL for every row.
4. Normalize fields and deduplicate.
5. Score confidence (High/Med/Low) based on contact directness and recency.
6. Deliver CSV + short notes with caveats and next-step enrichment plan.

## Minimum output schema
Use this default unless user specifies otherwise:
- lead_name
- title
- organization
- location
- email
- phone
- website
- source_url
- lead_type
- confidence
- notes

## Data quality rules
- Never fabricate names, titles, emails, or phones.
- If person-level contact is unavailable, use role-based/org-level contact and mark it in `notes`.
- Keep one source URL per row (best supporting source).
- Prefer official pages over third-party aggregators.

## Prioritization rules
Score higher when:
- Role aligns with buying authority.
- Organization size/segment matches ICP.
- Contact path is direct (named email/phone).
- Signals indicate current need (initiative, policy, growth, pain point).

## Required deliverables
- `leads.csv` (or user-named file)
- `lead-gen-notes.md` with:
  - targeting assumptions
  - sources used
  - quality caveats
  - suggested outreach sequence

## Quick templates
See:
- `references/query-patterns.md` for reusable search prompts/patterns
- `references/qualification-rubric.md` for lead scoring
- `assets/lead_sheet_template.csv` for starter format
