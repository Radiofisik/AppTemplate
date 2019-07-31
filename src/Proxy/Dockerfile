FROM openresty/openresty:alpine-fat
RUN luarocks install lua-resty-openidc
RUN rm /etc/nginx/conf.d/default.conf
COPY ./nginx.conf /usr/local/openresty/nginx/conf/nginx.conf
COPY ./conf/*.conf /etc/nginx/conf.d/
