# Fake News Detection

A school project that analyzes a news article and returns `Fake`, `Real`, or `Uncertain` with a confidence score and explanation.

## Projects

- `FND.API` – ASP.NET Core Web API
- `FND.Application` – analysis services
- `FND.Domain` – entities and verdict types
- `FND.Infastructure` – EF Core/MySQL persistence
- `FakeNewsDetector.Extension` – Chrome/Edge browser extension

## Run locally

Requirements: .NET 9 SDK and MySQL.

1. Create a MySQL database named `fake_news_detector`.
2. Configure the connection string and optional Google Fact Check API key using user secrets or environment variables. Do not commit real passwords or API keys.
3. Run:

```powershell
dotnet ef database update --project FND.Infastructure --startup-project FND.API
dotnet run --project FND.API --urls "http://0.0.0.0:5011"
```

Open `http://localhost:5011/swagger`. The main endpoint is `POST /api/news/analyze` with `title`, `content`, and optional `source` fields.

For a deployed API, update `API_URL` in `FakeNewsDetector.Extension/popup.js` and add the deployed URL to `host_permissions` in `manifest.json`.

## Load the extension

Open `chrome://extensions` or `edge://extensions`, enable Developer mode, choose **Load unpacked**, and select `FakeNewsDetector.Extension`.

## Limitations

This is an educational screening tool, not proof that an article is true or false. Results use content signals and, when configured, published fact checks.
