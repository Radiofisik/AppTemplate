---
title: Identity
description: Identtity Server 4, OAuth, OpenId, Password flow
---

IdentityServer4 - это реализация протокола OpenId. Она предоставляет следующие ендпойнты:

- discovery endpoint ( /.well-known/openid-configuration) возвращает информацию об остальных ендпойнтах сервера
- authorize endpoint служит для получения request токенов или кодов авторизации сам процесс требует участия пользователя
- token endpoint для программного получения токена
- UserInfo endpoint  для получения информации  о пользователе
- device authorization endpoint для device flow
- introspection endpoint для валидации reference токенов
- revocation endpoint для отзыва refresh токенов и reference токенов
- end session endpoint  для логаута

К регистрации пользователей и хранению учетных данных IdentityServer4  отношения не имеет, для этого обычно используется Identity.

Реализуем простейшую реализацию авторизации через OpenId. 

Создадим проект который будет реализовывать Resource Owner Paasword Flow OpenId.  Создадим проект, добавим логирование (Serilog, ElasticSearch), Autofac. Далее установим пакеты

```bash
dotnet add package IdentityServer4
```

В простейшем случае для того чтобы заработал сервер аутентификации небходимо добавить (код взят из примеров IdentityServer4)

```c#
services.AddIdentityServer()
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryApiResources(Config.GetApis())
                .AddInMemoryClients(Config.GetClients())
                .AddTestUsers(Config.GetUsers());


app.UseIdentityServer();
```

где config

```c#
 public static class Config
    {
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "alice",
                    Password = "password"
                },
                new TestUser
                {
                    SubjectId = "2",
                    Username = "bob",
                    Password = "password"
                }
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new IdentityResource[]
            {
                new IdentityResources.OpenId()
            };
        }

        public static IEnumerable<ApiResource> GetApis()
        {
            return new List<ApiResource>
            {
                new ApiResource("api1", "My API")
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = "client",

                    // no interactive user, use the clientid/secret for authentication
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    // scopes that client has access to
                    AllowedScopes = { "api1" }
                },
                // resource owner password grant client
                new Client
                {
                    ClientId = "ro.client",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    AllowedScopes = { "api1" }
                }
            };
        }
    }
```

В принципе это уже работоспособный сервер, на нем уже можно аутентифицироваться. Но надо спрятать его за прокси.

```nginx
location ~ ^/auth/ {
    rewrite ^/auth/(.*)$ /$1 break;
    proxy_pass  http://debughost:5005;
}
```

таким образом сервер будет доступен по адресу http://docker/auth/.well-known/openid-configuration но в ответ он выдает адреса как будто он доступен напрямую.

## Claim Based аутентификация

В Asp.Net Identity включена возможность аутентификации по Claim'ам. для того чтобы добавить пользователю Claim можно выполнить следующий код

```c#
await  _userManager.AddClaimAsync(user, new Claim("Admin", "true"));
```

Для того чтобы внедрить в токен этот Claim добавим специального клиента IdentityServer4

Создадим клиента, в скопах которого добавим ресурс `administration`

```c#
 public static Client AdministrationClient(int lifetime) => new Client
        {
            ClientName = "administration_client",
            ClientId = "administration_client",
            ClientSecrets =
            {
                new Secret("administration_client-secret".Sha256())
            },

            AllowedGrantTypes = { GrantType.ResourceOwnerPassword },
            AccessTokenType = AccessTokenType.Jwt,

            AllowOfflineAccess = true,
            AccessTokenLifetime = lifetime,
            AllowAccessTokensViaBrowser = true,
            RequireConsent = false,
            AllowedCorsOrigins = Origins.Urls,

            RefreshTokenExpiration = TokenExpiration.Absolute,
            RefreshTokenUsage = TokenUsage.OneTimeOnly,

            AllowedScopes = new List<string>
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Email,
                IdentityServerConstants.StandardScopes.OfflineAccess,
                "administration"
            }
        };
```

Определим ресурс в скопе `    .AddInMemoryApiResources(GetApiResources())`

```c#
public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource()
                {
                    Name = "administration_api",
                    ApiSecrets = { new Secret("administration_api_secret".Sha256()) },
                    UserClaims = {
                       "IsAdmin"
                    },
                    Description = "Administration API",
                    DisplayName = "Administration API",
                    Enabled = true,
                    Scopes = { new Scope("administration") }
                }
            };
        }
```

Для получения информации о пользователях из Identity для IdentityServer4 надо реализовать интерфейс `IProfileService` который включает два метода. один для проверки активен ли пользовтель

```c#
 public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            context.IsActive = user != null;
        }
```

Второй для получения информации о нем. В него при запросе Claim прилитит что мы просим в `context.RequestedClaimTypes`

```c#
   public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            var principal = await _claimsFactory.CreateAsync(user);

            var claims = principal.Claims
                                  .Where(claim => context.RequestedClaimTypes.Contains(claim.Type))
                                  .ToList();

            claims.Add(new Claim(IdentityServerConstants.StandardScopes.Email, user.Email.ToLower()));

            context.IssuedClaims = claims;
        }
```

Для проверки создадим простой скрипт на python который получает токен с Claim и обращается к ресурсу

```python
import requests
import jwt
import json

headers = {}
payload = {
    'client_id': 'administration_client',
    'grant_type': 'password',
    'client_secret': 'administration_client-secret',
    'scope': 'openid email offline_access administration',
    'username': 'admin3@radiofisik.ru',
    'password': 'password'
     }

response = requests.post("http://base-url/connect/token", data=payload, headers=headers)

# print(response.content)
token = response.json()['access_token']
# print(token)

jwtContent = jwt.decode(token, 'secret',  verify=False)
# print(jwtContent)

print(json.dumps(jwtContent, indent=4, sort_keys=True))


headers = {'Authorization': 'Bearer '+token,
             'Content-Type':'application/json',
             'Accept': 'text/plain',
             'Content-Encoding': 'utf-8'}
payload = {
    'testParam': 'fe67b4ab-4d6d-4cbe-ac3f-213b37dc740d',
     }

response = requests.post("http://api-url", data=json.dumps(payload), headers=headers)
print(response.content)
```



