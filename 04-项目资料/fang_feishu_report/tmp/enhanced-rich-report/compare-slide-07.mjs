import sharp from "file:///C:/Users/duyiyan/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/lib/index.js";

const a = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\work\presentations\fang_feishu_report\tmp\enhanced-rich-report\final-renders\slide-07.png`;
const b = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\work\presentations\fang_feishu_report\tmp\enhanced-rich-report\verified-final-renders\slide-07.png`;
const ra = await sharp(a).removeAlpha().raw().toBuffer({ resolveWithObject: true });
const rb = await sharp(b).removeAlpha().raw().toBuffer({ resolveWithObject: true });
let changedChannels = 0;
let maxDelta = 0;
for (let i = 0; i < ra.data.length; i += 1) {
  const delta = Math.abs(ra.data[i] - rb.data[i]);
  if (delta) changedChannels += 1;
  if (delta > maxDelta) maxDelta = delta;
}
console.log(JSON.stringify({ changedChannels, totalChannels: ra.data.length, maxDelta }));
