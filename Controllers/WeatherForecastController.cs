using Microsoft.AspNetCore.Mvc;

namespace aop.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IForecastService forecastService;
    private readonly ILogger<WeatherForecastController> logger;

    public WeatherForecastController(IForecastService forecastService, ILogger<WeatherForecastController> logger)
    {
        this.forecastService = forecastService;
        this.logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return forecastService.GetForecast();
    }
}
