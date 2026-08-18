import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const workDir = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\work\presentations\fang_feishu_report\tmp\user-modified-multiclient`;
const outputPath = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\outputs\仿飞书协同办公平台-第7组项目汇报-多端效果图版.pptx`;
const webScreenshot = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\work\presentations\fang_feishu_report\tmp\assets\web-calendar.png`;
const miniScreenshot = path.join(workDir, "assets", "mini-multiview-16x10.png");

async function writeBlob(filePath, blob) {
  await fs.writeFile(filePath, new Uint8Array(await blob.arrayBuffer()));
}

function replaceText(presentation, id, before, after) {
  const target = presentation.resolve(id);
  target.text.replace(before, after);
}

function parseInspect(ndjson) {
  return ndjson
    .split(/\r?\n/)
    .filter((line) => line.trim())
    .map((line) => JSON.parse(line));
}

function findTextId(records, slide, text) {
  const match = records.find(
    (record) => record.kind === "textbox" && record.slide === slide && record.text === text,
  );
  if (!match) throw new Error(`Text anchor not found on slide ${slide}: ${text}`);
  return match.id;
}

async function addImageOverlay(slide, imagePath, alt, position) {
  const bytes = new Uint8Array(await fs.readFile(imagePath));
  slide.images.add({
    blob: bytes,
    contentType: "image/png",
    alt,
    fit: "cover",
    position,
    geometry: "rect",
  });
}

async function main() {
  const qaDir = path.join(workDir, "qa");
  const renderDir = path.join(workDir, "final-renders");
  const layoutDir = path.join(workDir, "final-layouts");
  await fs.mkdir(qaDir, { recursive: true });
  await fs.rm(renderDir, { recursive: true, force: true });
  await fs.rm(layoutDir, { recursive: true, force: true });
  await fs.mkdir(renderDir, { recursive: true });
  await fs.mkdir(layoutDir, { recursive: true });
  await fs.mkdir(path.dirname(outputPath), { recursive: true });

  const presentation = await PresentationFile.importPptx(
    await FileBlob.load(path.join(workDir, "template-starter.pptx")),
  );

  const initialInspect = await presentation.inspect({
    kind: "slide,textbox,image",
    maxChars: 100000,
  });
  const initialRecords = parseInspect(initialInspect.ndjson);
  const editedSlide = presentation.slides.items[14];
  await writeBlob(
    path.join(qaDir, "before-slide-15.png"),
    await presentation.export({ slide: editedSlide, format: "png", scale: 1 }),
  );
  await fs.writeFile(
    path.join(qaDir, "before-slide-15.layout.json"),
    await (await editedSlide.export({ format: "layout" })).text(),
  );

  replaceText(
    presentation,
    findTextId(initialRecords, 15, "内容协作覆盖创建、治理与回收"),
    "内容协作覆盖创建、治理与回收",
    "桌面端与小程序延续同一套协作业务",
  );
  replaceText(
    presentation,
    findTextId(initialRecords, 15, "截图：真实后端数据；依据：Documents / Files / Wiki 接口"),
    "截图：真实后端数据；依据：Documents / Files / Wiki 接口",
    "截图：本次实际运行；依据：Electron 复用 Web，uni-app H5 / 小程序同源",
  );
  replaceText(presentation, findTextId(initialRecords, 15, "06"), "06", "15");
  replaceText(presentation, findTextId(initialRecords, 15, "云文档"), "云文档", "Electron 桌面端");
  replaceText(
    presentation,
    findTextId(initialRecords, 15, "创建与编辑、评论、版本历史、协作者、可见性、版本恢复与软删除。"),
    "创建与编辑、评论、版本历史、协作者、可见性、版本恢复与软删除。",
    "Electron 复用 Vue 3 界面，可打包桌面应用；登录、日历、文档与 Web 保持一致。",
  );
  replaceText(presentation, findTextId(initialRecords, 15, "云盘与知识库"), "云盘与知识库", "微信小程序");
  replaceText(
    presentation,
    findTextId(initialRecords, 15, "文件夹、上传下载、预览、移动、分享、回收站；知识库支持空间、节点、成员和搜索。"),
    "文件夹、上传下载、预览、移动、分享、回收站；知识库支持空间、节点、成员和搜索。",
    "uni-app 独立实现；真实账号登录后可进入消息与工作台，共享同一套后端数据。",
  );

  await addImageOverlay(
    editedSlide,
    webScreenshot,
    "Electron 桌面端日历页面的实际运行截图",
    { left: 58.8, top: 140, width: 534.4, height: 334 },
  );
  await addImageOverlay(
    editedSlide,
    miniScreenshot,
    "uni-app 小程序同源消息列表与工作台实际运行截图",
    { left: 665.8, top: 140, width: 534.4, height: 334 },
  );

  replaceText(presentation, findTextId(initialRecords, 17, "16"), "16", "17");
  replaceText(presentation, findTextId(initialRecords, 18, "17"), "17", "18");
  replaceText(presentation, findTextId(initialRecords, 19, "18"), "18", "19");

  await writeBlob(
    path.join(qaDir, "after-slide-15.png"),
    await presentation.export({ slide: editedSlide, format: "png", scale: 1 }),
  );
  await fs.writeFile(
    path.join(qaDir, "after-slide-15.layout.json"),
    await (await editedSlide.export({ format: "layout" })).text(),
  );

  for (const [index, slide] of presentation.slides.items.entries()) {
    const stem = `slide-${String(index + 1).padStart(2, "0")}`;
    await writeBlob(
      path.join(renderDir, `${stem}.png`),
      await presentation.export({ slide, format: "png", scale: 1 }),
    );
    await fs.writeFile(
      path.join(layoutDir, `${stem}.layout.json`),
      await (await slide.export({ format: "layout" })).text(),
    );
  }

  await writeBlob(
    path.join(workDir, "final-montage.webp"),
    await presentation.export({ format: "webp", montage: true, scale: 1 }),
  );

  const finalInspect = await presentation.inspect({
    kind: "slide,textbox,shape,image,table,chart,notes,thread,layout",
    maxChars: 200000,
  });
  await fs.writeFile(path.join(workDir, "final-deck.inspect.ndjson"), finalInspect.ndjson);

  const pptx = await PresentationFile.exportPptx(presentation);
  await pptx.save(outputPath);
  console.log(JSON.stringify({ outputPath, slides: presentation.slides.items.length }));
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
