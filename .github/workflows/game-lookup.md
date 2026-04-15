---
name: Game Lookup
description: >
  ChatOps slash command: post /game-lookup <name> in any issue comment to fetch
  free-to-play game details from the FreeToGame API and reply with a formatted
  Markdown summary.
on:
  slash_command:
    name: game-lookup
    events: [issue_comment]
  roles: all
  reaction: eyes
  status-comment: true
permissions:
  issues: read
network:
  allowed:
    - defaults
    - www.freetogame.com
checkout: false
rate-limit:
  max: 5
  window: 60
timeout-minutes: 5
safe-outputs:
  add-comment:
    max: 1
---

# Game Lookup

A user has posted a `/game-lookup` slash command in an issue comment.
Your job is to look up the requested free-to-play game from the FreeToGame API
and reply to the issue with formatted game details.

## Instructions

### Step 1 — Extract the game name

You have been invoked by a `/game-lookup` slash command.  The triggering
comment body is provided in your context.  Parse the game name from that
comment: it is everything that appears after `/game-lookup` on the first line,
with leading/trailing whitespace removed.

- If the comment is just `/game-lookup` with nothing (or only whitespace) after
  it, skip the API calls and go straight to **Error: no game name**.
- Otherwise, trim the extracted name and use it as the search query.

### Step 2 — Fetch the game list

Run:

```bash
curl -sf "https://www.freetogame.com/api/games"
```

This returns a JSON array where every element has at least `id`, `title`,
`genre`, `platform`, `publisher`, `developer`, `release_date`,
`short_description`, and `game_url`.

If the request fails (non-zero exit code or empty response), skip to
**Error: API unavailable**.

### Step 3 — Find the best match

Search the returned array for the best title match against the query:

1. **Exact match** — case-insensitive equality (`title.lower() == query.lower()`)
2. **Starts-with match** — title starts with the query (case-insensitive)
3. **Contains match** — title contains the query as a substring (case-insensitive)

Use the first result from the highest-priority category that yields at least
one match.  If multiple results share the same priority, take the first one
(lowest array index).

If no match is found in any category, skip to **Error: not found**.

### Step 4 — Fetch game details

Using the `id` from the matched entry, run:

```bash
curl -sf "https://www.freetogame.com/api/game?id=<id>"
```

Extract from the response:

| Field              | JSON key            |
|--------------------|---------------------|
| Name               | `title`             |
| Genre              | `genre`             |
| Platform           | `platform`          |
| Publisher          | `publisher`         |
| Developer          | `developer`         |
| Release date       | `release_date`      |
| Short description  | `short_description` |
| FreeToGame URL     | `game_url`          |

Truncate `short_description` to at most 200 characters; append `…` if
truncated.

### Step 5 — Reply

Post a comment on the issue using the template below.

---

## Reply templates

### Success

```markdown
## 🎮 <title>

| | |
|---|---|
| **Genre** | <genre> |
| **Platform** | <platform> |
| **Publisher** | <publisher> |
| **Developer** | <developer> |
| **Release Date** | <release_date> |

<short_description (≤200 chars, ending with … if truncated)>

[🔗 View on FreeToGame](<game_url>)
```

### Error: no game name

```markdown
❌ **No game name provided.**

**Usage:** `/game-lookup <game name>`
**Example:** `/game-lookup Fortnite`
```

### Error: not found

```markdown
❌ **No game found matching "< query >".**

Please check the spelling or try a broader search term.
Browse the full catalogue at [FreeToGame](https://www.freetogame.com/).
```

### Error: API unavailable

```markdown
❌ **Could not reach the FreeToGame API.**

The service may be temporarily unavailable.  Please try again in a few minutes.
```
