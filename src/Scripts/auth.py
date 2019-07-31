import requests
import jwt  #pip install pyjwt
import json

headers = {}
payload = {
    'client_id': 'client',
    'grant_type': 'password',
    'client_secret': 'secret',
    'scope': 'openid',
    'username': 'alice',
    'password': 'password'
     }

response = requests.post("http://docker:80/auth/connect/token", data=payload, headers=headers)

# print(response.content)
token = response.json()['access_token']
print(token)

jwtContent = jwt.decode(token, 'secret',  verify=False)
print(jwtContent)

print(json.dumps(jwtContent, indent=4, sort_keys=True))

headers = {'Authorization': 'Bearer '+token,
             'Content-Type':'application/json',
             'Accept': 'text/plain',
             'Content-Encoding': 'utf-8'}

response = requests.get("http://docker/api/app/api/internal/do-something", headers=headers)
print(response.content)