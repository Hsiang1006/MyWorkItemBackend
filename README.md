# MyWorkItem Backend (任務管理系統後端)

這是一個基於 **ASP.NET Core Web API** 構建的任務管理系統後端。系統採用 N-Tier 架構設計，並針對多使用者的「共用任務」與「個人確認狀態」進行了優雅的拆分與關聯，支援完整的 JWT 驗證與角色權限管控。

## 🌐 線上 Demo 與測試帳號

您可以直接透過前端網站體驗此系統的完整功能：

- **Demo 連結**：[https://myworkitemfrontend.onrender.com](https://myworkitemfrontend.onrender.com)

**測試帳號資料**：
| 角色 | 帳號 | 密碼 | 說明 |
| :--- | :--- | :--- | :--- |
| **管理員** | `admin` | `Test1234` | 可執行任務的 CRUD (新增/修改/刪除) 操作。 |
| **一般使用者 1** | `user1` | `Test1234` | 可瀏覽任務並進行個人狀態的批次確認與撤銷。 |
| **一般使用者 2** | `user2` | `Test1234` | 各使用者的任務狀態互相獨立，互不影響。 |

---

## ✨ 核心功能

- **身分驗證與授權 (Auth)**：基於 JWT 的登入機制，並區分 Admin 與 User 角色權限。
- **任務管理 (Admin)**：
  - 工作項目的新增、修改、刪除與列表查詢。
- **個人狀態追蹤 (User)**：
  - 任務列表的分頁與排序。
  - 分離的狀態設計：僅記錄個人的 `Confirmed` 或 `Pending` 狀態。
  - 支援任務狀態的**批次確認**與**單筆撤銷**。

---

## 🛠 技術架構

- **後端框架**：ASP.NET Core Web API (.NET)
- **資料庫存取 (ORM)**：Entity Framework Core (EF Core)
- **資料庫**：PostgreSQL (透過 Npgsql)
- **API 文件**：Swagger (已配置 JWT Bearer 授權設定)

---

## 📁 專案結構

```text
MyWorkItemBackend/
├── Controllers/      # API 控制器 (接收 HTTP 請求，處理路由與授權)
├── Services/         # 商業邏輯層 (處理分頁、資料庫關聯查詢、批次邏輯)
├── Data/             # 資料存取層 (AppDbContext, 全域設定如軟刪除與 UTC 轉換)
├── Entities/         # 領域實體層 (對應 DB 的 User, Role, WorkItem 等類別)
├── Models/DTOs/      # 資料傳輸物件 (定義 API Request/Response 格式)
├── Migrations/       # EF Core 資料庫遷移紀錄
├── MyWorkItemBackend.Tests/ # xUnit 單元測試專案 (包含 Moq 與 InMemory DB 測試)
└── Program.cs        # 應用程式進入點與服務依賴注入 (DI) 配置
```

---

## ⚙️ 環境變數設定

在本地啟動或部署前，請確保配置了以下環境變數 (或在 `appsettings.json` 中設定)：

| 變數名稱 / JSON Key                   | 說明                             | 範例                                              |
| :------------------------------------ | :------------------------------- | :------------------------------------------------ |
| `ConnectionStrings:DefaultConnection` | PostgreSQL 連線字串              | `Host=...;Database=...;Username=...;Password=...` |
| `Jwt:Key`                             | JWT 簽章用的密鑰 (至少 16 字元)  | `YourSuperSecretKey...`                           |
| `Jwt:Issuer`                          | JWT 簽發者                       | `MyWorkItemBackend`                               |
| `Jwt:Audience`                        | JWT 接收者                       | `MyWorkItemFrontend`                              |
| `PORT`                                | 應用程式監聽埠號 (預設為 `8080`) | `8080`                                            |

---

## 🚀 本地啟動方式

### 先決條件

1. 安裝 [.NET SDK](https://dotnet.microsoft.com/download)
2. 安裝 PostgreSQL 並建立好空白資料庫。

### 步驟

1. **設定設定檔**
   在專案根目錄建立或修改 `appsettings.Development.json` (或環境變數)，填入資料庫連線字串與 JWT 設定。

2. **更新資料庫 (套用 Migrations)**
   開啟終端機，確保已安裝 `dotnet-ef` 工具，然後在專案目錄執行：

   ```bash
   dotnet ef database update
   ```

3. **啟動應用程式**
   ```bash
   dotnet run
   ```
   _啟動後，可以前往 `http://localhost:<埠號>/swagger` (或依據終端機顯示的 URL) 瀏覽 API 文件並進行測試。_

---

## 🧪 單元測試

本專案使用 **xUnit** 搭配 **Moq** 與 **InMemory Database** 進行高覆蓋率的單元測試，涵蓋了所有核心 Controller 與 Service 邏輯。

**執行測試**：
在專案根目錄中，執行以下指令即可跑完所有測試案例：
```bash
dotnet test MyWorkItemBackend.Tests/MyWorkItemBackend.Tests.csproj
```
