using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5080");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UsePathBase("/proxy/5080");
app.UseDefaultFiles();
app.UseStaticFiles();

// ADAPTE ces chemins si nécessaire.
var gameDirectory = "/home/code-jeu/code/c#/rpg-c";
var projectPath = "/home/code-jeu/code/c#/rpg-c/rpg.csproj";

// Historique complet de la sortie : chaque client demande "depuis l'index N".
var history = new List<string>();
var historyLock = new object();
const int MaxChunks = 5000;
var trimmed = 0; // nombre de chunks supprimés du début

Process? game = null;
var gameLock = new object();

void Append(string text)
{
    lock (historyLock)
    {
        history.Add(text);
        if (history.Count > MaxChunks)
        {
            history.RemoveAt(0);
            trimmed++;
        }
    }
}

async Task ReadStreamAsync(StreamReader reader, string prefix)
{
    var buffer = new char[1024];
    try
    {
        while (true)
        {
            int count = await reader.ReadAsync(buffer.AsMemory());
            if (count == 0) break;
            Append(prefix + new string(buffer, 0, count));
        }
    }
    catch
    {
        // Flux fermé.
    }
}

bool IsRunning()
{
    lock (gameLock)
    {
        try { return game is not null && !game.HasExited; }
        catch { return false; }
    }
}

void StartGame()
{
    lock (gameLock)
    {
        if (IsRunning()) return;

        var startInfo = new ProcessStartInfo
        {
            FileName = "/usr/bin/dotnet",
            WorkingDirectory = gameDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        // Évite le bruit de dotnet (logo, télémétrie) dans la sortie du jeu.
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["TERM"] = "dumb";

        try
        {
            var p = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            p.Exited += (_, _) => Append("\n[Le RPG s'est arrêté]\n");
            p.Start();
            p.StandardInput.AutoFlush = true;
            game = p;

            _ = ReadStreamAsync(p.StandardOutput, "");
            _ = ReadStreamAsync(p.StandardError, "\n[ERREUR] ");
        }
        catch (Exception ex)
        {
            Append($"\n[ERREUR] Impossible de lancer le jeu : {ex.Message}\n");
        }
    }
}

void StopGame()
{
    lock (gameLock)
    {
        try
        {
            if (game is not null && !game.HasExited)
                game.Kill(entireProcessTree: true);
        }
        catch { }
    }
}

StartGame();

// GET /api/output?since=N -> renvoie les chunks à partir de l'index N (absolu)
app.MapGet("/api/output", (int? since) =>
{
    lock (historyLock)
    {
        int start = Math.Max((since ?? 0) - trimmed, 0);
        var chunks = start < history.Count ? history.GetRange(start, history.Count - start) : new List<string>();
        return Results.Ok(new
        {
            chunks,
            next = trimmed + history.Count,
            running = IsRunning()
        });
    }
});

app.MapPost("/api/input", async (InputRequest request) =>
{
    if (request?.Command is null)
        return Results.BadRequest(new { error = "Commande manquante." });

    Process? p;
    lock (gameLock) { p = game; }

    if (p is null || !IsRunning())
        return Results.Conflict(new { error = "Le RPG est arrêté." });

    try
    {
        await p.StandardInput.WriteLineAsync(request.Command);
        await p.StandardInput.FlushAsync();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapPost("/api/restart", () =>
{
    StopGame();
    lock (historyLock) { history.Clear(); trimmed = 0; }
    StartGame();
    return Results.Ok();
});

app.Lifetime.ApplicationStopping.Register(StopGame);

await app.RunAsync();

public record InputRequest(string Command);