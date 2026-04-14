---
on:
  schedule:
    - cron: "0 8 * * 1-5"
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
    expires: 7
---

# Daily Digest

Generate a daily digest issue summarising all currently open issues and pull requests in this repository.

## Instructions

1. Use the GitHub API to fetch **all open issues** and **all open pull requests** in the repository `${{ github.repository }}`.
   - Issues and pull requests without any label should be grouped under **"No Label"**.
   - A single item may carry multiple labels — include it once under its first label (or under "No Label" if it has none).

2. For each item collect:
   - **Title** (linked to the item URL)
   - **Number** (e.g. `#42`)
   - **Author** (`@`-mention escaped — do not trigger notifications)
   - **Age** — how long it has been open, expressed as a human-friendly duration such as `3 days`, `2 weeks`, `1 month`.

3. Compute summary statistics:
   - Total open issues count
   - Total open pull requests count

4. Create a GitHub issue using `create-issue` with the following content.

## Output Format

Title: `Daily Digest – <YYYY-MM-DD>` where the date is today's UTC date.

Body structure (use `###` and lower for all headers):

```
### Summary

- 📋 Open issues: <N>
- 🔀 Open pull requests: <N>
- 🗓️ Generated: <YYYY-MM-DD UTC>

### Open Issues by Label

#### <Label Name>
| # | Title | Author | Open for |
|---|-------|--------|----------|
| #N | [Title](url) | author | X days |

#### No Label
| # | Title | Author | Open for |
|---|-------|--------|----------|

### Open Pull Requests by Label

#### <Label Name>
| # | Title | Author | Open for |
|---|-------|--------|----------|

#### No Label
| # | Title | Author | Open for |
|---|-------|--------|----------|
```

- If there are no open issues or no open pull requests, include the section with a brief note: _No open items._
- Do not mention yourself (@copilot or any bot username) anywhere in the output.
- Do not use `@username` mentions that would trigger GitHub notifications; write plain usernames instead.
- Do not use `#N` cross-reference links that would create backlinks on the referenced items; use plain text issue numbers.
