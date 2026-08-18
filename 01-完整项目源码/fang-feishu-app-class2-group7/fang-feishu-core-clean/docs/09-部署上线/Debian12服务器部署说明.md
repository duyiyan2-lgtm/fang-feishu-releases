# Debian 12 服务器部署说明

## 一、适用环境

本说明适用于 Debian GNU/Linux 12 bookworm 服务器。

后端部署方式：

- Docker Compose 启动 ASP.NET Core API
- Docker Compose 启动 PostgreSQL
- Docker Compose 启动 MinIO
- Nginx 反向代理 API、Swagger 和 SignalR WebSocket

## 二、服务器前置条件

服务器需要提前准备：

- Debian 12
- Docker Engine
- Docker Compose Plugin
- Nginx
- Git 或可上传项目压缩包的方式

检查命令：

```bash
docker --version
docker compose version
nginx -v
```

## 三、上传项目

建议将项目放到：

```bash
/opt/fang-feishu-core-clean
```

进入项目根目录：

```bash
cd /opt/fang-feishu-core-clean
```

## 四、准备生产环境变量

复制环境变量模板：

```bash
cp deploy/docker/.env.prod.example deploy/docker/.env.prod
```

编辑生产环境配置：

```bash
nano deploy/docker/.env.prod
```

必须修改这些值：

```text
APP_DOMAIN=你的域名或服务器IP
CORS_ORIGIN_0=https://你的域名
CORS_ORIGIN_1=http://你的域名
POSTGRES_PASSWORD=强密码
MINIO_ROOT_PASSWORD=强密码
JWT_SECRET=至少32位的强随机字符串
```

如果暂时没有域名，可以先使用服务器 IP：

```text
APP_DOMAIN=服务器IP
CORS_ORIGIN_0=http://服务器IP
CORS_ORIGIN_1=http://服务器IP:5173
```

## 五、授权脚本

Linux 服务器首次执行前需要给脚本执行权限：

```bash
chmod +x backend/scripts/start-full.sh
chmod +x backend/scripts/stop-full.sh
```

## 六、一键启动后端完整系统

执行：

```bash
./backend/scripts/start-full.sh
```

首次执行会自动构建后端镜像，并启动：

- API
- PostgreSQL
- MinIO

启动成功后，本机可访问：

```text
http://127.0.0.1:5080/health
http://127.0.0.1:5080/swagger
```

注意：生产 Compose 默认只把 API 绑定到服务器本机 `127.0.0.1:5080`，公网访问需要通过 Nginx 代理。

## 七、重置数据后启动

如果需要清空数据库和文件存储后重新启动：

```bash
./backend/scripts/start-full.sh --reset
```

该命令会删除 Docker volume，请谨慎用于生产环境。

## 八、停止系统

普通停止：

```bash
./backend/scripts/stop-full.sh
```

停止并删除数据卷：

```bash
./backend/scripts/stop-full.sh --remove-volumes
```

## 九、配置 Nginx

复制 Nginx 配置：

```bash
sudo cp deploy/nginx/fang-feishu-api.conf /etc/nginx/sites-available/fang-feishu-api.conf
```

修改域名：

```bash
sudo nano /etc/nginx/sites-available/fang-feishu-api.conf
```

将：

```nginx
server_name example.com;
```

改为你的域名或服务器 IP。

启用站点：

```bash
sudo ln -s /etc/nginx/sites-available/fang-feishu-api.conf /etc/nginx/sites-enabled/fang-feishu-api.conf
sudo nginx -t
sudo systemctl reload nginx
```

## 十、开放防火墙端口

如果服务器启用了防火墙，需要开放：

```text
80
443
```

不建议直接开放：

```text
5080
5432
55432
9000
9001
```

数据库和 MinIO 管理端口默认只绑定到本机，避免直接暴露到公网。

## 十一、前端联调地址

如果通过 Nginx 对外访问，前端 API 基础地址使用：

```text
http://你的域名/api/v1
```

SignalR 地址：

```text
http://你的域名/hubs/im
```

如果配置 HTTPS，则使用：

```text
https://你的域名/api/v1
https://你的域名/hubs/im
```

## 十二、常用排查命令

查看容器状态：

```bash
docker compose --env-file deploy/docker/.env.prod -f deploy/docker/docker-compose.prod.yml ps
```

查看 API 日志：

```bash
docker logs -f fang-feishu-api
```

查看数据库日志：

```bash
docker logs -f fang-feishu-postgres
```

查看 MinIO 日志：

```bash
docker logs -f fang-feishu-minio
```

检查健康接口：

```bash
curl http://127.0.0.1:5080/health
```

## 十三、部署结论

完成以上配置后，后端可以在 Debian 12 服务器上以 Docker Compose 方式稳定运行。

阶段状态：

```text
后端已支持 Windows 本地一键启动和 Debian 12 服务器一键部署，可进入前后端联调与演示环境部署阶段。
```
