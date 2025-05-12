using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDirectoryBrowser();

var app = builder.Build();

string author = "Michał Lorenc";
string port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
Console.WriteLine($"[START] Data: {DateTime.Now}, Autor: {author}, Port: {port}");

// Serwowanie strony index.html
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/weather", async (string city, string country) =>
{
    var apiKey = Environment.GetEnvironmentVariable("OWM_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.BadRequest("Brak klucza API (OWM_API_KEY)");

    var url = $"https://api.openweathermap.org/data/2.5/weather?q={city},{country}&appid={apiKey}&units=metric&lang=pl";
    using var client = new HttpClient();

    try
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(json);

        var root = parsed.RootElement;

        var result = new
        {
            Lokalizacja = root.GetProperty("name").GetString(),
            Kraj = root.GetProperty("sys").GetProperty("country").GetString(),
            Temperatura = root.GetProperty("main").GetProperty("temp").GetDouble(),
            Opis = root.GetProperty("weather")[0].GetProperty("description").GetString(),
            Wiatr = root.GetProperty("wind").GetProperty("speed").GetDouble()
        };

        return Results.Json(result);
    }
    catch (HttpRequestException e)
    {
        return Results.Problem($"Błąd HTTP: {e.Message}");
    }
});

// Healthcheck
app.MapGet("/health", () => Results.Ok("OK"));

app.Run($"http://0.0.0.0:{port}");
