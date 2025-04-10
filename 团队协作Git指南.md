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

1.在CODING中点击个人账户设置，确认自己绑定的邮箱和密码

2.在某盘（以下假设是在D盘）新建文件夹**TimeController** ，在空白文件夹**TimeController**右键单击并选择**open git bash here** ，**不需要进行本地环境初始化**，按以下步骤操作：

```bash
git config --global user.name "你的名字（真名）"
git config --global user.email "你的邮箱（CODING里绑定的邮箱）"
git clone https://e.coding.net/g-cwiu5526/TimeController/time-manager-core.git
```

首次拉取仓库会提示填写凭据（会有弹窗），在弹窗中，用户名一栏输入自己绑定的邮箱，密码一栏输入个人账户中的密码。

---

## 🌿 功能开发流程



打开**D:/TimeController/time-manager-core**文件夹，空白处右键单击并点**open git bash here** 

**首次拉取：**

### 1. 拉取远程分支列表：

```bash
git fetch origin
```

### 2.切换到自己负责的功能分支

```bash
git checkout -b feature/自己负责的分支 origin/feature/自己负责的分支
```

|     feature/dev     |     项目开发主分支（用于合并代码）      |
| :-----------------: | :-------------------------------------: |
| feature/casual-mode |          咸鱼日常模式功能开发           |
| feature/month-view  |          强管理月视图功能开发           |
|  feature/week-view  |          强管理周视图功能开发           |
| feature/review-core |           强管理复盘功能开发            |
| feature/navigation  | 整体UI框架开发（导航栏+主内容区域切换） |

然后进行开发或其他修改

### 3.提交本地修改并推送到远程仓库

```bash
git add .
git commit -m "本次的信息（详见提交信息规范）"
git push origin feature/自己负责的分支
```

示例：`git commit -m "feat: 添加复盘界面交互逻辑" `



**日常拉取：**

打开**D:/TimeController/time-manager-core**文件夹，空白处右键单击并点**open git bash here** 

### 1.切换到自己负责的分支

```bash
git checkout feature/自己负责的分支
```

然后进行代码开发：打开VS、修改代码、保存。



### 2.提交修改

```bash
git add .
git commit -m "本次的信息（详见提交信息规范）"
git push origin feature/casual-mode
```



---

## 🧾 什么是 PR？

**PR = Pull Request（合并请求）**

> 👉 它是一种团队协作中**代码合并前的“申请与审查”流程**。

你可以这样理解它：

> “我写好一个功能啦，现在我想把它合并进主线（feature/dev），但我不直接动手，而是**先发一个‘请求’，请你帮我看看这段代码行不行**。”

## 💡 PR 的场景

举例：

你在 `feature/casual-mode` 上开发完成，想把它合并到 `feature/dev`：

1. **你发起一个 PR（合并请求)**
2. PR 内容是：
   - 来源分支：`feature/casual-mode`
   - 目标分支：`feature/dev`
   - 附带说明文字，例如“完成咸鱼模式日历显示和任务卡片交互”
3. 同事看到 PR，会：
   - 点进去 Review（查看你提交的代码）
   - 可以评论 / 提建议 / 点同意
4. 一切 OK 之后 👉 合并（Merge）！

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
| 拉代码 | `git pull origin feature/你自己负责的分支` |
| 新建分支 | `git checkout -b feature/你自己负责的分支` |
| 提交修改 | `git add . && git commit -m "本次的信息（详见提交信息规范）"` |
| 推送远程 | `git push origin feature/你自己负责的分支` |

---

如需更多协作建议或 Git 使用技巧，请联系项目负责人或在 Wiki 添加补充内容
