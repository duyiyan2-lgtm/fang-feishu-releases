import fs from "node:fs/promises";
import {
  Presentation,
  PresentationFile,
  layers,
  shape,
  text,
} from "@oai/artifact-tool";

const ROOT = "C:/Users/duyiyan/Documents/Codex/2026-07-19/wbe-2";
const TMP = `${ROOT}/work/presentations/fang_feishu_report/tmp`;
const ASSET = `${TMP}/assets`;
const PREVIEW = `${TMP}/preview`;
const LAYOUT = `${TMP}/layout`;
const FINAL = `${ROOT}/outputs/仿飞书协同办公平台-第7组项目汇报.pptx`;

const W = 1280;
const H = 720;
const FONT = "Microsoft YaHei";
const INK = "#101318";
const MUTED = "#687386";
const PANEL = "#F1F3F5";
const RULE = "#C8CDD5";
const BLUE = "#3370FF";
const PALE_BLUE = "#EAF1FF";
const GREEN = "#00B578";
let objectCounter = 0;

function uid(prefix) {
  objectCounter += 1;
  return `${prefix}-${objectCounter}`;
}

function tx(value, left, top, width, height, size = 24, options = {}) {
  return text([value], {
    name: options.name || uid("text"),
    position: { left, top },
    width,
    height,
    style: {
      fontSize: `${size}px`,
      typeface: FONT,
      color: options.color || INK,
      bold: Boolean(options.bold),
      alignment: options.align || "left",
      verticalAlignment: options.valign || "top",
      autoFit: options.autoFit || "shrinkText",
      wrap: "square",
      insets: { top: 0, right: 0, bottom: 0, left: 0 },
    },
  });
}

function rect(left, top, width, height, fill = PANEL, options = {}) {
  return shape({
    name: options.name || uid("rect"),
    geometry: options.geometry || "rect",
    fill,
    line: options.line
      ? { style: "solid", width: options.lineWidth || 1, fill: options.line }
      : { style: "solid", width: 0, fill: "none" },
    position: { left, top },
    width,
    height,
  });
}

function rule(left, top, width, color = RULE, thickness = 1) {
  return shape({
    name: uid("rule"),
    geometry: "straightConnector1",
    fill: "none",
    line: { style: "solid", width: thickness, fill: color },
    position: { left, top },
    width,
    height: 0.01,
  });
}

function circle(left, top, diameter, fill = BLUE) {
  return rect(left, top, diameter, diameter, fill, { geometry: "ellipse" });
}

function footer(page, source = "依据：项目最终代码与 2026-07-19 实际验证") {
  return [
    tx(source, 42, 684, 900, 18, 13, { color: MUTED }),
    tx(String(page).padStart(2, "0"), 1185, 682, 54, 20, 14, { color: MUTED, align: "right" }),
  ];
}

function header(title, page, source) {
  return [
    tx(title, 42, 34, 1196, 62, 48, { bold: true, autoFit: "none" }),
    ...footer(page, source),
  ];
}

function composeSlide(presentation, nodes, name) {
  const slide = presentation.slides.add();
  slide.background.fill = "#FFFFFF";
  slide.compose(
    layers({ name, width: "fill", height: "fill" }, nodes),
    { frame: { left: 0, top: 0, width: W, height: H }, baseUnit: 1 },
  );
  return slide;
}

async function imageBytes(path) {
  const bytes = await fs.readFile(path);
  return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
}

async function addPng(slide, path, position, fit = "cover", alt = "项目界面截图") {
  slide.images.add({
    blob: await imageBytes(path),
    contentType: "image/png",
    alt,
    fit,
    position,
    geometry: "roundRect",
    borderRadius: "rounded-md",
  });
}

function bulletText(items) {
  return items.map((item) => `• ${item}`).join("\n");
}

const presentation = Presentation.create({ slideSize: { width: W, height: H } });

// 01 — Cover (Codex Grid slide 08 silhouette)
{
  const slide = composeSlide(presentation, [
    rect(646, 38, 592, 644, PALE_BLUE, { geometry: "roundRect", line: RULE }),
    tx("FANG FEISHU · PROJECT REPORT", 42, 46, 500, 28, 18, { color: BLUE, bold: true }),
    tx("仿飞书协同\n办公平台", 42, 156, 548, 190, 72, { bold: true, autoFit: "none" }),
    tx("多端一体化协作工作台", 42, 386, 520, 46, 30, { color: MUTED }),
    rule(42, 470, 184, BLUE, 4),
    tx("第7组项目汇报", 42, 506, 300, 34, 24, { bold: true }),
    tx("Web · Desktop · Android · WeChat Mini Program", 42, 554, 520, 26, 17, { color: MUTED }),
    tx("2026.07", 42, 646, 180, 24, 16, { color: MUTED }),
  ], "cover-half-text-image");
  await addPng(slide, `${ASSET}/web-messages.png`, { left: 660, top: 52, width: 564, height: 616 }, "cover", "仿飞书消息页面");
}

