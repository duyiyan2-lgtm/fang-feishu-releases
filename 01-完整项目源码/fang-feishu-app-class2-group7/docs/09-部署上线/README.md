# 09 · 部署上线

## 🎯 阶段目标

输出一键可执行的部署手册、回滚方案、CI/CD 流水线，保证系统从**测试环境 → 预发布 → 生产**的稳定交付。

## 📝 必含章节

| 章节 | 说明 | 必含配图 |
| :--: | :-- | :--: |
| 一、部署架构 | 拓扑、组件、端口、依赖 | ✅ |
| 二、环境清单 | dev / staging / prod 配置差异 | ✅ |
| 三、Docker 镜像 | Dockerfile、镜像构建、推送到哪 | ✅ |
| 四、Docker Compose | 多容器编排 | ❌ |
| 五、CI/CD 流水线 | GitHub Actions / Gitee Go 配置 | ✅ |
| 六、部署步骤 | 从 0 到 1 完整步骤 | ✅ |
| 七、回滚方案 | 触发条件、回滚步骤 | ✅ |
| 八、验证清单 | 部署后 smoke test | ❌ |

## 📂 命名规范

```
09-部署上线-<环境>-<主题>.md
```

示例：
- `09-部署上线-生产-Docker部署.md`
- `09-部署上线-生产-回滚方案.md`
- `09-部署上线-CI-CD流水线.md`

## 🖼 配图要求

- 部署架构拓扑图
- CI/CD 流水线图
- Docker 镜像构建过程截图
- 部署命令执行截图
- **每篇 ≥ 6 张配图**

## 📦 产出物清单

- [ ] 部署架构图
- [ ] Dockerfile（前端 + 后端 + 各个微服务）
- [ ] docker-compose.yml（dev / prod）
- [ ] Nginx 配置（`deploy/nginx/`）
- [ ] CI/CD 配置（`.github/workflows/` 或 `.gitee/`）
- [ ] 部署手册
- [ ] 回滚手册
- [ ] 监控告警配置

## ✍️ 文档模板

```markdown
# <环境> - <主题> 部署

## 一、部署架构
（插入部署拓扑图：Nginx / API / Worker / DB / Cache / MQ / Storage）

## 二、环境清单
| 环境 | 域名 | 服务器 | 数据库 | 备注 |
| :--: | :--: | :--: | :--: | :--: |
| dev | dev.xxx.com | 192.168.1.10 | pgsql-dev | |
| staging | staging.xxx.com | 192.168.1.20 | pgsql-stg | |
| prod | xxx.com | 192.168.1.30 | pgsql-prod | 主从 |

## 三、Docker 镜像
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FF.App.Api.dll"]
```

## 四、Docker Compose
（关键片段）

## 五、CI/CD 流水线
（插入流水线图 + GitHub Actions YAML 关键配置）

## 六、部署步骤
```bash
# 1. 拉取代码
git pull origin main

# 2. 构建镜像
docker compose -f deploy/docker/docker-compose.prod.yml build

# 3. 启动服务
docker compose -f deploy/docker/docker-compose.prod.yml up -d

# 4. 数据库迁移
docker compose exec api dotnet ef database update

# 5. 验证
curl https://xxx.com/health
```

## 七、回滚方案
| 触发条件 | 响应时限 | 回滚步骤 |
| :--: | :--: | :-- |
| 5xx > 1% | 5min | git revert + 重新部署 |

## 八、验证清单
- [ ] 健康检查通过
- [ ] 关键接口 200
- [ ] 数据库连接正常
- [ ] 静态资源 200
```
