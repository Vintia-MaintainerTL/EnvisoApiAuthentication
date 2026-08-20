# .net/ — Enviso auth sample (reference implementation)

This is the sample that's already been modernized/audited — see root `README.md` for the
full writeup. Login is verified working end-to-end against the real staging API (2026-08-20,
per https://help.vintia.com/enviso/developers/authentication-api/en/index-en.html) — see
"Fixed against the real API" below for what was actually broken before that.

- `src/Library/LoginGenerator.cs` — builds the signed login request:
  `{ApiKey}_{TimestampUtc}` → SHA-256 hex (lowercase, matching every other sample) →
  RSA/PKCS#1 v1.5 *encrypt* with the tenant's PEM public key (`RSAEncryptionPadding.Pkcs1`)
  → base64. Field is called `Signature` on the wire but this is envelope encryption, not a
  digital signature — doc'd in the XML comments there.
- `src/Library/EnvisoClient.cs` — login POST (`x-api-key` header + body), then authenticated
  GETs with `Bearer` token + `x-tenantsecretkey` + `x-api-key` headers. On a 401 it re-runs
  the full login rather than calling `authenticationapi/v1/renew` — that endpoint is
  documented as deprecated in favor of logging in again (see EnvisoClient.cs:79).
- The library only demonstrates login (`EnvisoClient.AuthToken`); the console previously
  also called a `GET .../resellingapi/v1/venues` demo endpoint, but that endpoint isn't
  covered by the authentication-API docs and 403'd even with a fully working login — it was
  removed (along with `VenuesUri`/`VenueModelDTO`) rather than left pointed at an unverified
  path. `GetAsync<TResponse>` remains as a general authenticated-GET helper for callers who
  do have a real endpoint to hit.

## Docker
`Dockerfile` (multi-stage `dotnet/sdk` build → `dotnet/runtime`) + `.dockerignore` added.
`Console/Program.cs`'s `Main()` reads `ENVISO_API_KEY`/`ENVISO_PUBLIC_KEY`/
`ENVISO_TENANT_SECRET` via `RequiredEnv()`, which writes `Missing required env var: <NAME>`
to stderr and exits 1 if any is unset/empty — no interactive prompt, no checked-in demo
credential fallback, matching `php`'s `requiredEnv()` fail-fast behavior. `ENVISO_BASE_URL`
still defaults to `EnvisoClient.DefaultBaseUrl` when unset (not a credential). Run via
`docker run --rm -e ENVISO_API_KEY=... -e ENVISO_PUBLIC_KEY="$(cat key.pem)" -e
ENVISO_TENANT_SECRET=... <image>`, or `docker compose run --rm dotnet` from the repo root.

## Fixed against the real API (2026-08-20)
Login here previously failed with a `403 Forbidden` against real staging credentials. Per
the official docs, four independent bugs were stacked on top of each other:
1. **Wrong endpoint entirely**: pointed at `resellingapi/v1/apis/login` instead of the
   documented `authenticationapi/v1/login` (matching `javascript`/`php`/`python`/`Postman`).
2. **Base-URL composition was broken by the env var itself**: `DefaultBaseUrl` used to bake
   in `resellingapi/v1/` and each endpoint appended a short relative path on top — but
   `ENVISO_BASE_URL` (as set in `.env`/`.env.example`, just the root host) overrode that
   entirely, producing a nonsensical URL whenever the env var was set at all. `BaseUri` is
   now just the root host; `LoginUri` appends the full `authenticationapi/v1/login` path
   itself, matching the convention `php` already used.
3. **Missing required header**: the login POST never sent `x-api-key` (only the later
   authenticated GETs did), even though the docs mark it required on login.
4. **Silent null auth token**: `LoginResponseDTO`'s PascalCase properties (`AuthToken`) were
   deserialized case-sensitively against Enviso's camelCase JSON (`authToken`), so the token
   silently came back `null` instead of erroring. Fixed via `PropertyNameCaseInsensitive`;
   also renamed `RefreshKey` → `RefreshToken` to match the real field name.