// 02 — Plan to delivery
composeSlide(presentation, [
  ...header("10 天 MVP 最终形成多端交付闭环", 2),
  tx("最初目标", 42, 132, 180, 34, 22, { color: MUTED }),
  tx("做出可演示的仿飞书核心流程", 42, 174, 520, 46, 34, { bold: true }),
  tx("最终结果", 678, 132, 180, 34, 22, { color: BLUE, bold: true }),
  tx("形成共享后端的四端协作系统", 678, 174, 520, 46, 34, { bold: true }),
  rule(42, 260, 1196, RULE, 1),
  tx("WEB", 42, 310, 250, 30, 20, { color: BLUE, bold: true }),
  tx("24", 42, 350, 250, 72, 56, { bold: true }),
  tx("路由声明\n核心业务主端", 42, 438, 250, 70, 22, { color: MUTED }),
  rule(320, 304, 1, RULE, 1),
  tx("DESKTOP", 342, 310, 250, 30, 20, { color: BLUE, bold: true }),
  tx("0.4.0", 342, 350, 250, 72, 52, { bold: true }),
  tx("Electron 复用 Web\n可打包桌面应用", 342, 438, 250, 70, 22, { color: MUTED }),
  rule(620, 304, 1, RULE, 1),
  tx("ANDROID", 642, 310, 250, 30, 20, { color: BLUE, bold: true }),
  tx("NATIVE", 642, 350, 250, 72, 48, { bold: true }),
  tx("Kotlin + Compose\nDebug APK 构建通过", 642, 438, 250, 70, 22, { color: MUTED }),
  rule(920, 304, 1, RULE, 1),
  tx("MINI PROGRAM", 942, 310, 296, 30, 20, { color: BLUE, bold: true }),
  tx("PASS", 942, 350, 270, 72, 52, { bold: true }),
  tx("微信小程序\n生产构建通过", 942, 438, 270, 70, 22, { color: MUTED }),
  tx("结论：交付重点已从“页面数量”转向“多端共用数据与工程闭环”。", 42, 584, 1130, 38, 26, { bold: true }),
], "plan-to-delivery-comparison");

// 03 — Four-client architecture. Connectors first, nodes second.
composeSlide(presentation, [
  ...header("一个账号、一套 API 连接四类客户端", 3),
  rule(250, 204, 292, BLUE, 2), rule(250, 294, 292, BLUE, 2), rule(250, 384, 292, BLUE, 2), rule(250, 474, 292, BLUE, 2),
  rule(758, 294, 184, BLUE, 2), rule(758, 384, 184, BLUE, 2), rule(758, 474, 184, BLUE, 2),
  rect(42, 164, 208, 76, PANEL, { line: RULE }),
  rect(42, 254, 208, 76, PANEL, { line: RULE }),
  rect(42, 344, 208, 76, PANEL, { line: RULE }),
  rect(42, 434, 208, 76, PANEL, { line: RULE }),
  tx("Web", 62, 182, 160, 32, 26, { bold: true }),
  tx("Electron 桌面端", 62, 272, 170, 32, 26, { bold: true }),
  tx("原生 Android", 62, 362, 170, 32, 26, { bold: true }),
  tx("微信小程序", 62, 452, 170, 32, 26, { bold: true }),
  rect(542, 224, 216, 310, PALE_BLUE, { line: BLUE, lineWidth: 2 }),
  tx("Nginx", 576, 260, 148, 36, 25, { color: BLUE, bold: true, align: "center" }),
  rule(578, 316, 144, BLUE, 2),
  tx("ASP.NET Core\nREST API", 570, 346, 160, 84, 30, { bold: true, align: "center", valign: "middle" }),
  tx("JWT · Swagger\nSignalR Hub", 570, 462, 160, 54, 20, { color: MUTED, align: "center" }),
  rect(942, 254, 296, 76, PANEL, { line: RULE }),
  rect(942, 344, 296, 76, PANEL, { line: RULE }),
  rect(942, 434, 296, 76, PANEL, { line: RULE }),
  tx("PostgreSQL · 业务数据", 966, 272, 240, 32, 24, { bold: true }),
  tx("MinIO / Local · 文件", 966, 362, 240, 32, 24, { bold: true }),
  tx("SignalR / Agora · 实时", 966, 452, 240, 32, 24, { bold: true }),
  tx("client_type 区分终端会话；所有终端共享同一用户、组织与业务数据模型。", 42, 590, 1160, 40, 25, { bold: true }),
], "four-client-architecture");

