import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const workDir = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\work\presentations\fang_feishu_report\tmp\enhanced-rich-report`;
const outputPath = String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\outputs\仿飞书协同办公平台-第7组项目汇报-图文充实汇报版.pptx`;

const assets = {
  webWiki: String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\work\presentations\fang_feishu_report\tmp\assets\web-wiki.png`,
  webAdmin: String.raw`C:\Users\duyiyan\Documents\Codex\2026-07-19\wbe-2\work\presentations\fang_feishu_report\tmp\assets\web-admin.png`,
  miniContacts: path.join(workDir, "assets", "mini-contacts-top.png"),
  miniApprovals: path.join(workDir, "assets", "mini-approvals-top.png"),
};

const notes = [
  "各位老师好，我们是第7组。我们汇报的是仿飞书协同办公平台。这个项目的重点不是只复刻几个页面，而是把多端入口、统一数据、实时协作和工程交付串成一套可运行系统。下面先看最终交付结果。",
  "项目用10天形成MVP。Web有24个核心业务路由，Electron桌面端可以打包，Android原生Debug APK构建通过，小程序生产构建通过。这里想强调，交付重点已经从页面数量转向多端共享数据和完整工程闭环。",
  "总体架构可以概括为一个账号、一套API、四类客户端。Web、桌面端、Android和小程序只是不同交互入口，账号、组织、消息和业务数据都由ASP.NET Core统一处理。client_type只区分终端会话，不会复制一份业务数据。",
  "业务上我们归纳为四条主线：沟通、内容、流程和组织治理。老师可以把它理解成从聊天开始，继续完成文档协作、日程审批和权限管理，用户不需要在多套系统之间切换。下一页先看最核心的即时通信。",
  "这张图是真实后端账号登录后的群聊页面。会话列表、成员和历史消息来自后端；SignalR负责把新消息和状态变化实时推到客户端，REST接口负责持久化和历史查询。演示时可以发送一条消息，再说明刷新后数据仍然存在。",
  "内容协作不只包含新建文档，还覆盖评论、版本、协作者、文件移动、分享和回收站。两张图分别展示文档与云盘页面。这里的价值是把内容的创建、治理和回收放在同一个账号体系里。",
  "这一页补充两个容易被忽略的能力。左侧知识库把零散文档按空间和节点组织起来；右侧管理后台统一维护用户、部门、角色和操作日志。因此平台既能支持日常协作，也具备组织治理的基础。",
  "端侧技术按场景分工。Web和Electron共用Vue 3业务界面，减少重复开发；Android采用Kotlin和Jetpack Compose，适合相机、麦克风和RTC场景；小程序使用uni-app，提高移动入口的交付效率。不同端最终都调用同一套API。",
  "后端不是只有增删改查。它同时处理JWT鉴权、会话撤销、角色权限、SignalR实时事件、文件存储和健康检查。16个控制器、142个HTTP动作和36个DbSet说明业务能力已经覆盖主要模块。",
  "一致性的关键是先落库，再推送。客户端提交后先完成鉴权和成员校验，再由EF Core写入数据库，成功后生成通知事件并通过SignalR分组推送。即使即时推送失败，客户端重连后仍可通过列表接口恢复数据。",
  "安全策略贯穿整个请求链。JWT确认身份，session_version控制会话撤销，角色和permissions控制资源访问，操作日志和traceId帮助追踪问题。回答老师时可以强调，安全不是只有登录页，而是每个业务接口都要再次校验。",
  "部署入口由Nginx统一代理HTTPS和WebSocket，ASP.NET Core API通过Docker Compose运行，PostgreSQL存业务数据，文件可在Local与MinIO之间切换。健康检查分别验证进程和数据库连接，便于发现问题和回滚。",
  "这页是完成度证据。后端自动化测试41项全部通过，Web生产构建通过，Android assembleDebug和微信小程序构建通过。数字来自本次实际执行。我们也保留了已知边界，没有把构建通过说成所有质量问题都解决。",
  "团队采用4开发加2运维的并行方式。我作为组长和后端负责人，主要负责范围取舍、任务拆分、接口规范、关键后端能力、代码Review、跨端联调和最终汇报。组长价值是让六个人的产出按同一接口和验收节奏收敛。",
  "我们主动说明当前边界。Messages构建块仍偏大，文档还不是CRDT多人光标共编，Android测试执行链还需要完善，演示依赖实时后端。下一阶段会依次处理拆包、Yjs协同、Android CI和监控压测。",
  "桌面端复用Web界面，所以登录、日历和文档体验可以保持一致；小程序使用uni-app独立实现，但账号和业务数据仍来自统一后端。左图和右图都是本次实际运行，不是静态设计稿。",
  "移动入口不仅能看消息。左侧通讯录按部门和好友关系展示成员；右侧审批能够查看申请状态并提交新申请。它们与Web端共享账号、组织和审批实例，所以同一用户换端后仍能继续处理业务。",
  "最后总结：我们交付的是一套可运行的协作系统。它有多端入口、统一数据、可验证的构建与测试，也明确说明了尚未完成的能力。现场演示建议按登录、发群消息、新建文档、发起审批、查看后台的顺序完成。",
  "如果老师问技术选择，先回答团队和场景匹配，再补充可维护性；如果问为什么不拆微服务，回答当前规模下模块化单体降低部署与联调成本，同时已经按Controller、Service、Domain和Data分层，为后续拆分保留边界。",
  "如果老师问安全和一致性，先讲数据先落库、再推送；再讲JWT、session_version、RBAC和审计。推送失败不会丢业务数据，客户端重连后可以通过REST列表恢复。测试方面要同时说通过项和已知边界，避免过度承诺。",
  "如果老师问组长贡献，回答范围取舍、任务拆分、接口规范、跨端联调、风险验收和最终汇报。最大难点是多端实时状态既要及时又不能因网络失败破坏数据一致性，因此采用先落库、后推送、可重连恢复的策略。",
];

async function writeBlob(filePath, blob) {
  await fs.writeFile(filePath, new Uint8Array(await blob.arrayBuffer()));
}

function parseInspect(ndjson) {
  return ndjson.split(/\r?\n/).filter((line) => line.trim()).map((line) => JSON.parse(line));
}

function findTextId(records, slide, text) {
  const match = records.find(
    (record) => record.kind === "textbox" && record.slide === slide && record.text === text,
  );
  if (!match) throw new Error(`Text anchor not found on slide ${slide}: ${text}`);
  return match.id;
}

function replaceText(presentation, records, slide, before, after) {
  presentation.resolve(findTextId(records, slide, before)).text.replace(before, after);
}

async function replaceSlideImages(presentation, records, slideNumber, replacements) {
  const imageRecords = records
    .filter((record) => record.kind === "image" && record.slide === slideNumber)
    .sort((a, b) => a.bbox[0] - b.bbox[0]);
  if (imageRecords.length !== replacements.length) {
    throw new Error(`Expected ${replacements.length} images on slide ${slideNumber}, found ${imageRecords.length}`);
  }

  const slide = presentation.slides.items[slideNumber - 1];
  for (const record of imageRecords) presentation.resolve(record.id).delete();

  for (const replacement of replacements) {
    const bytes = new Uint8Array(await fs.readFile(replacement.path));
    slide.images.add({
      blob: bytes,
      contentType: "image/png",
      alt: replacement.alt,
      fit: "cover",
      position: replacement.position,
      geometry: "roundRect",
      borderRadius: 18,
    });
  }
}

async function main() {
  const renderDir = path.join(workDir, "final-renders");
  const layoutDir = path.join(workDir, "final-layouts");
  const qaDir = path.join(workDir, "qa");
  await fs.rm(renderDir, { recursive: true, force: true });
  await fs.rm(layoutDir, { recursive: true, force: true });
  await fs.mkdir(renderDir, { recursive: true });
  await fs.mkdir(layoutDir, { recursive: true });
  await fs.mkdir(qaDir, { recursive: true });
  await fs.mkdir(path.dirname(outputPath), { recursive: true });

  const presentation = await PresentationFile.importPptx(
    await FileBlob.load(path.join(workDir, "template-starter.pptx")),
  );
  const initial = await presentation.inspect({ kind: "slide,textbox,image", maxChars: 160000 });
  const records = parseInspect(initial.ndjson);

  const beforeWeb = presentation.slides.items[6];
  const beforeMobile = presentation.slides.items[16];
  await writeBlob(path.join(qaDir, "before-slide-07.png"), await presentation.export({ slide: beforeWeb, format: "png", scale: 1 }));
  await writeBlob(path.join(qaDir, "before-slide-17.png"), await presentation.export({ slide: beforeMobile, format: "png", scale: 1 }));

  replaceText(presentation, records, 7, "内容协作覆盖创建、治理与回收", "业务已延伸到知识沉淀与组织治理");
  replaceText(presentation, records, 7, "截图：真实后端数据；依据：Documents / Files / Wiki 接口", "截图：真实后端数据；依据：Wiki / Users / Roles / Audit 接口");
  replaceText(presentation, records, 7, "06", "07");
  replaceText(presentation, records, 7, "云文档", "知识库");
  replaceText(presentation, records, 7, "创建与编辑、评论、版本历史、协作者、可见性、版本恢复与软删除。", "空间与节点组织零散文档，支持成员、搜索与后续持续沉淀。");
  replaceText(presentation, records, 7, "云盘与知识库", "组织治理");
  replaceText(presentation, records, 7, "文件夹、上传下载、预览、移动、分享、回收站；知识库支持空间、节点、成员和搜索。", "用户、部门、角色、数据字典和操作日志集中管理，权限边界可追踪。");

  await replaceSlideImages(presentation, records, 7, [
    {
      path: assets.webWiki,
      alt: "Web知识库真实运行截图",
      position: { left: 58.8, top: 140, width: 534.4, height: 334 },
    },
    {
      path: assets.webAdmin,
      alt: "Web用户管理真实运行截图",
      position: { left: 665.8, top: 140, width: 534.4, height: 334 },
    },
  ]);

  const pageRenumbers = [
    [8, "07", "08"], [9, "08", "09"], [10, "09", "10"], [11, "10", "11"],
    [12, "11", "12"], [13, "12", "13"], [14, "13", "14"], [15, "14", "15"],
    [16, "15", "16"], [19, "17", "19"], [20, "18", "20"], [21, "19", "21"],
  ];
  for (const [slide, before, after] of pageRenumbers) {
    replaceText(presentation, records, slide, before, after);
  }

  replaceText(presentation, records, 17, "内容协作覆盖创建、治理与回收", "小程序继续覆盖通讯录与审批流程");
  replaceText(presentation, records, 17, "截图：真实后端数据；依据：Documents / Files / Wiki 接口", "截图：本次实际运行；依据：uni-app H5 / 小程序同源页面");
  replaceText(presentation, records, 17, "06", "17");
  replaceText(presentation, records, 17, "云文档", "移动通讯录");
  replaceText(presentation, records, 17, "创建与编辑、评论、版本历史、协作者、可见性、版本恢复与软删除。", "按部门查看成员，支持好友关系和用户发现；数据与 Web 通讯录一致。");
  replaceText(presentation, records, 17, "云盘与知识库", "移动审批");
  replaceText(presentation, records, 17, "文件夹、上传下载、预览、移动、分享、回收站；知识库支持空间、节点、成员和搜索。", "查看申请状态并提交新申请，审批实例由统一后端接口处理。");

  replaceText(
    presentation,
    records,
    20,
    "本地存储便于开发与演示，MinIO 适合服务器对象存储。它们实现同一 IFileStorageService，靠配置切换，业务控制器无需改代码。",
    "本地存储用于开发演示，MinIO 用于对象存储；二者共用 IFileStorageService，可按配置切换，业务接口无需改动。",
  );

  await replaceSlideImages(presentation, records, 17, [
    {
      path: assets.miniContacts,
      alt: "uni-app小程序同源通讯录真实运行截图",
      position: { left: 58.8, top: 140, width: 534.4, height: 334 },
    },
    {
      path: assets.miniApprovals,
      alt: "uni-app小程序同源审批真实运行截图",
      position: { left: 665.8, top: 140, width: 534.4, height: 334 },
    },
  ]);

  if (notes.length !== presentation.slides.items.length) {
    throw new Error(`Expected ${presentation.slides.items.length} note entries, found ${notes.length}`);
  }
  presentation.slides.items.forEach((slide, index) => {
    slide.speakerNotes.textFrame.setText(notes[index]);
    slide.speakerNotes.setVisible(true);
  });

  await writeBlob(path.join(qaDir, "after-slide-07.png"), await presentation.export({ slide: presentation.slides.items[6], format: "png", scale: 1 }));
  await writeBlob(path.join(qaDir, "after-slide-17.png"), await presentation.export({ slide: presentation.slides.items[16], format: "png", scale: 1 }));

  for (const [index, slide] of presentation.slides.items.entries()) {
    const stem = `slide-${String(index + 1).padStart(2, "0")}`;
    await writeBlob(path.join(renderDir, `${stem}.png`), await presentation.export({ slide, format: "png", scale: 1 }));
    await fs.writeFile(path.join(layoutDir, `${stem}.layout.json`), await (await slide.export({ format: "layout" })).text());
  }

  await writeBlob(path.join(workDir, "final-montage.webp"), await presentation.export({ format: "webp", montage: true, scale: 1 }));
  const inspect = await presentation.inspect({
    kind: "slide,textbox,shape,image,table,chart,notes,thread,layout",
    maxChars: 240000,
  });
  await fs.writeFile(path.join(workDir, "final-deck.inspect.ndjson"), inspect.ndjson);

  const pptx = await PresentationFile.exportPptx(presentation);
  await pptx.save(outputPath);
  console.log(JSON.stringify({ outputPath, slides: presentation.slides.items.length, notes: notes.length }));
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
