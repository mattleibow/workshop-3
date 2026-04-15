---
name: Daily Digest
description: >
  Every weekday, creates a GitHub issue summarising all open issues and pull
  requests in this repository. Items are grouped by label with the total count,
  title, author, and how long each item has been open.
on:
  schedule:
    - cron: "0 8 * * 1-5"   # 08:00 UTC, Monday–Friday
  workflow_dispatch:
permissions:
  issues: read
  pull-requests: read
safe-outputs:
  mentions: false
  allowed-github-references: []

  create-issue:
    title-prefix: "Daily Digest –"
    labels: [digest]
    close-older-issues: true
    expires: 7d
timeout-minutes: 10
---

# Daily Digest

Use the actual current UTC date when generating the report title and content.

Search this repository for **all open issues** (type: issue) and **all open
pull requests** (type: pr). For each item collect:

- **Number** and **title**
- **Author** (login)
- **Age** — how long it has been open (e.g. "3 days", "2 weeks", "5 months")
- **Labels** assigned to it

## Report to produce

Create a GitHub issue titled **"Daily Digest – <YYYY-MM-DD>"** (today's UTC
date) with the following structure:

### Summary

Provide a one-sentence overview: total open issues, total open PRs, and the
number of distinct labels seen across all items.

### By Label

Group every open item (issues **and** PRs) by their labels.  For items that
carry more than one label, include them under every matching label section.

- If an item has **no labels**, place it under an `### Unlabelled` section.
- Within each label section, sub-divide with `#### Issues` and
  `#### Pull Requests` only if both types are present.
- For each item list it as:

  `- #<number> **<title>** — @<author> · open <age>`

- Show a **count badge** at the start of each label section heading, e.g.
  `### bug (4 items)`.

### Oldest Items

List the 5 oldest open items (any type) with their age, so the team can spot
long-running work at a glance.

<details>
<summary>View all open issues</summary>

List every open issue in one table:

| # | Title | Author | Age | Labels |
|---|-------|--------|-----|--------|

</details>

<details>
<summary>View all open pull requests</summary>

List every open pull request in one table:

| # | Title | Author | Age | Labels |
|---|-------|--------|-----|--------|

</details>

### Notes

- Format ages in human-readable terms (minutes → hours → days → weeks →
  months → years), always rounding to the nearest whole unit.
- Do not @mention anyone; use plain login names.
- Do not use `#<number>` cross-reference links — write plain text issue numbers
  instead (e.g. `issue 42` or `PR 17`) to avoid creating backlinks.