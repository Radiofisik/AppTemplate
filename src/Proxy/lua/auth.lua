local module = {};

module.opts = {
    discovery = "http://auth/.well-known/openid-configuration"
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