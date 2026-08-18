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
      rollupOptions: {
        output: {
          manualChunks: {
            vue: ["vue", "vue-router", "pinia"],
            editor: [
              "@tiptap/vue-3",
              "@tiptap/starter-kit",
              "@tiptap/extension-placeholder",
              "@tiptap/extension-link",
            ],
            utils: ["axios", "dayjs", "pinyin-pro"],
            rtc: ["livekit-client"],
          },
        },
      },
      // LiveKit 本身是按需加载的独立 RTC 包，体积较大但不会阻塞消息页首屏。
      chunkSizeWarningLimit: 600,
    },
  };
});
