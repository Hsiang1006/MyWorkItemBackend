# 第一階段：編譯階段 (Build Stage)
# 使用包含完整 SDK 的鏡像進行編譯與發佈
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 複製專案檔並執行還原 (Restore)
# 分開複製能有效利用 Docker Layer Cache，加快之後的編譯速度
COPY ["MyWorkItemBackend.csproj", "./"]
RUN dotnet restore "MyWorkItemBackend.csproj"

# 複製其餘所有原始碼並進行編譯
COPY . .
RUN dotnet build "MyWorkItemBackend.csproj" -c Release -o /app/build

# 發佈發行版本 (Publish)
FROM build AS publish
RUN dotnet publish "MyWorkItemBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 第二階段：執行階段 (Runtime Stage)
# 僅使用極小的 Runtime 鏡像，確保最終映像檔體積最小化且安全
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 暴露 Render 預設偵測的埠號 (10000) 或 .NET 預設 (8080)
EXPOSE 8080

# 從編譯階段複製發佈後的檔案到執行階段
COPY --from=publish /app/publish .

# 設定啟動指令
ENTRYPOINT ["dotnet", "MyWorkItemBackend.dll"]