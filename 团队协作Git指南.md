# 🧑‍💻 团队协作 Git 指南（智能化时间管理系统）

本指南适用于本项目在 CODING + Git 的协作开发流程，统一团队开发习惯，提高效率并减少冲突。

---

## 🌱 分支模型说明

我们采用 **功能分支规范**（Feature-based Workflow）：

| 分支名 | 用途 |
|--------|------|
| `master` | 主分支，存放发布版本代码 |
| `develop` | 开发分支，集成所有 feature 分支 |
| `feature/xxx` | 功能开发分支，每个新功能一个分支 |

---

## 🧭 初次拉取仓库

```bash
git clone <仓库地址>
cd 项目目录
```

---

## 🌿 功能开发流程

### 1. 从 `develop` 分支拉取功能分支：

```bash
git checkout develop
git pull origin develop
git checkout -b feature/模块名-简述
```

### 2. 提交本地修改

```bash
git add .
git commit -m "feat: 添加任务复盘页面 UI"
```

### 3. 推送到远程仓库

```bash
git push origin feature/模块名-简述
```

---

## 🔁 合并流程（发起 Pull Request）

1. 登录 CODING
2. 发起合并请求（Merge Request），**目标分支选择 `develop`**
3. 指定一位组员进行 Review
4. Review 无误后合并至 develop

---

## 📦 发布版本流程（可选）

当需发布稳定版本时：

```bash
git checkout develop
git pull
git checkout -b release/vX.Y.Z
```

在 `release` 分支上做最后调整，再合并至 `master`。

---

## 🧹 功能完成后清理分支

```bash
git branch -d feature/xxx                # 本地删除
git push origin --delete feature/xxx     # 远程删除
```

---

## 📌 提交信息规范（建议使用）

```text
feat: 新增功能
fix: 修复 bug
refactor: 重构代码
docs: 修改文档
style: 格式调整（空格、缩进等）
chore: 杂项（构建流程、依赖管理等）
```

示例：

```bash
git commit -m "fix: 修复任务详情页崩溃问题"
```

---

## 🧠 常用命令速查表

| 操作 | 命令 |
|------|------|
| 拉代码 | `git pull origin develop` |
| 新建分支 | `git checkout -b feature/xxx` |
| 提交修改 | `git add . && git commit -m "xxx"` |
| 推送远程 | `git push origin feature/xxx` |
| 合并 develop 更新 | `git merge origin/develop` |

---

如需更多协作建议或 Git 使用技巧，请联系项目负责人或在 Wiki 添加补充内容。