// 04 — Functional pillars
composeSlide(presentation, [
  ...header("四条主线覆盖完整协作场景", 4),
  rule(318, 154, 1), rule(622, 154, 1), rule(926, 154, 1),
  tx("01", 42, 156, 70, 36, 20, { color: BLUE, bold: true }),
  tx("沟通协作", 42, 206, 230, 42, 32, { bold: true }),
  tx(bulletText(["单聊 / 群聊 / @成员", "撤回、已读、搜索", "好友与通知中心", "视频会议与会中聊天"]), 42, 280, 244, 220, 23, { color: MUTED }),
  tx("02", 346, 156, 70, 36, 20, { color: BLUE, bold: true }),
  tx("内容协作", 346, 206, 230, 42, 32, { bold: true }),
  tx(bulletText(["富文本云文档", "评论、版本、协作者", "云盘、文件夹、回收站", "知识库空间与节点"]), 346, 280, 244, 220, 23, { color: MUTED }),
  tx("03", 650, 156, 70, 36, 20, { color: BLUE, bold: true }),
  tx("流程协作", 650, 206, 230, 42, 32, { bold: true }),
  tx(bulletText(["月 / 周 / 日历", "忙闲与参会回执", "审批模板与实例", "任务状态闭环"]), 650, 280, 244, 220, 23, { color: MUTED }),
  tx("04", 954, 156, 70, 36, 20, { color: BLUE, bold: true }),
  tx("组织治理", 954, 206, 230, 42, 32, { bold: true }),
  tx(bulletText(["通讯录与部门树", "用户、角色与权限", "数据字典", "操作日志与个人资料"]), 954, 280, 244, 220, 23, { color: MUTED }),
  rect(42, 560, 1196, 70, PALE_BLUE),
  tx("核心价值：用户不必在多套系统间切换，沟通、内容与流程可以在同一工作台连续完成。", 66, 580, 1148, 34, 25, { color: BLUE, bold: true }),
], "four-functional-pillars");

// 05 — IM evidence
{
  const slide = composeSlide(presentation, [
    ...header("即时通信已经跑通核心群聊场景", 5, "截图：真实后端测试账号；依据：IM 前后端代码"),
    rect(42, 126, 826, 510, PANEL, { line: RULE }),
    tx("真实数据", 914, 142, 260, 32, 20, { color: BLUE, bold: true }),
    tx("会话列表、群成员和历史消息来自真实后端，不是静态原型。", 914, 184, 290, 84, 24, { bold: true }),
    rule(914, 294, 290),
    tx("核心交互", 914, 320, 260, 30, 20, { color: MUTED, bold: true }),
    tx(bulletText(["单聊与群聊", "@成员、撤回、未读", "群公告与成员管理", "消息搜索与已读回执"]), 914, 366, 290, 168, 22, { color: MUTED }),
    rect(914, 560, 290, 76, PALE_BLUE),
    tx("SignalR 接收事件\nREST API 持久化", 934, 574, 250, 50, 22, { color: BLUE, bold: true }),
  ], "im-evidence-screenshot");
  await addPng(slide, `${ASSET}/web-messages.png`, { left: 54, top: 138, width: 802, height: 486 }, "contain", "消息与群聊真实页面");
}

// 06 — Content collaboration evidence
{
  const slide = composeSlide(presentation, [
    ...header("内容协作覆盖创建、治理与回收", 6, "截图：真实后端数据；依据：Documents / Files / Wiki 接口"),
    rect(42, 130, 568, 354, PANEL, { line: RULE }),
    rect(628, 130, 610, 354, PANEL, { line: RULE }),
    tx("云文档", 42, 512, 180, 32, 24, { color: BLUE, bold: true }),
    tx("创建与编辑、评论、版本历史、协作者、可见性、版本恢复与软删除。", 42, 554, 554, 70, 22, { color: MUTED }),
    tx("云盘与知识库", 628, 512, 220, 32, 24, { color: BLUE, bold: true }),
    tx("文件夹、上传下载、预览、移动、分享、回收站；知识库支持空间、节点、成员和搜索。", 628, 554, 594, 70, 22, { color: MUTED }),
  ], "content-collaboration-evidence");
  await addPng(slide, `${ASSET}/web-documents.png`, { left: 52, top: 140, width: 548, height: 334 }, "contain", "云文档页面");
  await addPng(slide, `${ASSET}/web-cloud.png`, { left: 638, top: 140, width: 590, height: 334 }, "contain", "云盘页面");
}

