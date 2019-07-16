---
title: Proxy, Nginx, OpenResty
description: Настройка аутентификации через Google
---

Микросервисная архитектура предполагает наличие некоторого проксирующего узла, который контролирует может контролировать доступ к внутренним API, проверять аутентификацию пользователей... Так в проекте  в демонстрационном проекте Microsoft  https://github.com/dotnet-architecture/eShopOnContainers этот слой реализован с помощью Ocelot. Однако в индустрии наиболее типичной практикой является использование Nginx в качестве прокси сервера. Для расширения возможностей Nginx в него можно встроить язык программирования Lua (проект Open Resty).

## Open Resty Hello World

Понимая что надо будет менять образ, создадим свой, пока пустой `Dockerfile` на основании образа `openresty/openresty:alpine-fat`

```dockerfile
FROM openresty/openresty:alpine-fat
```

Создадим файл `docker-compose.yml`

```yml
version: '3.5'

services:       
  proxy:
    image: radiofisik/proxy
    ports:
      - 80:80
```

И `docker-compose.override.yml`

```yml
version: '3.5'

services:       
  proxy:
    build:
        context: ./Proxy
        dockerfile: Dockerfile
```

Соберем образ 

```bash
$ docker-compose build
Building proxy
Step 1/1 : FROM openresty/openresty:alpine-fat
alpine-fat: Pulling from openresty/openresty
e7c96db7181b: Pull complete                                                                                             4d3219fa62f3: Pull complete                                                                                             d9569d5c915d: Pull complete                                                                                             8de440772a85: Pull complete                                                                                             37293d22edeb: Pull complete                                                                                             Digest: sha256:463a3b32d76bc18efbe0c7b9a73f942964b59537517c73a3d5b6cc9506f22d47
Status: Downloaded newer image for openresty/openresty:alpine-fat
 ---> e7f04f75007e

Successfully built e7f04f75007e
Successfully tagged radiofisik/proxy:latest
```

Запустим его 

```bash
$ docker-compose up -d
Creating network "src_default" with the default driver
Creating src_proxy_1 ... done    
```

Убедимся что запустился

```bash
$ docker ps
CONTAINER ID        IMAGE               COMMAND                  CREATED             STATUS              PORTS                NAMES
ce0baac03395        radiofisik/proxy    "/usr/local/openrest…"   6 seconds ago       Up 5 seconds        0.0.0.0:80->80/tcp   src_proxy_1

```

После чего можно зайти по ссылке http://docker/ (docker это адрес хоста с докером - в моем случае 192.168.99.100)

Если зайти в образ через `docker exec -it src_proxy_1 /bin/sh` можно увидеть что основной конфиг nginx лежит по пути ` /usr/local/openresty/nginx/conf/nginx.conf ` Внутри этот файл подключает `include /etc/nginx/conf.d/*.conf;` Для того чтобы можно было вносить изменения в конфигурацию добавим папку `conf` и строчки в `Dockerfile`
```dockerfile
RUN rm /etc/nginx/conf.d/default.conf
COPY ./conf/*.conf /etc/nginx/conf.d/ 
```
в папку conf добавим файл `hello.conf`

```nginx
 server {
			listen 80;
			location / {
				default_type text/html;
				content_by_lua '
					ngx.say("<p>hello, world</p>")
				';
			}
		}
```

После чего можно зайти по ссылке http://docker/ (docker это адрес хоста с докером - в моем случае 192.168.99.100) и увидеть `hello world ` в браузере

## Google аутентификация

В проекте https://github.com/zmartzone/lua-resty-openidc есть пример который можно использовать для внедрения google аутентификации в проект. Добавим в `Dockerfile`

```dockerfile
RUN luarocks install lua-resty-openidc
```

Получим в консоле девелопера https://console.developers.google.com/ параметры аутентификации приложения `client_id` и `client_secret`. Там же надо настроить разрешения для урлов редиректа. Для целей тестирования пропишем в hosts файл `192.168.99.100 test.com` Для теста создадим небольшое приложение (код, docker-compose см. репозиторий) которое возвращает заголовки запроса сделанного к нему, запустим его в докере по адресу app.  Добавим в папку с файлами конфигурации файл `google.conf`

```nginx
  lua_package_path '~/lua/?.lua;;';

  resolver 8.8.8.8;

  lua_ssl_trusted_certificate /etc/ssl/certs/ca-certificates.crt;
  lua_ssl_verify_depth 5;

  # cache for discovery metadata documents
  lua_shared_dict discovery 1m;
  # cache for JWKs
  lua_shared_dict jwks 1m;

  server {
    listen 80;

    location / {

      access_by_lua_block {
	  
          local opts = {
             redirect_uri = "http://test.com/secured",
             discovery = "https://accounts.google.com/.well-known/openid-configuration",
             client_id = "id from google",
             client_secret = "secret from google"
          }

          -- call authenticate for OpenID Connect user authentication
          local res, err = require("resty.openidc").authenticate(opts)

          if err then
            ngx.status = 500
            ngx.say(err)
            ngx.exit(ngx.HTTP_INTERNAL_SERVER_ERROR)
          end

			ngx.req.set_header("X-EMAIL", res.user.email)
			ngx.req.set_header("X-USER", res.id_token.sub)
      }

      proxy_pass http://app;
    }
}
```

Соберем и запустим все в докере 

```bash
docker-compose build && docker-compose up -d
```

Перейдя в по ссылке http://test.com/ Увидим что после аутентификации на сайте Google в заголовок запроса попадают заданные в конфиге `X-EMAIL` которые можно использовать в коде программы.



> Репозиторий проекта https://github.com/Radiofisik/AuthProxy