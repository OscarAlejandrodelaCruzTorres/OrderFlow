using OrderFlow.Api.Extensions;
using OrderFlow.Composition;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOrderFlow(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddApiErrors();
builder.Services.AddOpenApi();
builder.Services.AddApiSecurity();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();