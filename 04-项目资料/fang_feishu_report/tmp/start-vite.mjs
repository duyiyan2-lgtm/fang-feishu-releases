import { createServer } from "file:///C:/Users/duyiyan/Desktop/%E6%96%87%E4%BB%B6/111/source_code_0718(1)/node_modules/vite/dist/node/index.js";

const root = "C:/Users/duyiyan/Desktop/文件/111/source_code_0718(1)";
process.env.BROWSER = "none";

const server = await createServer({
  root,
  configFile: `${root}/vite.config.js`,
  server: {
    host: "127.0.0.1",
    port: 5182,
    strictPort: true,
    open: false,
  },
});

await server.listen();
console.log("VITE_READY http://127.0.0.1:5182");