// 07 — Client technology choices
{
  const slide = composeSlide(presentation, [
    ...header("端侧技术按场景分工，共享同一套 API", 7),
    rect(42, 138, 546, 472, "#FFFFFF", { line: RULE }),
    rect(692, 138, 546, 472, "#FFFFFF", { line: RULE }),
    tx("Web + Desktop", 70, 166, 300, 40, 30, { bold: true }),
    tx("Vue 3 + Electron", 70, 228, 360, 46, 36, { color: BLUE, bold: true }),
    tx(bulletText(["Web 是完整业务主端", "Electron 直接复用构建产物", "统一路由、状态与 UI", "减少桌面端重复开发"]), 70, 306, 440, 190, 23, { color: MUTED }),
    rect(70, 520, 440, 64, PALE_BLUE),
    tx("微信小程序：uni-app 独立实现 · 生产构建通过", 88, 538, 404, 30, 18, { color: BLUE, bold: true }),
    tx("原生 Android", 720, 166, 390, 40, 30, { bold: true }),
    tx("Kotlin + Jetpack Compose", 720, 228, 450, 46, 32, { color: BLUE, bold: true }),
    tx(bulletText(["AppViewModel / StateFlow 状态驱动", "Retrofit / OkHttp 对接 REST", "SignalR 接收实时消息", "Agora RTC 视频会议"]), 720, 306, 450, 190, 22, { color: MUTED }),
    rect(720, 520, 458, 64, PALE_BLUE),
    tx("DataStore 会话 · Coil 图片 · 61 个 Compose 标记", 738, 538, 422, 30, 20, { color: BLUE, bold: true }),
    circle(605, 334, 62, BLUE),
    tx("API", 611, 349, 50, 28, 20, { color: "#FFFFFF", bold: true, align: "center" }),
  ], "client-reuse-comparison");
}

// 08 — Backend capability lines
composeSlide(presentation, [
  ...header("三条后端能力线支撑全部业务", 8),
  tx("16 controllers", 42, 132, 240, 36, 22, { color: BLUE, bold: true }),
  tx("142 HTTP actions", 348, 132, 250, 36, 22, { color: BLUE, bold: true }),
  tx("36 DbSets", 654, 132, 220, 36, 22, { color: BLUE, bold: true }),
  tx("ASP.NET Core 8", 960, 132, 278, 36, 22, { color: BLUE, bold: true }),
  rule(42, 198, 1196, RULE, 1),
  tx("鉴权与治理", 42, 244, 330, 42, 32, { bold: true }),
  tx(bulletText(["JWT + jti 撤销", "角色与权限持久化", "客户端会话版本", "统一异常与 traceId"]), 42, 318, 320, 200, 24, { color: MUTED }),
  rule(420, 226, 1),
  tx("实时协作", 462, 244, 330, 42, 32, { bold: true }),
  tx(bulletText(["SignalR 用户组 / 会话组", "消息与通知推送", "好友与会议事件", "Agora 会议令牌"]), 462, 318, 320, 200, 24, { color: MUTED }),
  rule(840, 226, 1),
  tx("数据与文件", 882, 244, 330, 42, 32, { bold: true }),
  tx(bulletText(["EF Core + PostgreSQL", "失败自动重试", "Local / MinIO 可切换", "软删除与回收站"]), 882, 318, 320, 200, 24, { color: MUTED }),
  rect(42, 568, 1196, 60, PANEL),
  tx("后端不是单纯 CRUD：它同时处理会话安全、实时事件、文件生命周期与部署健康。", 66, 584, 1148, 30, 24, { bold: true }),
], "backend-capability-lines");

