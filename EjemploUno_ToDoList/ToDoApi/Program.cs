using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToDoApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ToDoApi.BackgroundServices;
using ToDoApi.Services;
using ToDoApi.Services.Notifications;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
    
    builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HostingConnection")));

builder.Services.AddIdentityCore<IdentityUser>().AddEntityFrameworkStores<ToDoDbContext>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

builder.Services.TryAddSingleton(TimeProvider.System);//Reloj
builder.Services.Configure<OverdueReviewOptions>(builder.Configuration.GetSection(OverdueReviewOptions.SectionName));//Overdue con Json
builder.Services.AddSingleton<IOverdueTaskNotifier, FileOverdueTaskNotifier>();//Canal de aviso
builder.Services.AddScoped<IOverdueTaskReviewService, OverdueTaskReviewService>();
builder.Services.AddHostedService<OverdueTaskBackgroundService>();//Arranca el job con la app
//Notas para acordarse

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
//    app.UseSwagger();
//   app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();