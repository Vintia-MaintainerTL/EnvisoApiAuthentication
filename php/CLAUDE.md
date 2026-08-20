# php/ — Enviso auth sample

A Laravel-flavored client (`src/ApiClient.php`, namespace `Enviso`). Uses `Illuminate`
facades (`Cache`, `Http`, `Carbon`) directly, so it only runs inside a booted Laravel
app/container — not standalone PHP. `composer.json` pins `illuminate/http ^9.0.0`,
`phpseclib/phpseclib ^3.0`, `nesbot/carbon ^2.50.0`.

## Flow (`ApiClient::getAuthToken`, ApiClient.php:35)
1. No cached token, or the 1-hour `authTokenStillActive` marker has expired →
   `createNewToken()`: builds `{apiKey}_{timestamp}` (Carbon, `YYYY-MM-DDTHH:mm:ss.SSS\Z`),
   SHA-256 hashes it, encrypts with phpseclib3 `RSA::ENCRYPTION_PKCS1`, base64-encodes →
   POSTs to `{baseUrl}authenticationapi/v1/login`. Caches `authToken` for 30 days and
   refreshes the 1-hour `authTokenStillActive` marker.
2. Otherwise reuses the cached `authToken`.

Previously renewed via `authenticationapi/v1/renew` instead of re-logging-in once the
1-hour marker expired — removed (2026-08-20) once
https://help.vintia.com/enviso/developers/authentication-api/en/index-en.html confirmed
that endpoint is deprecated ("use the endpoint Log in" instead). Now just re-logs-in, same
pattern `.net`'s `EnvisoClient` already used for its own 401 case.

## Cross-sample notes (see root `index.md` for the full comparison)
- Login path is `authenticationapi/v1/login`, matching `python/run.py`, `.net`, and the
  Postman collection.

## Docker
`Dockerfile` + `run.php` + `.dockerignore` added so this sample runs standalone. Two things
worth knowing if you touch either:
- **`ApiClient::getAuthToken()` is `public`** — the only entry point into the class;
  `run.php` calls it directly.
- `run.php` does NOT boot a real Laravel app. `ApiClient` calls the `Cache`/`Http` facades
  statically, which normally requires a booted `Illuminate\Container\Container` with
  `illuminate/cache`'s `CacheManager` + a config repository bound in. Instead, `run.php`
  binds a tiny hand-rolled `InMemoryCache` (duck-typed `has`/`get`/`put`, no TTL config
  needed) to the `'cache'` key, and a plain `Illuminate\Http\Client\Factory` (already
  available via the existing `illuminate/http`/`guzzlehttp/guzzle` deps) to the `Http`
  facade's accessor, via a minimal `ArrayAccess` stand-in for the app container. No new
  composer dependencies were needed. If `ApiClient` ever calls another facade, it'll need
  a binding added here too.
- Run via `docker run --rm -e ENVISO_API_KEY=... -e ENVISO_TENANT_SECRET=... -e
  ENVISO_PUBLIC_KEY="$(cat key.pem)" <image>`, or `docker compose run --rm php` from the
  repo root with `.env` populated (see root `.env.example`).
- **Build-verified end-to-end**: with dummy credentials, the container makes a real call
  to the staging API and gets a clean `403` surfaced via the `RuntimeException`; with real
  staging credentials it successfully logs in and returns a real auth token — confirms the
  facade shim, error handling, and login flow all actually work.
- The vendor stage MUST run Composer under the same PHP version as the runtime stage
  (`php:8.2-cli`, with `unzip` installed for Composer's downloader) — the first draft used
  the generic `composer:2` image instead, which bundles a newer PHP, so Composer resolved
  dependency versions requiring PHP 8.4+ that then failed to load under the 8.2 runtime.
  If bumping the runtime PHP version, bump the vendor stage's `FROM` to match.

## Things worth double-checking if touching this file
- `x-tenantsecretkey` header (from `$this->tenantSecret`) is sent on `createNewToken`,
  matching `.net`/`Postman` — the docs don't actually require it on login (only on
  subsequent authenticated calls), but sending it doesn't hurt.
- `createNewToken` throws a `\RuntimeException` on a non-ok response instead of silently
  leaving the cache unset.
- **TTL mismatch**: `authToken` is cached 30 days, but the `authTokenStillActive` marker is
  only 1 hour — every call after hour 1 re-triggers a full `createNewToken()` login (a
  network round-trip) rather than trusting the 30-day token lifetime. May be intentional
  hourly revalidation; worth confirming.
- No lock around the cache read-then-write in `getAuthToken` — concurrent requests on an
  expired/missing token can both trigger a login simultaneously. Low severity for a sample,
  real under concurrent Laravel workers.
- Cache keys (`envisopay.authToken` etc.) are global, not per-tenant/apiKey — fine for a
  single-tenant sample, would collide if reused for multiple tenants in one app.
- `composer.json`'s `illuminate/http ^9.0.0` pin is stale relative to current Laravel LTS;
  worth a bump if this sample is meant to stay current like `.net` was.
- phpseclib3's `RSA::ENCRYPTION_PKCS1` confirmed equivalent to `.net`'s
  `RSAEncryptionPadding.Pkcs1` (PKCS#1 v1.5 encryption, not OAEP).
