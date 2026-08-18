import sharp from "file:///C:/Users/duyiyan/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.pnpm/sharp@0.34.5/node_modules/sharp/lib/index.js";

const base = "C:/Users/duyiyan/Documents/Codex/2026-07-19/wbe-2/work/presentations/fang_feishu_report/tmp/user-modified-multiclient/assets";
const left = await sharp(`${base}/mini-after-login.png`)
  .extract({ left: 0, top: 0, width: 860, height: 1000 })
  .resize(790, 920, { fit: "contain", background: "#f6f8fc" })
  .png()
  .toBuffer();
const right = await sharp(`${base}/mini-workbench.png`)
  .extract({ left: 0, top: 0, width: 860, height: 1000 })
  .resize(790, 920, { fit: "contain", background: "#f6f8fc" })
  .png()
  .toBuffer();

await sharp({
  create: { width: 1600, height: 1000, channels: 4, background: "#f6f8fc" },
})
  .composite([
    { input: left, left: 0, top: 40 },
    { input: right, left: 810, top: 40 },
  ])
  .png()
  .toFile(`${base}/mini-multiview-16x10.png`);
