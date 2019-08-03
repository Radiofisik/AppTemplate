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
                .AddTestUsers(Config.GetUsers())
                .AddDeveloperSigningCredential();


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
                    AllowedScopes = {  IdentityServerConstants.StandardScopes.OpenId, "api1" }
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

таким образом сервер будет доступен по адресу http://docker/auth/.well-known/openid-configuration но в ответ он выдает адреса как будто он доступен напрямую. Для того чтобы с этим что-то сделать надо знать на какой url шел запрос на первую прокси от польлзователя. Добавим в прокси конфиг который кладет этот адрес в заголовок.

```nginx
 	set $url $http_X_real_base_url;

    if ($http_X_real_base_url = "") {
        set $url $scheme://$http_host/auth;
    }

    proxy_set_header X-real-base-url $url;
```

Чтобы IdentityServer понимал этот url добавим MiddleWare

```c#
 public class BaseUrlMiddleWare
    {
        private readonly RequestDelegate _next;

        public BaseUrlMiddleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var realBaseUrl = context.Request.Headers["X-real-base-url"];
            var url = realBaseUrl.FirstOrDefault();

            if (url != null)
            {
                Uri uriAddress = new Uri(url, UriKind.Absolute);

                context.Request.Scheme = uriAddress.Scheme;
                context.Request.PathBase = new PathString(uriAddress.LocalPath);
                context.Request.Host = new HostString(uriAddress.Host, uriAddress.Port);
                context.SetIdentityServerBasePath(uriAddress.LocalPath);
            }

            await _next(context);
        }
    }
```

теперь запрос discovery http://docker/auth/.well-known/openid-configuration выдает приличны результат

```json
{"issuer":"http://docker:80/auth","authorization_endpoint":"http://docker:80/auth/connect/authorize","token_endpoint":"http://docker:80/auth/connect/token","userinfo_endpoint":"http://docker:80/auth/connect/userinfo","end_session_endpoint":"http://docker:80/auth/connect/endsession","check_session_iframe":"http://docker:80/auth/connect/checksession","revocation_endpoint":"http://docker:80/auth/connect/revocation","introspection_endpoint":"http://docker:80/auth/connect/introspect","device_authorization_endpoint":"http://docker:80/auth/connect/deviceauthorization","frontchannel_logout_supported":true,"frontchannel_logout_session_supported":true,"backchannel_logout_supported":true,"backchannel_logout_session_supported":true,"scopes_supported":["openid","api1","offline_access"],"claims_supported":["sub"],"grant_types_supported":["authorization_code","client_credentials","refresh_token","implicit","password","urn:ietf:params:oauth:grant-type:device_code"],"response_types_supported":["code","token","id_token","id_token token","code id_token","code token","code id_token token"],"response_modes_supported":["form_post","query","fragment"],"token_endpoint_auth_methods_supported":["client_secret_basic","client_secret_post"],"subject_types_supported":["public"],"id_token_signing_alg_values_supported":["RS256"],"code_challenge_methods_supported":["plain","S256"],"request_parameter_supported":true}
```

## Проверка токена

Приложение сейчас доступно напрямую по ссылке http://docker:8081/swagger/index.html и внутри докера по http://app/swagger/index.html Вынесем его за прокси чтобы адрес был  http://docker/api/app/swagger/index.html и внутри докера по http://app/swagger/index.html 

```nginx
location ~ ^/api/(?<service>[\.a-zA-Z0-9_-]+)/(.*)$ {
    set $upstream_endpoint http://${service}:80;
    rewrite ^/api/([\.a-zA-Z0-9_-]+)/(.*) /$2 break;
    proxy_pass $upstream_endpoint;
}

```

для отладки переопределим конфиг менее общим, который редиректит на приложение запущенное в Visual Studio и проверим аутентификацию

```lua
location ~ ^/api/app/(.*)$ {
	rewrite ^/api/app/(.*) /$1 break;
	proxy_pass http://debughost:5000;
	 access_by_lua '
	 local opts = {
		 discovery = "http://192.168.1.103:5005/.well-known/openid-configuration"
	 }

	  	-- call bearer_jwt_verify for OAuth 2.0 JWT validation
          local res, err = require("resty.openidc").bearer_jwt_verify(opts)

           if err or not res then
            ngx.status = 403
            ngx.say(err and err or "no access_token provided")
            ngx.exit(ngx.HTTP_FORBIDDEN)
          end
	 ';
}
```

