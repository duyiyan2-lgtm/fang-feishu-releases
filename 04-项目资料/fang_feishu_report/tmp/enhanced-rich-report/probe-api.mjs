import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const source = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\outputs\仿飞书协同办公平台-第7组项目汇报-多端效果图版.pptx`;
const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
const inspected = await presentation.inspect({ kind: "image", maxChars: 20000 });
const records = inspected.ndjson.split(/\r?\n/).filter(Boolean).map(JSON.parse);
const imageRecord = records.find((r) => r.slide === 6);
const image = presentation.resolve(imageRecord.id);
const slide = presentation.slides.items[5];

function methods(value) {
  const result = new Set();
  let current = value;
  while (current && current !== Object.prototype) {
    for (const name of Object.getOwnPropertyNames(current)) result.add(name);
    current = Object.getPrototypeOf(current);
  }
  return [...result].sort();
}

console.log(JSON.stringify({
  image: methods(image),
  slide: methods(slide),
  images: methods(slide.images),
  shapes: methods(slide.shapes),
}, null, 2));
