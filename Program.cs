using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ── 1-2. 파일에서 데이터 읽기 ───────────────────────────────────────────
var dataPath = Path.Combine(app.Environment.ContentRootPath, "Data", "monsters.json");
var json = File.ReadAllText(dataPath);
var monsters = JsonSerializer.Deserialize<List<Monster>>(
    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

// ── 1-1. 살아 있는지 확인 ──────────────────────────────────────────────
app.MapGet("/hello", () => "안녕하세요, 서버입니다.");

// ── 몬스터 조회 ────────────────────────────────────────────────────────
app.MapGet("/monsters", () => new { monsters });

app.MapGet("/monsters/{id:int}", (int id) =>
{
    var found = monsters.Find(m => m.Id == id);
    return found is null
        ? Results.NotFound(new { error = $"id {id} 몬스터가 없습니다." })
        : Results.Ok(found);
});

// ── 끊김 체험용: 일부러 느린 서버 ──────────────────────────────────────
app.MapGet("/monsters/slow", async (int ms = 3000) =>
{
    await Task.Delay(ms);
    return new { monsters };
});

// ── 끊김 체험용: 일부러 큰 데이터 ──────────────────────────────────────
app.MapGet("/monsters/big", (int count = 20000) =>
{
    var big = new List<Monster>(count);
    for (int i = 1; i <= count; i++)
    {
        var src = monsters[(i - 1) % monsters.Count];
        big.Add(new Monster { Id = i, Name = $"{src.Name} #{i}", Hp = src.Hp, Attack = src.Attack, Speed = src.Speed });
    }
    return new { monsters = big };
});

// ── 3회차. 점수 등록과 랭킹 ────────────────────────────────────────────
var scores = new List<ScoreEntry>();

app.MapPost("/scores", (ScoreEntry entry) =>
{
    if (string.IsNullOrWhiteSpace(entry.Player) || entry.Score < 0)
        return Results.BadRequest(new { error = "player는 비어 있으면 안 되고 score는 0 이상이어야 합니다." });
    scores.Add(entry);
    return Results.Created("/scores", entry);
});

app.MapGet("/scores", (int top = 10) =>
{
    var ranking = scores
        .OrderByDescending(s => s.Score)
        .Take(top)
        .Select((s, i) => new { rank = i + 1, player = s.Player, score = s.Score });
    return new { scores = ranking };
});

// ── 실행 ───────────────────────────────────────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");

// ── 데이터 모양 ────────────────────────────────────────────────────────
class Monster
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Hp { get; set; }
    public int Attack { get; set; }
    public float Speed { get; set; }
}

class ScoreEntry
{
    public string Player { get; set; } = "";
    public int Score { get; set; }
}
