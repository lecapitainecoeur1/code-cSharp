using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ============================ CONFIGURATION ============================

// Adresse publique : https://skz.kaantay.fr:51708
const int Port = 51708;

const string DockerImage = "rpg-image";               // image construite à partir de ton RPG (voir Dockerfile)
const string ContainerPrefix = "rpgsess-";            // préfixe des conteneurs de joueurs
const int MaxSessions = 20;                           // nombre max de joueurs en même temps
var idleTimeout = TimeSpan.FromMinutes(15);           // inactivité avant fermeture du conteneur

// --- HTTPS : 3 possibilités (par variables d'environnement) ---
//  1) CERT_PATH (+ CERT_PASSWORD)  -> certificat .pfx
//  2) CERT_PEM + KEY_PEM           -> fullchain.pem + privkey.pem
//  3) rien                         -> HTTP simple (derrière un reverse proxy qui gère le HTTPS)
var certPfx = Environment.GetEnvironmentVariable("CERT_PATH");
var certPassword = Environment.GetEnvironmentVariable("CERT_PASSWORD");
var certPem = "/etc/code-server/certs/fullchain.pem";
var keyPem = "/etc/code-server/certs/privkey.pem";

if (!string.IsNullOrWhiteSpace(certPfx))
{
    builder.WebHost.ConfigureKestrel(o =>
        o.ListenAnyIP(Port, l => l.UseHttps(certPfx, certPassword)));
}
else if (!string.IsNullOrWhiteSpace(certPem) && !string.IsNullOrWhiteSpace(keyPem))
{
    builder.WebHost.ConfigureKestrel(o =>
        o.ListenAnyIP(Port, l => l.UseHttps(X509Certificate2.CreateFromPemFile(certPem, keyPem))));
}
else
{
    builder.WebHost.UseUrls($"https://0.0.0.0:{Port}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();

// ============================ DÉMARRAGE : vérifications Docker ============================

// Supprime les conteneurs oubliés d'un précédent lancement (crash, arrêt brutal...).
var (_, orphans) = DockerCli.Run("ps", "-q", "--filter", $"name={ContainerPrefix}");
foreach (var id in orphans.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    DockerCli.Run("kill", id);

var (imageCode, _) = DockerCli.Run("image", "inspect", DockerImage);
if (imageCode != 0)
    app.Logger.LogWarning("L'image Docker '{Image}' est introuvable (ou Docker est inaccessible). " +
                          "Construis-la avec : docker build -t {Image} .", DockerImage, DockerImage);

// ============================ SESSIONS ============================

var sessions = new ConcurrentDictionary<string, GameSession>();
var sessionsLock = new object();

// Retrouve le conteneur du visiteur grâce au cookie, ou en crée un nouveau.
// Renvoie null si le serveur est plein.
GameSession? GetSession(HttpContext ctx)
{
    if (ctx.Request.Cookies.TryGetValue("rpg_sid", out var existingId)
        && sessions.TryGetValue(existingId, out var existing))
    {
        existing.LastSeen = DateTime.UtcNow;
        return existing;
    }

    lock (sessionsLock)
    {
        if (sessions.Count >= MaxSessions) return null;

        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var session = new GameSession(id, ContainerPrefix + id, DockerImage);
        sessions[id] = session;

        ctx.Response.Cookies.Append("rpg_sid", id, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(7)
        });

        session.Start();
        return session;
    }
}

IResult ServerFull() =>
    Results.Json(new { error = "Serveur plein, réessaie dans quelques minutes." }, statusCode: 503);

// Nettoyage : détruit les conteneurs inactifs (vérification chaque minute).
var cleanupTimer = new Timer(_ =>
{
    foreach (var (id, session) in sessions)
    {
        if (DateTime.UtcNow - session.LastSeen > idleTimeout && sessions.TryRemove(id, out var removed))
            removed.Stop();
    }
}, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

// ============================ API ============================

// GET /api/output?since=N -> sortie de SON conteneur à partir de l'index N
app.MapGet("/api/output", (HttpContext ctx, int? since) =>
{
    var s = GetSession(ctx);
    if (s is null) return ServerFull();
    return Results.Ok(s.ReadOutput(since ?? 0));
});

app.MapPost("/api/input", async (HttpContext ctx, InputRequest request) =>
{
    if (request?.Command is null)
        return Results.BadRequest(new { error = "Commande manquante." });

    var s = GetSession(ctx);
    if (s is null) return ServerFull();

    var (ok, error) = await s.SendAsync(request.Command);
    return ok ? Results.Ok() : Results.Conflict(new { error });
});

app.MapPost("/api/restart", (HttpContext ctx) =>
{
    var s = GetSession(ctx);
    if (s is null) return ServerFull();
    s.Restart();
    return Results.Ok();
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    cleanupTimer.Dispose();
    foreach (var s in sessions.Values) s.Stop();
});

await app.RunAsync();

// ============================ TYPES ============================

public record InputRequest(string Command);

// Petit utilitaire pour appeler la commande "docker".
public static class DockerCli
{
    public static (int ExitCode, string Output) Run(params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo("docker")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var p = Process.Start(psi);
            if (p is null) return (-1, "");
            var output = p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            if (!p.WaitForExit(10000))
            {
                try { p.Kill(); } catch { }
                return (-1, output);
            }
            return (p.ExitCode, output);
        }
        catch
        {
            return (-1, ""); // docker introuvable ou inaccessible
        }
    }
}

// Une partie = un conteneur Docker dédié + son propre historique de sortie.
public class GameSession
{
    private const int MaxChunks = 5000;

    private readonly string _containerName;
    private readonly string _image;
    private readonly List<string> _history = new();
    private readonly object _historyLock = new();
    private readonly object _gameLock = new();
    private int _trimmed; // nombre de chunks supprimés du début
    private volatile Process? _game; // processus "docker run" connecté au conteneur

    public string Id { get; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public GameSession(string id, string containerName, string image)
    {
        Id = id;
        _containerName = containerName;
        _image = image;
    }

    private void Append(string text)
    {
        lock (_historyLock)
        {
            _history.Add(text);
            if (_history.Count > MaxChunks)
            {
                _history.RemoveAt(0);
                _trimmed++;
            }
        }
    }

    public bool IsRunning()
    {
        var p = _game;
        try { return p is not null && !p.HasExited; }
        catch { return false; }
    }

    public object ReadOutput(int since)
    {
        lock (_historyLock)
        {
            int start = Math.Max(since - _trimmed, 0);
            var chunks = start < _history.Count
                ? _history.GetRange(start, _history.Count - start)
                : new List<string>();
            return new
            {
                chunks,
                next = _trimmed + _history.Count,
                running = IsRunning()
            };
        }
    }

    public void Start()
    {
        lock (_gameLock)
        {
            if (IsRunning()) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (var arg in new[]
            {
                "run", "--rm", "-i",
                "--name", _containerName,
                "--memory", "128m",              // RAM max
                "--cpus", "0.5",                 // CPU max
                "--pids-limit", "64",            // nombre max de processus
                "--network", "none",             // aucun accès réseau
                "--cap-drop", "ALL",             // aucune capacité Linux spéciale
                "--security-opt", "no-new-privileges",
                "-e", "DOTNET_NOLOGO=1",
                "-e", "DOTNET_CLI_TELEMETRY_OPTOUT=1",
                "-e", "TERM=dumb",
                _image
            })
                startInfo.ArgumentList.Add(arg);

            try
            {
                var p = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                p.Exited += (_, _) =>
                {
                    // Ignoré si la partie a été arrêtée/relancée volontairement.
                    if (ReferenceEquals(_game, p)) Append("\n[Le RPG s'est arrêté]\n");
                };
                p.Start();
                p.StandardInput.AutoFlush = true;
                _game = p;

                _ = ReadStreamAsync(p, p.StandardOutput, "");
                _ = ReadStreamAsync(p, p.StandardError, "\n[ERREUR] ");
            }
            catch (Exception ex)
            {
                Append($"\n[ERREUR] Impossible de lancer le conteneur : {ex.Message}\n");
            }
        }
    }

    private async Task ReadStreamAsync(Process p, StreamReader reader, string prefix)
    {
        var buffer = new char[1024];
        try
        {
            while (true)
            {
                int count = await reader.ReadAsync(buffer.AsMemory());
                if (count == 0) break;
                // Si la partie a été arrêtée ou relancée, on jette la sortie de l'ancien conteneur.
                if (!ReferenceEquals(_game, p)) break;
                Append(prefix + new string(buffer, 0, count));
            }
        }
        catch
        {
            // Flux fermé.
        }
    }

    public async Task<(bool Ok, string? Error)> SendAsync(string command)
    {
        var p = _game;
        if (p is null || !IsRunning())
            return (false, "Le RPG est arrêté.");

        try
        {
            await p.StandardInput.WriteLineAsync(command);
            await p.StandardInput.FlushAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public void Stop()
    {
        lock (_gameLock)
        {
            var p = _game;
            _game = null;
            if (p is null) return;

            // Tuer le client "docker run" ne suffit pas : on tue le conteneur lui-même.
            DockerCli.Run("kill", _containerName);

            try
            {
                if (!p.HasExited) p.Kill(entireProcessTree: true);
            }
            catch { }
        }
    }

    public void Restart()
    {
        Stop();
        lock (_historyLock)
        {
            _history.Clear();
            _trimmed = 0;
        }
        Start();
    }
}