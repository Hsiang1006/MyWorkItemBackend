using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using MyWorkItemBackend.Data;
using MyWorkItemBackend.Services;
using Serilog;
using Serilog.Events;

// 強制 Npgsql 使用 UTC 時區處理邏輯
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// 初始化 Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 使用 Serilog 替換預設日誌
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // 停用設定檔變更監控，以避免在 Render 環境發生 inotify 限制錯誤
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                         .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                         .AddEnvironmentVariables();

    // 確保應用程式監聽 Render 指定的埠號，並綁定到 0.0.0.0
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

    // Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 設定 CORS 政策，允許前端 Vite 連線 (預設 http://localhost:5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://myworkitemfrontend.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 註冊 AuthService (依賴注入)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkItemService, WorkItemService>();

// 設定 JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing."));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    // 自訂 401 與 403 的回傳訊息
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            // 略過預設的 Challenge 行為，避免 .NET 覆蓋我們的自訂訊息
            context.HandleResponse();

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { message = "尚未登入或憑證已過期，請重新登入", status = false });
            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("存取拒絕 (403): 使用者 {UserId} 嘗試存取 {Path}", 
                context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
                context.HttpContext.Request.Path);

            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { message = "權限不足，您沒有執行此操作的權限", status = false });
            return context.Response.WriteAsync(result);
        }
    };
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 1. 定義 Security Scheme (JWT Bearer)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "請輸入 JWT Token。格式為：Bearer {你的Token}"
    });

    // 2. 讓所有 API 預設套用這個安全要求
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(); // 加入這行來記錄 HTTP 請求詳細資訊

app.UseHttpsRedirection();

// 套用 CORS 政策
app.UseCors("AllowFrontend");

// 啟用驗證與授權
app.UseAuthentication();
app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "應用程式啟動失敗");
}
finally
{
    Log.CloseAndFlush();
}