// 09 — Consistency process. Connectors first.
composeSlide(presentation, [
  ...header("先落库再推送，保证最终一致", 9),
  rule(120, 312, 250, BLUE, 3), rule(390, 312, 250, BLUE, 3), rule(660, 312, 250, BLUE, 3), rule(930, 312, 210, BLUE, 3),
  circle(100, 302, 20, BLUE), circle(370, 302, 20, BLUE), circle(640, 302, 20, BLUE), circle(910, 302, 20, BLUE), circle(1140, 302, 20, BLUE),
  tx("01", 88, 202, 70, 28, 18, { color: BLUE, bold: true }),
  tx("客户端提交", 58, 242, 190, 36, 27, { bold: true }),
  tx("REST / Hub", 58, 344, 190, 30, 21, { color: MUTED }),
  tx("02", 358, 202, 70, 28, 18, { color: BLUE, bold: true }),
  tx("鉴权与成员校验", 312, 242, 230, 36, 27, { bold: true }),
  tx("JWT / RBAC", 328, 344, 190, 30, 21, { color: MUTED }),
  tx("03", 628, 202, 70, 28, 18, { color: BLUE, bold: true }),
  tx("数据库提交", 598, 242, 190, 36, 27, { bold: true }),
  tx("EF Core", 598, 344, 190, 30, 21, { color: MUTED }),
  tx("04", 898, 202, 70, 28, 18, { color: BLUE, bold: true }),
  tx("生成通知事件", 858, 242, 210, 36, 27, { bold: true }),
  tx("Interceptor", 868, 344, 190, 30, 21, { color: MUTED }),
  tx("05", 1128, 202, 70, 28, 18, { color: BLUE, bold: true }),
  tx("分组推送", 1088, 242, 150, 36, 27, { bold: true }),
  tx("SignalR", 1088, 344, 150, 30, 21, { color: MUTED }),
  rect(42, 450, 1196, 128, PALE_BLUE),
  tx("为什么这样设计？", 66, 478, 250, 32, 22, { color: BLUE, bold: true }),
  tx("数据库写入一旦成功，即使瞬时推送失败，也只记录警告，不把成功业务误报成 HTTP 500；客户端重连后仍可从列表接口取回已存数据。", 66, 526, 1120, 50, 23, { bold: true }),
], "realtime-consistency-process");

// 10 — Security
composeSlide(presentation, [
  ...header("安全策略贯穿登录、权限与审计", 10),
  tx("01", 42, 150, 60, 30, 18, { color: BLUE, bold: true }),
  tx("JWT 鉴权", 42, 194, 240, 40, 30, { bold: true }),
  tx("校验 issuer、audience、有效期、签名和 jti；退出登录可撤销当前令牌。", 42, 260, 250, 136, 23, { color: MUTED }),
  rule(318, 146, 1),
  tx("02", 346, 150, 60, 30, 18, { color: BLUE, bold: true }),
  tx("多端会话", 346, 194, 240, 40, 30, { bold: true }),
  tx("client_type + session_version：同类型终端重新登录会让旧会话失效，不同类型终端可共存。", 346, 260, 250, 150, 23, { color: MUTED }),
  rule(622, 146, 1),
  tx("03", 650, 150, 60, 30, 18, { color: BLUE, bold: true }),
  tx("角色权限", 650, 194, 240, 40, 30, { bold: true }),
  tx("角色与 permissions[] 持久化；管理接口按身份授权，业务操作继续校验资源成员关系。", 650, 260, 250, 150, 23, { color: MUTED }),
  rule(926, 146, 1),
  tx("04", 954, 150, 60, 30, 18, { color: BLUE, bold: true }),
  tx("审计追踪", 954, 194, 240, 40, 30, { bold: true }),
  tx("操作日志记录关键管理动作；异常响应返回业务码与 traceId，便于定位线上问题。", 954, 260, 250, 150, 23, { color: MUTED }),
  rect(42, 500, 1196, 118, PANEL),
  tx("安全不是只看“有没有登录页”，而是把身份、会话、权限、资源归属和追踪统一进后端请求链。", 66, 538, 1148, 48, 27, { bold: true }),
], "security-four-pillars");

// 11 — Deployment diagram. Connectors first.
composeSlide(presentation, [
  ...header("部署链路可复现、可检查、可回滚", 11),
  rule(280, 304, 172, BLUE, 3), rule(700, 262, 168, BLUE, 3), rule(700, 390, 168, BLUE, 3),
  rect(42, 250, 238, 112, PANEL, { line: RULE }),
  tx("Web / App / 小程序", 64, 276, 194, 32, 26, { bold: true, align: "center" }),
  tx("HTTPS · WebSocket", 64, 322, 194, 24, 18, { color: MUTED, align: "center" }),
  rect(452, 218, 248, 216, PALE_BLUE, { line: BLUE, lineWidth: 2 }),
  tx("Nginx", 500, 248, 150, 34, 28, { color: BLUE, bold: true, align: "center" }),
  rule(494, 304, 164, BLUE, 2),
  tx("ASP.NET Core API", 478, 334, 196, 36, 27, { bold: true, align: "center" }),
  tx("Docker Compose", 478, 388, 196, 26, 19, { color: MUTED, align: "center" }),
  rect(868, 218, 370, 88, PANEL, { line: RULE }),
  tx("PostgreSQL", 894, 242, 180, 34, 26, { bold: true }),
  tx("数据 + 自动重试", 1060, 246, 150, 28, 19, { color: MUTED, align: "right" }),
  rect(868, 346, 370, 88, PANEL, { line: RULE }),
  tx("MinIO / Local", 894, 370, 180, 34, 26, { bold: true }),
  tx("文件存储可切换", 1060, 374, 150, 28, 19, { color: MUTED, align: "right" }),
  rect(42, 514, 1196, 98, "#FFFFFF", { line: RULE }),
  tx("健康检查", 66, 540, 150, 30, 21, { color: BLUE, bold: true }),
  tx("/health/live 进程存活", 244, 540, 250, 30, 21),
  tx("/health 数据库连通", 506, 540, 250, 30, 21),
  tx("异常 traceId", 776, 540, 180, 30, 21),
  tx("WebSocket Upgrade", 984, 540, 220, 30, 21),
], "deployment-runtime-diagram");

