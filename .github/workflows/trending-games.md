---
name: Trending Free Games
description: >
  Every weekday, fetches the top 15 most popular free-to-play games from the
  FreeToGame API and creates a GitHub issue with a Markdown table summary for
  the Tailspin Toys team.
on:
  schedule: daily on weekdays
  workflow_dispatch:
permissions:
  issues: read
network:
  allowed:
    - defaults
    - www.freetogame.com
checkout: false
timeout-minutes: 10
safe-outputs:
  mentions: false
  allowed-github-references: []
  create-issue:
    title-prefix: "🎮 Trending Free Games –"
    labels: [digest, games]
    close-older-issues: true
    expires: 14
---

# Trending Free Games Digest

Your task is to fetch the current list of popular free-to-play games from the
FreeToGame API and create a GitHub issue summarising the top 15 for the
Tailspin Toys team.

## Step 1 — Fetch the popularity-sorted game list

Run:

```bash
curl -sf "https://www.freetogame.com/api/games?sort-by=popularity"
```

This returns a JSON array sorted by popularity (most popular first).  Each
element contains at minimum: `id`, `title`, `genre`, `platform`, `publisher`,
`developer`, `release_date`, `short_description`, and `freetogame_profile_url`.

If the request fails, stop and emit a `report_incomplete` output explaining
that the FreeToGame API was unreachable.

## Step 2 — Select the top 15

Take the first 15 elements from the returned array.  These are the games you
will include in the issue.

## Step 3 — Determine today's date

Use the current UTC date formatted as `YYYY-MM-DD` for the issue title and
intro paragraph.

## Step 4 — Create the issue

Create a GitHub issue titled **"🎮 Trending Free Games – \<YYYY-MM-DD\>"**
(today's UTC date) with the structure below.

---

## Issue structure

### Intro paragraph

Begin the issue body with a short paragraph (2–3 sentences) explaining that
these are the 15 most popular free-to-play games currently listed on
FreeToGame, ordered by popularity, and that the data was fetched on today's
date.

### Top 15 table

Immediately after the intro, include a Markdown table with these columns in
order:

| # | Game | Genre | Platform | Publisher | Release Date | Page |
|---|------|-------|----------|-----------|--------------|------|

- **#** — rank (1–15)
- **Game** — the `title` value, plain text
- **Genre** — the `genre` value
- **Platform** — the `platform` value
- **Publisher** — the `publisher` value
- **Release Date** — the `release_date` value
- **Page** — a Markdown link `[FreeToGame](freetogame_profile_url)` using
  the `freetogame_profile_url` field

### Footer metadata

End the issue body with a small metadata block:

```
---
_Source: [FreeToGame](https://www.freetogame.com/) · Sorted by popularity · Top 15 of <total> games fetched_
```

Replace `<total>` with the actual count of games returned by the API.

---

## Formatting rules

- Use `###` (h3) or lower for any section headings — never `##` or `#`
- Do not @mention anyone
- Do not use `#<number>` cross-references
- Keep the table compact; do not truncate any field values
