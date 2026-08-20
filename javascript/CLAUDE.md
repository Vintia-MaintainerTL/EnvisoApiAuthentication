# javascript/ — Enviso auth sample

Not a client library — a static HTML page (`index.html`) that manually builds and
displays the signed login payload for copy/paste testing. Makes no HTTP call itself.

## Files
- `index.html` — UI + all logic, inline `<script>` at the bottom.
- `sha256.js` — vendored jsSHA library (provides `jsSHA`).
- `jsencrypt.js` — vendored JSEncrypt library (provides `JSEncrypt`), does the RSA step.
  Vendored version is v2.3.0 (2016) — current npm latest is 3.5.4, same API shape
  (`setPublicKey`/`encrypt`), so a bump would be a drop-in if this is ever revisited.

## Flow (`hashAndEncrypt()`, index.html:106)
1. `hash([apiKey, timestamp])` — jsSHA joins with `_` and SHA-256-hashes, hex output.
   Matches the `{ApiKey}_{TimestampUtc}` scheme in the root README.
2. `sign(hash, publicKey)` — JSEncrypt `.encrypt()` with the PEM public key. JSEncrypt's
   default padding is PKCS#1 v1.5, same as the `.net` (`RSAEncryptionPadding.Pkcs1`) and
   python (`PKCS1_v1_5`) samples — not verified byte-for-byte against them, just consistent
   by library default.
3. Output box shows `{apikey, timestamp, signature}` JSON — this is the login POST body,
   but nothing here posts it.
4. Timestamp defaults to `new Date().toISOString()`, which already matches
   `yyyy-MM-ddTHH:mm:ss.fffZ`.

## Docker
`Dockerfile` added — just an `nginx:alpine` serving `index.html`/`sha256.js`/`jsencrypt.js`
statically on port 80. No code changes needed (there are no secrets to inject here, the
apikey/pubkey are already user-editable form fields). Run via `docker run --rm -p 8080:80
<image>` then open `http://localhost:8080`, or `docker compose up javascript` from the
repo root.

## Quirks / things to check before relying on this as a reference
- jsSHA's `getHash("HEX")` (sha256.js) produces lowercase hex, matching `.net`/`php`/
  `python`/`Postman` (`.net` used to produce uppercase — fixed, see root `index.md`).
- `jsencrypt.js`'s `JSEncrypt.encrypt()` still returns `false` on failure (vendored lib,
  left as-is), but `hashAndEncrypt()` in index.html checks for `signed === false` and shows
  a clear error message instead of writing the literal string `"false"` into the output box.
- Confirmed padding parity: `pkcs1pad2` in jsencrypt.js is genuine PKCS#1 v1.5, matching
  `.net`/`php`/`python`.
- Loads jQuery over plain `http://ajax.googleapis.com/...` (index.html:8) — dead/mixed-content
  risk, and jQuery is only used for form wiring, not the crypto.
- Function is misleadingly named `sign` (index.html:90) even though it's RSA *encryption*
  of the hash with the *public* key, not a signature — same terminology drift the root
  README calls out for `.net`'s original naming.
- Does not exercise a login endpoint URL at all, so it can't confirm/deny the
  `authenticationapi` vs `resellingapi` path question raised in the root `index.md`.
