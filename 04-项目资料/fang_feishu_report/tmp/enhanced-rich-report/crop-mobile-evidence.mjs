import sharp from "file:///C:/Users/duyiyan/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.pnpm/sharp@0.34.5/node_modules/sharp/lib/index.js";
import fs from "node:fs/promises";

const sourceDir = "C:/Users/duyiyan/Documents/Codex/2026-07-19/wbe-2/work/presentations/fang_feishu_report/tmp/user-modified-multiclient/assets";
const outputDir = "C:/Users/duyiyan/Documents/Codex/2026-07-19/wbe-2/work/presentations/fang_feishu_report/tmp/enhanced-rich-report/assets";

await fs.mkdir(outputDir, { recursive: true });

await sharp(`${sourceDir}/mini-contacts.png`)
  .extract({ left: 0, top: 0, width: 860, height: 538 })
  .png()
  .toFile(`${outputDir}/mini-contacts-top.png`);

await sharp(`${sourceDir}/mini-approvals.png`)
  .extract({ left: 0, top: 0, width: 860, height: 538 })
  .png()
  .toFile(`${outputDir}/mini-approvals-top.png`);
