# 本地开发环境

基于Docker Compose的PostgreSQL、Redis、MinIO开发环境。

## 服务概览

| 服务 | 端口 | 用户名 | 密码 | 备注 |
|------|------|--------|------|------|
| PostgreSQL | 5432 | devuser | devpass123 | 数据库: devdb |
| Redis | 6379 | - | redispass123 | 需要密码认证 |
| MinIO API | 9000 | minioadmin | minioadmin123 | S3兼容API |
| MinIO Console | 9001 | minioadmin | minioadmin123 | Web管理界面 |

## 快速开始

### 启动所有服务

```bash
cd C:\Users\Lenovo\dev-env
docker-compose up -d
```

### 查看服务状态

```bash
docker-compose ps
```

### 查看日志

```bash
# 查看所有服务日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f postgres
docker-compose logs -f redis
docker-compose logs -f minio
```

### 停止所有服务

```bash
docker-compose down
```

### 停止并删除数据

```bash
docker-compose down -v
```

## 连接信息

### PostgreSQL

```bash
# 使用psql命令行
psql -h localhost -p 5432 -U devuser -d devdb

# 连接字符串
postgresql://devuser:devpass123@localhost:5432/devdb
```

### Redis

```bash
# 使用redis-cli
redis-cli -h localhost -p 6379 -a redispass123

# 连接字符串
redis://:redispass123@localhost:6379
```

### MinIO

```bash
# API端点
http://localhost:9000

# 控制台（Web界面）
http://localhost:9001

# 使用mc客户端
mc alias set local http://localhost:9000 minioadmin minioadmin123
```

## 环境变量配置

编辑 `.env` 文件可以自定义配置：

```bash
# PostgreSQL
POSTGRES_USER=devuser
POSTGRES_PASSWORD=devpass123
POSTGRES_DB=devdb
POSTGRES_PORT=5432

# Redis
REDIS_PASSWORD=redispass123
REDIS_PORT=6379

# MinIO
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=minioadmin123
MINIO_API_PORT=9000
MINIO_CONSOLE_PORT=9001
```

## 数据持久化

所有数据都存储在Docker命名卷中：

- `postgres_data` - PostgreSQL数据
- `redis_data` - Redis数据
- `minio_data` - MinIO数据

数据在容器重启后保留，只有执行 `docker-compose down -v` 时才会删除。

## 常用命令

```bash
# 进入PostgreSQL容器
docker exec -it dev-postgres psql -U devuser -d devdb

# 进入Redis容器
docker exec -it dev-redis redis-cli -a redispass123

# 进入MinIO容器
docker exec -it dev-minio sh

# 备份PostgreSQL
docker exec dev-postgres pg_dump -U devuser devdb > backup.sql

# 恢复PostgreSQL
cat backup.sql | docker exec -i dev-postgres psql -U devuser -d devdb
```

## 故障排除

### 端口被占用

如果端口被占用，修改 `.env` 文件中的端口配置，然后重新启动：

```bash
docker-compose down
docker-compose up -d
```

### 容器启动失败

查看日志排查问题：

```bash
docker-compose logs <service_name>
```

### 重置所有数据

```bash
docker-compose down -v
docker-compose up -d
```
