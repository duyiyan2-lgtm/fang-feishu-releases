# ====================================
# 多阶段构建：第一阶段构建前端，第二阶段用 Nginx 运行
# ====================================
FROM node:20-alpine AS builder

# 国内构建加速：使用 npmmirror 镜像
RUN npm config set registry https://registry.npmmirror.com

WORKDIR /app

# 先拷贝 package 文件，最大化 Docker 缓存命中
COPY package*.json ./
RUN npm ci --no-audit --no-fund

# 拷贝源码并构建
COPY . .
RUN npm run build

# ====================================
# 运行阶段：Nginx 静态托管（前端 SPA）
# ====================================
FROM nginx:alpine AS runner

# 复制自定义 nginx 配置（SPA 必须 history fallback）
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 从构建阶段拷贝打包产物
COPY --from=builder /app/dist /usr/share/nginx/html

# 暴露 80 端口
EXPOSE 80

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget -q --spider http://localhost/ || exit 1

# 启动 Nginx（前台）
CMD ["nginx", "-g", "daemon off;"]