// 12 — Verification metrics (Codex Grid slide 19 style)
composeSlide(presentation, [
  ...header("实际验证为完成度提供硬证据", 12, "验证日期：2026-07-19；来自 dotnet test / Vite / Gradle / uni-app 构建输出"),
  tx("所有数字均来自本次现场执行，不照抄旧版项目文档。", 42, 118, 900, 34, 24, { color: MUTED }),
  rect(42, 190, 276, 366, PANEL), rect(348, 190, 276, 366, PANEL), rect(654, 190, 276, 366, PANEL), rect(960, 190, 278, 366, PANEL),
  tx("41 / 41", 70, 250, 220, 78, 50, { color: BLUE, bold: true }),
  tx("后端自动化测试", 70, 352, 220, 34, 24, { bold: true }),
  tx("0 失败 · 0 跳过\nRelease 构建", 70, 414, 220, 80, 22, { color: MUTED }),
  tx("635", 376, 250, 220, 78, 50, { color: BLUE, bold: true }),
  tx("Web modules", 376, 352, 220, 34, 24, { bold: true }),
  tx("Vite 生产构建通过\n路由级代码拆分", 376, 414, 220, 80, 22, { color: MUTED }),
  tx("2 / 2", 682, 250, 220, 78, 50, { color: BLUE, bold: true }),
  tx("端侧目标构建", 682, 352, 220, 34, 24, { bold: true }),
  tx("Android assembleDebug\n微信小程序通过", 682, 414, 220, 80, 21, { color: MUTED }),
  tx("142", 988, 250, 220, 78, 50, { color: BLUE, bold: true }),
  tx("HTTP actions", 988, 352, 220, 34, 24, { bold: true }),
  tx("16 控制器\n36 个 DbSet", 988, 414, 220, 80, 22, { color: MUTED }),
  tx("已知边界：Android 单元测试任务存在测试类加载失败；Web Messages 构建块约 1.64 MB。", 42, 590, 1160, 36, 22, { color: MUTED }),
], "verification-metrics");

// 13 — Team allocation
composeSlide(presentation, [
  ...header("4 开发 + 2 运维形成并行闭环", 13, "团队角色依据：项目初稿；成果描述依据：最终代码与交付物"),
  rect(42, 126, 1196, 88, PALE_BLUE),
  tx("组长 / 后端负责人", 66, 150, 286, 34, 26, { color: BLUE, bold: true }),
  tx("拆任务 · 定接口 · 控进度 · Review · 联调兜底 · 汇报闭环", 370, 150, 830, 34, 25, { bold: true }),
  rule(42, 250, 1196),
  tx("成员 B", 42, 282, 110, 30, 19, { color: MUTED }),
  tx("前端负责人", 42, 322, 220, 36, 28, { bold: true }),
  tx("框架、登录权限、主布局、管理后台", 42, 378, 220, 78, 21, { color: MUTED }),
  rule(288, 270, 1),
  tx("成员 C", 316, 282, 110, 30, 19, { color: MUTED }),
  tx("前端业务", 316, 322, 220, 36, 28, { bold: true }),
  tx("IM、文档、云盘、日历、审批页面", 316, 378, 220, 78, 21, { color: MUTED }),
  rule(562, 270, 1),
  tx("成员 D", 590, 282, 110, 30, 19, { color: MUTED }),
  tx("后端业务", 590, 322, 220, 36, 28, { bold: true }),
  tx("业务接口、数据模型、SignalR 逻辑", 590, 378, 220, 78, 21, { color: MUTED }),
  rule(836, 270, 1),
  tx("成员 E", 864, 282, 110, 30, 19, { color: MUTED }),
  tx("部署与数据", 864, 322, 150, 36, 28, { bold: true }),
  tx("Docker、数据库、Nginx、演示环境", 864, 378, 150, 90, 21, { color: MUTED }),
  rule(1040, 270, 1),
  tx("成员 F", 1068, 282, 110, 30, 19, { color: MUTED }),
  tx("测试与文档", 1068, 322, 150, 36, 28, { bold: true }),
  tx("用例、Bug、截图、验收材料", 1068, 378, 150, 90, 21, { color: MUTED }),
  rect(42, 526, 1196, 94, PANEL),
  tx("组长的核心成果不是“写得最多”，而是让接口、人员、环境与验收在同一节奏上收敛。", 66, 558, 1148, 42, 27, { bold: true }),
], "team-allocation");

