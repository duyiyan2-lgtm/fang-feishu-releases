# 04 · 数据库设计

## 🎯 阶段目标

输出符合 3NF、可扩展、高性能的 PostgreSQL 数据库 schema，包含 ER 图、表结构、索引、约束、迁移脚本与种子数据。

## 📝 必含章节（每篇库表设计文档）

| 章节 | 说明 | 必含配图 |
| :--: | :-- | :--: |
| 一、设计原则 | 命名、字符集、时区、3NF / 反范式决策 | ❌ |
| 二、ER 图 | 核心实体关系图 | ✅ |
| 三、表结构 | 字段、类型、约束、默认值、注释 | ✅ |
| 四、索引设计 | 主键 / 唯一 / 普通 / 复合 / 部分 / GIN | ✅ |
| 五、关联关系 | 外键策略、级联、软删除 | ✅ |
| 六、DDL 脚本 | PostgreSQL DDL，可直接执行 | ❌ |
| 七、迁移方案 | EF Core Migration / Flyway 流程 | ✅ |
| 八、初始化数据 | 种子数据脚本 | ❌ |

## 📂 命名规范

```
04-数据库设计-<模块名>-<表名>.md
```

示例：
- `04-数据库设计-IM-消息表.md`
- `04-数据库设计-用户中心-用户表.md`

## 🖼 配图要求

- ER 图（每模块 1 张总图 + 关键关系放大图）
- 字段含义表
- 索引设计图
- **≥ 5 张配图**

## 📦 产出物清单

- [ ] 全局 ER 图（`04-数据库设计-全局ER图.md`）
- [ ] 各模块表结构文档
- [ ] PostgreSQL DDL 脚本（`backend/scripts/sql/*.sql`）
- [ ] EF Core Migration 文件
- [ ] 种子数据脚本
- [ ] 数据库设计评审报告

## ✍️ 文档模板

```markdown
# <模块名> - <表名> 设计

## 一、所属模块
## 二、表用途

## 三、字段说明
| 字段 | 类型 | 必填 | 默认 | 注释 |
| :--: | :--: | :--: | :--: | :--: |
| id | uuid | ✅ | gen_random_uuid() | 主键 |
| created_at | timestamptz | ✅ | now() | 创建时间 |
| updated_at | timestamptz | ✅ | now() | 更新时间 |
| is_deleted | boolean | ✅ | false | 软删除 |

## 四、索引
| 索引名 | 字段 | 类型 | 用途 |
| :-- | :-- | :--: | :-- |
| pk_xxx | id | 主键 | |
| idx_xxx_created | created_at desc | B-Tree | 列表分页 |

## 五、关联关系
| 关联表 | 关系 | 外键 | 级联 |
| :--: | :--: | :--: | :--: |
| user | 多对一 | user_id | SET NULL |

## 六、DDL
```sql
CREATE TABLE xxx (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  is_deleted boolean NOT NULL DEFAULT false
);
```

## 七、变更记录
```
