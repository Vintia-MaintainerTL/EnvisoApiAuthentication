# Repo map (for Claude)

Human-facing docs are in `README.md`. This file is a quick-nav index plus the cross-folder
facts that only became visible by reading all five samples side by side. Each folder also
has its own `CLAUDE.md` with per-sample detail (auto-loaded when working in that folder).

| Folder | Status | Notes file |
|---|---|---|
| `.net/` | Modernized (net8.0), treated as the reference | `.net/CLAUDE.md` |
| `javascript/` | Reviewed | `javascript/CLAUDE.md` |
| `php/` | Reviewed | `php/CLAUDE.md` |
| `python/` | Reviewed | `python/CLAUDE.md` |
| `Postman/` | Reviewed | `Postman/CLAUDE.md` (has its own human-facing `README.md` too) |

## Docker
`.net`, `php`, `python`, `javascript` each have a `Dockerfile`; root `docker-compose.yml` +
`.env.example` tie them together. `.net`/`php`/`python` are one-shot samples (run with
`docker compose run --rm <service>`, credentials via env vars — see `.env.example`),
`javascript` serves the demo page continuously (`docker compose up javascript`). Postman
was intentionally left out of this pass (not part of the reviewed scope).

Two of the harnesses needed real code changes, not just a Dockerfile, to run at all:
- **`php/src/ApiClient.php`'s `getAuthToken()` is `public`** — the only entry point into
  the class; see `php/CLAUDE.md` for how `run.php` calls it.
- **`.net`'s `Console/Program.cs` reads `ENVISO_API_KEY`/`ENVISO_PUBLIC_KEY`/
  `ENVISO_TENANT_SECRET`/`ENVISO_BASE_URL` env vars**, dropping the original interactive
  `Console.ReadLine()` prompts and checked-in demo-credential fallback.
- `php`'s container avoids needing a real Laravel app to satisfy the `Cache`/`Http`
  facades `ApiClient` depends on — see `php/CLAUDE.md` for how `run.php` fakes just enough
  of a Laravel container to make that work without pulling in `illuminate/cache`.

## Credential handling is now consistent: fail fast, no demo fallback
All three one-shot samples (`php`, `python`, `.net`) require `ENVISO_API_KEY`/
`ENVISO_PUBLIC_KEY`(/`ENVISO_TENANT_SECRET` for `php`/`.net`) to be set — each exits 1 with
`Missing required env var: <NAME>` on stderr if one is missing/empty, rather than silently
falling back to a checked-in demo credential (as `python` used to) or prompting
interactively (as `.net` used to). Only the non-secret base/login URL still has a sensible
default in each sample. `javascript` has no server-side credential handling (form fields
only); `Postman`'s environment file ships blank placeholders for the secret fields.

## The signing scheme, in one place
Every sample does the same three steps (see root `README.md` for the full explanation of
why "Signature" means encryption here, not a digital signature):
`{ApiKey}_{TimestampUtc}` → SHA-256 hex → RSA/PKCS#1-v1.5 *encrypt* with the tenant's public
key → base64. Timestamp format is `yyyy-MM-ddTHH:mm:ss.fffZ` (millisecond precision) in
every sample.

## Resolved: SHA-256 hex casing, login endpoint, and token refresh (2026-08-20)
All three were confirmed and fixed against
https://help.vintia.com/enviso/developers/authentication-api/en/index-en.html and a live
staging login:
- **Hex casing**: `.net`'s `Convert.ToHexString` produced uppercase hex while
  `javascript`/`php`/`python`/`Postman` all produce lowercase (the hex string is
  RSA-encrypted *as text*, so casing changes the signature entirely). `.net` now lowercases
  to match.
- **Login endpoint**: `.net` was posting to `resellingapi/v1/apis/login`; the docs confirm
  `authenticationapi/v1/login` is the only real (non-deprecated) endpoint, matching
  `php`/`python`/`Postman`. Fixing this also exposed a second, unrelated bug — `.net`'s base
  URL composition broke entirely once `ENVISO_BASE_URL` was set to just the root host (as
  `.env`/`.env.example` do) — see `.net/CLAUDE.md`.
- **Token refresh**: `authenticationapi/v1/renew` is documented as deprecated ("use the
  endpoint Log in" instead). `php` was the only sample calling it — removed in favor of
  re-logging-in on expiry, the same pattern `.net` already used for its own 401 case.

Two more real bugs surfaced while getting `.net`'s login to actually succeed against
staging credentials (both now fixed, see `.net/CLAUDE.md`): the login POST wasn't sending
the docs-required `x-api-key` header, and the login response was being deserialized
case-sensitively against Enviso's camelCase JSON, so the auth token silently came back
`null` instead of erroring.

## Other things worth reconciling if these samples get unified
- **`x-tenantsecretkey` header**: sent on the login call itself in `Postman` and `php`
  (though the docs only require it on subsequent authenticated requests, not login), sent
  only on subsequent authenticated requests in `.net`.
- **Terminology drift**: only `.net`'s naming/docs call out that `Signature` is envelope
  encryption, not a signature (per root README). `javascript`/`Postman` both literally name
  the function `sign`/"digital signature". Not wrong, just inconsistent language across
  samples — see root `README.md` for the canonical explanation to align on if that's ever
  done.
