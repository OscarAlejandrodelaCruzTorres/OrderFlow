using OrderFlow.Api.Errors;
using OrderFlow.Composition;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOrderFlow(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var problemDetails = context.ProblemDetails;

        if (problemDetails.Type is null || problemDetails.Type.StartsWith("https://tools.ietf.org/", StringComparison.Ordinal))
        {
            problemDetails.Type = ProblemTypes.GetProblemTypeForStatusCode(problemDetails.Status) ?? problemDetails.Type;
        }
    };
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
