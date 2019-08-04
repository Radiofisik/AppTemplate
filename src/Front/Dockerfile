FROM node:11 AS build

RUN npm install -g @angular/cli@7.2.2

WORKDIR /app

COPY front-app/angular.json .
COPY front-app/tsconfig.json .
COPY front-app/tsconfig.app.json .
COPY front-app/tslint.json .
COPY front-app/package.json .

RUN npm install 
# && \
#     cd node_modules/node-sass && \
#     node scripts/build -f

COPY front-app/angular.json .
COPY front-app/src/. ./src/
RUN node --max_old_space_size=8192 ./node_modules/@angular/cli/bin/ng build

FROM nginx AS runtime

COPY --from=build /app/dist /usr/local/nginx/html
COPY conf/default.conf /etc/nginx/conf.d/
COPY conf/nginx.conf /etc/nginx/nginx.conf