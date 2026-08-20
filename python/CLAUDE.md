# python/ — Enviso auth sample

Single top-level script, `run.py` (no package/CLI wrapper).

Runs top-to-bottom as a script: builds the signed login request, POSTs it, prints the
response. No token caching, no follow-up calls, no library/class structure to reuse.

## Flow (run.py:25-54)
1. `timestamp` — `datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%S.%fZ')[:-4]+'Z'`:
   `%f` gives microseconds (6 digits), so `[:-4]` trims to milliseconds before re-appending
   `Z` — a manual way to hit the `yyyy-MM-ddTHH:mm:ss.fffZ` format without a format-string
   library, worth keeping in mind if porting this line elsewhere.
2. `sha256(apiKey + '_' + timestamp)` hex digest — same `{ApiKey}_{TimestampUtc}` scheme.
3. RSA-encrypts the hash with `Crypto.Cipher.PKCS1_v1_5` (pycryptodome) using the hardcoded
   public key at the top of the file — matches `.net`'s `RSAEncryptionPadding.Pkcs1`.
4. POSTs `{apikey, timestamp, signature}` JSON to
   `https://api.staging-enviso.io/authenticationapi/v1/login/`, header `x-api-key` only.

## Docker
`Dockerfile` + `requirements.txt` added. `api_key`/`pub_key` are read from
`ENVISO_API_KEY`/`ENVISO_PUBLIC_KEY` — `required_env()` exits 1 with `Missing required env
var: <NAME>` on stderr if either is unset/empty, matching `php`'s `requiredEnv()` fail-fast
behavior rather than silently falling back to a demo credential. `ENVISO_LOGIN_URL` still
defaults to the staging login URL when unset (not a credential, same as `php`'s
`ENVISO_BASE_URL` default). Run via
`docker run --rm -e ENVISO_API_KEY=... -e ENVISO_PUBLIC_KEY="$(cat key.pem)" <image>`, or
`docker compose run --rm python` from the repo root.

## Quirks
- Timestamp truncation at line 25: `%f` always zero-pads to 6 digits, so `[:-4]` reliably
  strips microseconds down to milliseconds in every case, including zero. Fragile-looking
  but sound; a rewrite like `f'{dt.microsecond//1000:03d}Z'` would be clearer but isn't a
  bug fix.
- `PKCS1_v1_5` (pycryptodome) is equivalent to `.net`'s `RSAEncryptionPadding.Pkcs1`
  (RSAES-PKCS1-v1_5 per RFC 8017 §7.2). No length check on the plaintext before encrypting,
  but the 64-byte hex digest is well under the 117-byte max for this 1024-bit key.
- `requests.post` passes `timeout=10`; `response.raise_for_status()` surfaces real HTTP
  errors before `response.json()` is called.
- Login path is `authenticationapi/v1/login/` (trailing slash) — matches `php/`, `.net`,
  and the Postman collection modulo the slash. See root `index.md`.
