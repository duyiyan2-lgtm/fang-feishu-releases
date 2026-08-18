import { defineConfig, loadEnv } from "vite";
import vue from "@vitejs/plugin-vue";
import { fileURLToPath, URL } from "node:url";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const proxyTarget = env.VITE_API_PROXY_TARGET || "https://alxy.fun:443";

  return {
    base: "./",
    plugins: [vue()],
    resolve: {
      alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
    },
    server: {
      host: "0.0.0.0",
      allowedHosts: [".trycloudflare.com"],
      port: 5182,
      strictPort: false,
      open: true,
      proxy: {
        "/api": {
          target: proxyTarget,
          changeOrigin: true,
          secure: false,
        },
        "/hubs": {
          // 必须明确端口，否则 Vite 解析 target 时用 80 端口连 HTTPS → 502
          target: proxyTarget,
          ws: true,
          changeOrigin: true,
          secure: false,
          rewriteWsOrigin: true,
          // 关闭代理层的 CORS preflight，让浏览器以为同源
          bypass: (req) => {
            // 不需要特殊处理，Vite proxy 默认会让浏览器认为同源
            return undefined;
          },
        },
      },
    },
    build: {
      target: "es2018",
      outDir: "dist",
      assetsDir: "assets",
      sourcemap: false,
      cssCodeSplit: true,
      // 小资源内联，减少请求，提升本地 file:// / Electron 加载流畅度
      assetsInlineLimit: 4096,
      chunkSizeWarningLimit: 900,
      rollupOptions: {
        output: {
          manualChunks: {
            vue: ["vue", "vue-router", "pinia"],
            icons: ["@heroicons/vue"],
            editor: [
              "@tiptap/vue-3",
              "@tiptap/starter-kit",
              "@tiptap/extension-placeholder",
              "@tiptap/extension-link",
            ],
            utils: ["axios", "dayjs", "pinyin-pro"],
            // Agora 体积大，独立 chunk，避免拖慢首屏
            agora: ["agora-rtc-sdk-ng"],
            signalr: ["@microsoft/signalr"],
          },
        },
      },
    },
    optimizeDeps: {
      include: [
        "vue",
        "vue-router",
        "pinia",
        "axios",
        "dayjs",
        "@heroicons/vue",
        "@microsoft/signalr",
      ],
    },
  };
});
