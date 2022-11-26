using aop;
using aop.Controllers;
using Castle.DynamicProxy;
using Lithium.AOP;
using Lithium.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<IForecastService>(sp =>
{
    var generator = new ProxyGenerator();
    var proxy = generator.CreateInterfaceProxyWithTarget<IForecastService>(
        new SimpleForecastService(),
        new LogInterceptor(
            sp.GetRequiredService<ILoggerFactory>(),
            new[]
            {
                new SimpleInvocationAcceptor()
            }));
    return proxy;
});
builder.Services.AddTransient<WeatherForecastController>();
builder.Services.AddControllers().AddControllersAsServices();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
