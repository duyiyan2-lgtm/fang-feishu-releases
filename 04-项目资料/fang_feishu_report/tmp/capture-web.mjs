import { chromium } from "file:///C:/Users/duyiyan/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.pnpm/playwright@1.61.1/node_modules/playwright/index.mjs";
import fs from "node:fs/promises";

const outDir = "C:/Users/duyiyan/Documents/Codex/2026-07-19/wbe-2/work/presentations/fang_feishu_report/tmp/assets";
await fs.mkdir(outDir, { recursive: true });

const browser = await chromium.launch({
  executablePath: "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe",
  headless: true,
  args: ["--disable-gpu", "--no-first-run"],
});

const context = await browser.newContext({
  viewport: { width: 1440, height: 900 },
  deviceScaleFactor: 1,
  locale: "zh-CN",
});
const page = await context.newPage();
page.on("console", (message) => {
  if (message.type() === "error") console.log(`BROWSER_ERROR ${message.text()}`);
});

async function snap(name) {
  await page.waitForTimeout(1600);
  await page.screenshot({ path: `${outDir}/${name}.png`, fullPage: false });
  console.log(`CAPTURED ${name} ${page.url()}`);
}

async function openSidebar(label, name) {
  const link = page.locator("aside a").filter({ hasText: label }).first();
  await link.click();
  await page.waitForLoadState("networkidle", { timeout: 10000 }).catch(() => {});
  await snap(name);
}

try {
  await page.goto("http://127.0.0.1:5182", { waitUntil: "networkidle", timeout: 30000 });
  await page.waitForSelector('input[placeholder="请输入手机号或邮箱"]');
  await snap("web-login");
  await page.locator('input[placeholder="请输入手机号或邮箱"]').fill("admin");
  await page.locator('input[placeholder="请输入密码"]').fill("123456");
  await page.getByRole("button", { name: "登录", exact: true }).click();
  await page.waitForURL(/\/messages/, { timeout: 30000 });
  await page.waitForLoadState("networkidle", { timeout: 12000 }).catch(() => {});
  await snap("web-messages");
  await openSidebar("文档", "web-documents");
  await openSidebar("云空间", "web-cloud");
  await openSidebar("日历", "web-calendar");
  await openSidebar("任务", "web-tasks");
  await openSidebar("知识库", "web-wiki");
  await openSidebar("管理后台", "web-admin");
} catch (error) {
  console.error("CAPTURE_FAILED", error);
  await page.screenshot({ path: `${outDir}/capture-failed.png`, fullPage: false }).catch(() => {});
  process.exitCode = 1;
} finally {
  await browser.close();
}
