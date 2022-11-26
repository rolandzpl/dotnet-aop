namespace aop;

public interface IForecastService
{
    IEnumerable<WeatherForecast> GetForecast();
}