// 14 — Boundaries
composeSlide(presentation, [
  ...header("主动暴露边界，下一步才更可信", 14),
  tx("当前边界", 42, 134, 520, 36, 24, { color: MUTED, bold: true }),
  tx("下一阶段", 678, 134, 520, 36, 24, { color: BLUE, bold: true }),
  rule(42, 188, 1196),
  tx("Messages 构建块约 1.64 MB", 42, 226, 520, 36, 28, { bold: true }),
  tx("按 Agora / 消息 / 编辑器继续懒加载与拆包", 678, 226, 520, 36, 27, { bold: true }),
  rule(42, 286, 1196),
  tx("文档支持编辑、评论与版本，但非多光标共编", 42, 322, 520, 54, 25, { bold: true }),
  tx("接入 Yjs / CRDT，增加在线成员与冲突合并", 678, 322, 520, 54, 25, { bold: true }),
  rule(42, 392, 1196),
  tx("Android Debug 构建通过；单元测试任务类加载失败", 42, 428, 520, 54, 25, { bold: true }),
  tx("修复测试执行链，补 Compose UI Test 与 Android CI", 678, 428, 520, 54, 25, { bold: true }),
  rule(42, 498, 1196),
  tx("演示依赖实时后端与 WebSocket 代理", 42, 534, 520, 48, 25, { bold: true }),
  tx("补监控、断线重连指标、离线队列与压测", 678, 534, 520, 48, 25, { bold: true }),
], "boundaries-next-step");

// 15 — Close (Codex Grid slide 26 style)
composeSlide(presentation, [
  tx("结论", 42, 42, 180, 36, 24, { color: BLUE, bold: true }),
  tx("我们交付的是\n可运行的协作系统", 42, 154, 1040, 184, 68, { bold: true, autoFit: "none" }),
  tx("多端入口", 42, 430, 180, 34, 22, { color: MUTED }),
  tx("统一数据", 318, 430, 180, 34, 22, { color: MUTED }),
  tx("工程验证", 594, 430, 180, 34, 22, { color: MUTED }),
  tx("清晰边界", 870, 430, 180, 34, 22, { color: MUTED }),
  tx("Web / 桌面 / App / 小程序", 42, 474, 240, 54, 24, { bold: true }),
  tx("账号、组织、消息与文件", 318, 474, 240, 54, 24, { bold: true }),
  tx("构建、测试、部署与健康检查", 594, 474, 240, 54, 24, { bold: true }),
  tx("性能、Android 测试与共编路线", 870, 474, 300, 54, 22, { bold: true }),
  rect(42, 584, 1196, 70, PALE_BLUE),
  tx("建议现场演示：登录 → 发群消息 → 新建文档 → 发起审批 → 查看管理后台", 66, 604, 1148, 34, 25, { color: BLUE, bold: true }),
  tx("Q&A", 1160, 42, 78, 28, 18, { color: MUTED, align: "right" }),
], "closing-synthesis");

function qaPair(nodes, left, top, question, answer) {
  nodes.push(tx(question, left, top, 540, 38, 25, { color: BLUE, bold: true }));
  nodes.push(tx(answer, left, top + 50, 540, 96, 20, { color: MUTED }));
}

