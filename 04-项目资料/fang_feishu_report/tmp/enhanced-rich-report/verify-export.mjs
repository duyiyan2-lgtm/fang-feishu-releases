import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const pptxPath = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\outputs\仿飞书协同办公平台-第7组项目汇报-图文充实汇报版.pptx`;
const workDir = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\work\presentations\fang_feishu_report\tmp\enhanced-rich-report`;
const renderDir = path.join(workDir, "verified-final-renders");

async function writeBlob(filePath, blob) {
  await fs.writeFile(filePath, new Uint8Array(await blob.arrayBuffer()));
}

const presentation = await PresentationFile.importPptx(await FileBlob.load(pptxPath));
await fs.mkdir(renderDir, { recursive: true });
for (const [index, slide] of presentation.slides.items.entries()) {
  const stem = `slide-${String(index + 1).padStart(2, "0")}`;
  await writeBlob(path.join(renderDir, `${stem}.png`), await presentation.export({ slide, format: "png", scale: 1 }));
}
const inspect = await presentation.inspect({
  kind: "slide,textbox,shape,image,table,chart,notes,thread,layout",
  maxChars: 240000,
});
await fs.writeFile(path.join(workDir, "verified-final.inspect.ndjson"), inspect.ndjson);
console.log(JSON.stringify({ slides: presentation.slides.items.length }));