Для удобства использования вынесем работу с аутентификацией через lua в отдельный файл

```lua
local module = {};

module.opts = {
    discovery = "http://192.168.1.103:5005/.well-known/openid-configuration"
}

function module.authorize(ngx)
    local res, err = require("resty.openidc").bearer_jwt_verify(module.opts);

    if err or not res then
        ngx.status = 403
        ngx.say(err and err or "no access_token provided")
        ngx.exit(ngx.HTTP_FORBIDDEN)
    end

    ngx.req.set_header("X-TOKEN-VERIFIED", tostring(true));

    for name, value in pairs(res) do
        ngx.req.set_header("X-USER-"..name, value);
    end
end

function module.fillClaims(ngx)
    local res, err = require("resty.openidc").bearer_jwt_verify(module.opts);

    if err or not res then
        ngx.req.set_header("X-TOKEN-VERIFIED", tostring(false));
        ngx.req.set_header("X-USER-ISADMIN", tostring(false));
    else
        ngx.req.set_header("X-TOKEN-VERIFIED", tostring(true));

        for name, value in pairs(res) do
            ngx.req.set_header("X-USER-"..name, value);
        end
    end
end

return module;
```

теперь использование в конфиге упростится до 

```nginx
location ~ ^/api/app/(.*)$ {
    set $upstream_endpoint http://debughost:5000;
	rewrite ^/api/app/(.*) /$1 break;
	access_by_lua 'require("auth").fillClaims(ngx);';
	#access_by_lua 'require("auth").authorize(ngx);';
    proxy_pass $upstream_endpoint;
}
```

## Настройка авторизации в Swagger

Swagger поддерживает аутентификацию, для того чтобы ее добавить надо добавить

```c#
 services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });

                c.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "password",
                    TokenUrl = "http://docker:80/auth/connect/token",
                    Scopes = new Dictionary<string, string>{}
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "oauth2", new string[] { } }
                });
            });
```

и

```
 var baseUrl = "/api/app";

            app.UseSwagger(c=>c.PreSerializeFilters.Add((doc, req) =>
            {
                doc.BasePath = baseUrl;
            }));
         
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{baseUrl}/swagger/v1/swagger.json", "My API V1");
                c.OAuthClientId("client");
                c.OAuthClientSecret("secret");
                c.OAuthRealm("test-realm");
                c.OAuthAppName("test-app");
                c.OAuthScopeSeparator(" ");
                c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            });
```

## ASP Net Identity

IdentityServer 4 не имеет встроенного функционала по поддержанию базы данных пользователей. Для этого используется AspNet Identity. Создадим классы пользователя, роли, контекста

```c#
public class ApplicationUser: IdentityUser<Guid>
    {
    }
    
  public class ApplicationRole: IdentityRole<Guid>
    {
    }
    
public class ApplicationDbContext: IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        private readonly Connections _connections;

        public ApplicationDbContext(Connections connections)
        {
            _connections = connections;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(_connections.DBConnectionString, options=>options.MigrationsHistoryTable("__EFMigrationsHistory", "auth"));

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasDefaultSchema("auth");
            base.OnModelCreating(builder);
        }
    }
```

Зарегстрируем Identity и контекст БД

```c#
  services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
  services.AddDbContext<ApplicationDbContext>();
```

Добавим миграции и смигрируем БД. Теперь сделаем простейший контроллер для регистрации пользователя

```c#
[Route("account")]
    public class AccountController: BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

            [HttpGet("do-something")]
            public async Task<IActionResult> DoSomething(string email, string password)
            {
                var user = new ApplicationUser()
                {
                    Email = email,
                    UserName = email
                };

                var result = await _userManager.CreateAsync(user, password);

                return Result(new Success<bool>(result.Succeeded));
            }
    }
```

Осталось подключить IdentityServer4 к Identity. Для этого надо установить пакет `IdentityServer4.AspNetIdentity` и добавить

```c#
  .AddAspNetIdentity<ApplicationUser>()
```



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

>  Репозиторий https://github.com/Radiofisik/AppTemplate tag `identity`