// 16 — Q&A: choices and architecture
{
  const nodes = [...header("答辩问题：技术选择与整体架构", 16, "附录：建议回答请结合现场演示简洁复述")];
  qaPair(nodes, 42, 130, "Q1 为什么选 Vue 3 + ASP.NET Core？", "Vue 3 适合组件化多页面工作台；ASP.NET Core 8 在鉴权、EF Core、SignalR 与部署方面形成完整技术链，且与团队能力匹配。选择标准是可交付与可维护，而不是追求最潮技术。");
  qaPair(nodes, 678, 130, "Q2 为什么没有拆成微服务？", "当前是实训规模，模块化单体能降低部署与联调成本。我们已按 Controller、Service、Domain、Data 分层；当团队和流量增长后，再按 IM、文档、文件拆服务更合适。");
  qaPair(nodes, 42, 386, "Q3 四端如何保证数据一致？", "四端不各自保存业务真相，而是共享 REST API、JWT 身份和 PostgreSQL 数据；SignalR 负责实时提示，列表接口负责恢复与最终一致。");
  qaPair(nodes, 678, 386, "Q4 为什么 Android 采用原生 Compose？", "原生方案更适合相机、麦克风、生命周期和 RTC 场景。Compose 用状态驱动 UI，StateFlow 统一状态，SignalR 与 Agora 分别承担实时消息和音视频；小程序仍用 uni-app 提高交付效率。");
  composeSlide(presentation, nodes, "qa-architecture");
}

// 17 — Q&A: security, consistency, testing
{
  const nodes = [...header("答辩问题：安全、一致性与测试", 17, "附录：回答先讲机制，再给代码或测试证据")];
  qaPair(nodes, 42, 130, "Q5 JWT 泄露或退出后怎么办？", "Token 带 jti，后端维护撤销表；每次认证还校验客户端会话版本。退出可撤销当前令牌，同类型客户端重新登录可让旧会话失效。");
  qaPair(nodes, 678, 130, "Q6 推送失败会不会丢数据？", "业务数据先提交数据库，再发送 SignalR。推送失败只记录警告，不把成功写入误报成失败；客户端重连后可通过列表接口恢复。");
  qaPair(nodes, 42, 386, "Q7 如何证明项目不是只能演示？", "本次实际执行后端 41/41 测试、Web 生产构建、Android assembleDebug 和微信小程序构建，核心交付目标通过；Android 单元测试类加载问题也被如实记录为质量边界。");
  qaPair(nodes, 678, 386, "Q8 文件存储为什么做两套？", "本地存储便于开发与演示，MinIO 适合服务器对象存储。它们实现同一 IFileStorageService，靠配置切换，业务控制器无需改代码。");
  composeSlide(presentation, nodes, "qa-security-testing");
}

// 18 — Q&A: leader and boundaries
{
  const nodes = [...header("答辩问题：组长贡献与项目边界", 18, "附录：组长回答要强调决策、协调与闭环证据")];
  qaPair(nodes, 42, 130, "Q9 你作为组长做了什么？", "我负责范围取舍、任务拆解、接口规范、关键后端能力、代码 Review、跨端联调、风险收敛和最终汇报。我的价值是让六个人的产出能合成一个可验收系统。");
  qaPair(nodes, 678, 130, "Q10 最大技术难点是什么？", "多端实时状态最难：既要实时，又不能让瞬时网络失败破坏业务成功语义。最终采用先落库、后推送、重连再拉取的策略。");
  qaPair(nodes, 42, 386, "Q11 哪些功能还不完整？", "文档尚未实现 CRDT 多光标共编；Messages 包体偏大；Android 单元测试任务仍有类加载问题。Debug APK 与小程序构建已通过，以上边界都有明确改进路径。");
  qaPair(nodes, 678, 386, "Q12 如果再给一周怎么安排？", "先修复 Android 测试执行链并接入 CI，同时拆分消息与会议包体；再接 Yjs 文档协同；最后补可观测性、离线队列与并发压测，用指标验证改进。");
  composeSlide(presentation, nodes, "qa-leader-boundaries");
}

await fs.mkdir(PREVIEW, { recursive: true });
await fs.mkdir(LAYOUT, { recursive: true });
await fs.mkdir(`${ROOT}/outputs`, { recursive: true });

for (const [index, slide] of presentation.slides.items.entries()) {
  const stem = `slide-${String(index + 1).padStart(2, "0")}`;
  const png = await presentation.export({ slide, format: "png", scale: 1 });
  await fs.writeFile(`${PREVIEW}/${stem}.png`, new Uint8Array(await png.arrayBuffer()));
  const layout = await slide.export({ format: "layout" });
  await fs.writeFile(`${LAYOUT}/${stem}.json`, await layout.text(), "utf8");
}

const montage = await presentation.export({ format: "webp", montage: true, scale: 1 });
await fs.writeFile(`${TMP}/deck-montage.webp`, new Uint8Array(await montage.arrayBuffer()));

const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(FINAL);
console.log(`WROTE ${FINAL}`);
