using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenAISelfhost.DatabaseContext;
using OpenAISelfhost.DataContracts.Utils.Serialization.Chat;
using OpenAISelfhost.Exceptions;
using OpenAISelfhost.Service;
using OpenAISelfhost.Service.Billing;
using OpenAISelfhost.Service.Interface;
using OpenAISelfhost.Service.OpenAI;
using OpenAISelfhost.Middleware;
using OpenAISelfhost.Service.MCP;
using OpenAISelfhost.Service.ChatHistory;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddExceptionHandler<ExceptionHandler>();

builder.Services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonChatRoleConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonChatContentTypeConverter());
                });
builder.Services.AddSingleton<RsaKeyService>();
builder.Services.AddSingleton<IRsaKeyService>(sp => sp.GetRequiredService<RsaKeyService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<RsaKeyService>());
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IChatService, ChatService>();
builder.Services.AddTransient<ITransactionService, TransactionService>();
builder.Services.AddTransient<IModelService, ModelService>();
builder.Services.AddTransient<IChatHistoryService, ChatHistoryService>();
builder.Services.AddSingleton<IMCPTransportService, MCPTransportService>();
builder.Services.AddHostedService<CreditResetBackgroundService>();
builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["JWT:ValidAudience"],
                        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                        ClockSkew = TimeSpan.Zero
                    };
                });
// Wire the RSA key resolver after the service provider is built so that
// IssuerSigningKeyResolver always uses the current in-memory RSA key pair.
builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(sp =>
{
    var rsaKeyService = sp.GetRequiredService<IRsaKeyService>();
    return new PostConfigureOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        opts.TokenValidationParameters.IssuerSigningKeyResolver =
            (_, _, _, _) => rsaKeyService.GetValidationKeys();
    });
});
var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
builder.Services.AddDbContext<ServiceDatabaseContext>(options => options.UseMySql(builder.Configuration.GetConnectionString("DBConn"), serverVersion));

// Add response compression with exception for SSE content type
builder.Services.AddResponseCompression(options =>
{
    // Only compress specific mime types (exclude text/event-stream)
    options.EnableForHttps = true;
    options.MimeTypes = new[] 
    {
        "application/json",
        "application/xml",
        "text/plain",
        "text/html"
    };
    // Explicitly exclude SSE content type
    options.ExcludedMimeTypes = new[] { "text/event-stream" };
});

var app = builder.Build();

// Our custom middleware to disable compression for SSE endpoints
app.UseDisableCompressionForSSE();
app.UseWebSockets();
app.UseResponseCompression();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseExceptionHandler(options => { });
app.Run();
