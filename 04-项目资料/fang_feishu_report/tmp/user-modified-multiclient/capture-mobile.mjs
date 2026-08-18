import { chromium } from "file:///C:/Users/duyiyan/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.pnpm/playwright@1.61.1/node_modules/playwright/index.mjs";
import fs from "node:fs/promises";

const outDir = "C:/Users/duyiyan/Documents/Codex/2026-07-19/wbe-2/work/presentations/fang_feishu_report/tmp/user-modified-multiclient/assets";
await fs.mkdir(outDir, { recursive: true });

const browser = await chromium.launch({
  executablePath: "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe",
  headless: true,
  args: ["--disable-gpu", "--no-first-run"],
});

const context = await browser.newContext({
  viewport: { width: 430, height: 932 },
  deviceScaleFactor: 2,
  locale: "zh-CN",
  isMobile: true,
  hasTouch: true,
});
const page = await context.newPage();
page.on("console", (message) => {
  if (message.type() === "error") console.log(`BROWSER_ERROR ${message.text()}`);
});

async function snap(name) {
  await page.waitForTimeout(1800);
  await page.screenshot({ path: `${outDir}/${name}.png`, fullPage: false });
  console.log(`CAPTURED ${name} ${page.url()}`);
}

try {
  await page.goto("http://127.0.0.1:5195/#/pages/login/index", {
    waitUntil: "networkidle",
    timeout: 30000,
  });
  await page.locator("input").first().waitFor({ timeout: 15000 });
  await snap("mini-login");

  const inputs = page.locator("input");
  await inputs.nth(0).fill("admin");
  await inputs.nth(1).fill("123456");
  await page.getByText("登录", { exact: true }).last().click();
  await page.waitForTimeout(6000);
  await snap("mini-after-login");

  if (!page.url().includes("pages/login/index")) {
    await page.goto("http://127.0.0.1:5195/#/pages/home/index", { waitUntil: "networkidle", timeout: 30000 }).catch(() => {});
    await snap("mini-workbench");

    const extraPages = [
      ["pages/contacts/index", "mini-contacts"],
      ["pages/documents/index", "mini-documents"],
      ["pages/approvals/index", "mini-approvals"],
      ["pages/tasks/index", "mini-tasks"],
    ];
    for (const [route, name] of extraPages) {
      await page.goto(`http://127.0.0.1:5195/#/${route}`, {
        waitUntil: "networkidle",
        timeout: 30000,
      }).catch(() => {});
      await snap(name);
    }
  }
} catch (error) {
  console.error("CAPTURE_FAILED", error);
  await page.screenshot({ path: `${outDir}/mini-capture-failed.png`, fullPage: false }).catch(() => {});
  process.exitCode = 1;
} finally {
  await browser.close();
}
