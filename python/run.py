import base64
import hashlib
import os
import sys

from datetime import datetime, timezone
from Crypto.PublicKey import RSA
from Crypto.Cipher import PKCS1_v1_5

import requests


def required_env(name):
    value = os.environ.get(name)
    if not value:
        print(f"Missing required env var: {name}", file=sys.stderr)
        sys.exit(1)
    return value


pub_key = required_env('ENVISO_PUBLIC_KEY')
api_key = required_env('ENVISO_API_KEY')
url = os.environ.get('ENVISO_LOGIN_URL', 'https://api.staging-enviso.io/authenticationapi/v1/login/')

timestamp = datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%S.%fZ')[:-4]+'Z'

print("TIMESTAMP: ", timestamp, "\n")
key = api_key + '_' + timestamp

print("MESSAGE: ", key, "\n")

sha_hash = hashlib.sha256(key.encode()).hexdigest()

print("SHA256 HASH: ", sha_hash, "\n")

rsa_key = RSA.importKey(pub_key)
cipher = PKCS1_v1_5.new(rsa_key)

signature = base64.b64encode(cipher.encrypt(sha_hash.encode()))

print("SIGNATURE: ", signature, "\n")

response = requests.post(
    url,
    json={
        'apikey': api_key,
        'timestamp': timestamp,
        'signature': signature.decode(),
    }, headers = {'x-api-key': api_key, 'Content-Type': 'application/json',},
    timeout=10,
)

response.raise_for_status()
print ("RESPONSE: ", response.json(), "\n")
