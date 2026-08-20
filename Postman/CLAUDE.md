# Postman/ — Enviso auth sample

Collection + environment pair, not code. `README.md` in this folder is the user-facing
usage guide (import steps, variable names) — this file is the "how the auth actually
works" note for whoever (human or agent) is reading the collection JSON next.

## Files
- `EnvisoApiAuthentication.postman_collection.json` — one example request plus a
  collection-level **Pre-request Script** that does the login.
- `EnvisoApiAuthentication.postman_environment.json` — holds `apikey`, `tenant_secretkey`,
  `authenticationApi`/`baseUrl`, etc.

## Flow (collection pre-request script, ~line 367-446)
1. Only re-logs-in if the `tokenExpireDate` collection variable is stale (throttled to
   roughly every 59 minutes) — every other request reuses the cached token.
2. `createSHA256(apiKey)` — hashes `apiKey + '_' + timestamp` with `CryptoJS.SHA256`
   (same `{ApiKey}_{TimestampUtc}` scheme as every other sample).
3. `createDigitalSignature(sha256Hash, apiSecret)` — `eval()`s a vendored JSEncrypt build
   stored in the `jsEncryptLibrary` collection variable, then `encrypt.encrypt(hash)` with
   the public key — same library/approach as `javascript/index.html`, same "digital
   signature" naming drift (it's RSA/PKCS#1 v1.5 encryption of the hash, not a signature).
4. POSTs to `{{authenticationApi}}/v1/login` with header `x-tenantsecretkey`.
5. Every request (login included) gets `x-tenantsecretkey` injected from the environment
   via a request-level pre-request script upsert (line ~446).

## Cross-sample notes
- `{{authenticationApi}}/v1/login` matches `php/`, `python/run.py`, and `.net`'s login path.
  See root `index.md`.
- This is the only sample that sends `x-tenantsecretkey` on the *login* call itself, not
  just on subsequent authenticated requests.
