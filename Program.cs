using System.Text.Json;        // 씨앗 데이터(Data/monsters.json)를 읽을 때만 쓴다
using Microsoft.Data.Sqlite;   // ← 추가: SQLite 데이터베이스를 여는 도구

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ── 2회차. DB 파일 준비 ────────────────────────────────────────────────
// Data/game.db 파일 하나가 데이터베이스다. 없으면 처음 열 때 만들어진다
var dbPath = Path.Combine(app.Environment.ContentRootPath, "Data", "game.db");
var connectionString = $"Data Source={dbPath}";

// 테이블이 없으면 만든다. 있으면 그대로 둔다 (IF NOT EXISTS)
using (var conn = new SqliteConnection(connectionString))
{
    conn.Open();
    var create = conn.CreateCommand();
    create.CommandText = """
        CREATE TABLE IF NOT EXISTS monsters (
            id     INTEGER PRIMARY KEY AUTOINCREMENT,
            name   TEXT    NOT NULL,
            hp     INTEGER NOT NULL,
            attack INTEGER NOT NULL,
            speed  REAL    NOT NULL
        )
        """;
    create.ExecuteNonQuery();

    // ── 3회차. 점수 표 — 몬스터 표 옆에 하나 더. 씨앗은 없다. 처음엔 비어 있다 ──
    var createScores = conn.CreateCommand();
    createScores.CommandText = """
        CREATE TABLE IF NOT EXISTS scores (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            player     TEXT    NOT NULL,
            score      INTEGER NOT NULL,
            created_at TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP
        )
        """;
    createScores.ExecuteNonQuery();

    // 테이블이 비어 있을 때만 1회차의 monsters.json을 씨앗으로 넣는다. 그래서 재시작해도 중복되지 않는다
    var count = conn.CreateCommand();
    count.CommandText = "SELECT COUNT(*) FROM monsters";
    if ((long)count.ExecuteScalar()! == 0)
    {
        var seedPath = Path.Combine(app.Environment.ContentRootPath, "Data", "monsters.json");
        var seed = JsonSerializer.Deserialize<List<Monster>>(
            File.ReadAllText(seedPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        foreach (var m in seed)
        {
            var insert = conn.CreateCommand();
            insert.CommandText = "INSERT INTO monsters (name, hp, attack, speed) VALUES ($name, $hp, $attack, $speed)";
            insert.Parameters.AddWithValue("$name", m.Name);
            insert.Parameters.AddWithValue("$hp", m.Hp);
            insert.Parameters.AddWithValue("$attack", m.Attack);
            insert.Parameters.AddWithValue("$speed", m.Speed);
            insert.ExecuteNonQuery();
        }
    }
}

// SELECT 결과(행들)를 Monster 목록으로 바꾸는 헬퍼. GET 네 곳에서 같이 쓴다
static List<Monster> ReadMonsters(SqliteDataReader reader)
{
    var list = new List<Monster>();
    while (reader.Read())   // 한 줄(한 마리)씩
    {
        list.Add(new Monster
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Hp = reader.GetInt32(2),
            Attack = reader.GetInt32(3),
            Speed = reader.GetFloat(4)
        });
    }
    return list;
}

// 모든 몬스터를 DB에서 읽는다. /monsters, /monsters/slow, /monsters/big이 같이 쓴다
static List<Monster> LoadAllMonsters(string connectionString)
{
    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT id, name, hp, attack, speed FROM monsters ORDER BY id";
    using var reader = cmd.ExecuteReader();
    return ReadMonsters(reader);
}

// ── 1-1. 살아 있는지 확인 ──────────────────────────────────────────────
app.MapGet("/hello", () => "안녕하세요, 서버입니다.");

// ── 몬스터 조회 — 이제 파일이 아니라 DB에서 읽는다 ─────────────────────
app.MapGet("/monsters", () => new { monsters = LoadAllMonsters(connectionString) });

app.MapGet("/monsters/{id:int}", (int id) =>
{
    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT id, name, hp, attack, speed FROM monsters WHERE id = $id";
    cmd.Parameters.AddWithValue("$id", id);   // 값은 글자로 이어 붙이지 않고 파라미터로 넘긴다
    using var reader = cmd.ExecuteReader();
    var found = ReadMonsters(reader);
    return found.Count == 0
        ? Results.NotFound(new { error = $"id {id} 몬스터가 없습니다." })
        : Results.Ok(found[0]);
});

// ── 2회차. 몬스터 추가 — POST는 "받아라". 본문의 JSON이 Monster로 들어온다 ──
app.MapPost("/monsters", (Monster m) =>
{
    if (string.IsNullOrWhiteSpace(m.Name))
        return Results.BadRequest(new { error = "name은 비어 있으면 안 됩니다." });

    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = """
        INSERT INTO monsters (name, hp, attack, speed) VALUES ($name, $hp, $attack, $speed);
        SELECT last_insert_rowid();
        """;
    cmd.Parameters.AddWithValue("$name", m.Name);
    cmd.Parameters.AddWithValue("$hp", m.Hp);
    cmd.Parameters.AddWithValue("$attack", m.Attack);
    cmd.Parameters.AddWithValue("$speed", m.Speed);
    m.Id = (int)(long)cmd.ExecuteScalar()!;   // DB가 붙여 준 새 id

    return Results.Created($"/monsters/{m.Id}", m);   // 201 Created + 새 몬스터
});

// ── 2회차. 몬스터 고치기 — PUT은 "이걸로 바꿔라". 본문의 JSON으로 그 id의 줄을 통째로 바꾼다 ──
app.MapPut("/monsters/{id:int}", (int id, Monster m) =>
{
    if (string.IsNullOrWhiteSpace(m.Name))
        return Results.BadRequest(new { error = "name은 비어 있으면 안 됩니다." });

    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE monsters SET name = $name, hp = $hp, attack = $attack, speed = $speed WHERE id = $id";
    cmd.Parameters.AddWithValue("$name", m.Name);
    cmd.Parameters.AddWithValue("$hp", m.Hp);
    cmd.Parameters.AddWithValue("$attack", m.Attack);
    cmd.Parameters.AddWithValue("$speed", m.Speed);
    cmd.Parameters.AddWithValue("$id", id);
    int changed = cmd.ExecuteNonQuery();   // 바뀐 줄 수. 그 id가 없으면 0

    if (changed == 0)
        return Results.NotFound(new { error = $"id {id} 몬스터가 없습니다." });

    m.Id = id;                 // 본문의 id는 무시하고 주소의 id를 쓴다
    return Results.Ok(m);      // 200 OK + 바뀐 몬스터
});

// ── 2회차. 몬스터 삭제 — DELETE는 "지워라". 본문은 없고 주소의 id만 쓴다 ──
app.MapDelete("/monsters/{id:int}", (int id) =>
{
    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "DELETE FROM monsters WHERE id = $id";
    cmd.Parameters.AddWithValue("$id", id);
    int deleted = cmd.ExecuteNonQuery();   // 지운 줄 수

    return deleted == 0
        ? Results.NotFound(new { error = $"id {id} 몬스터가 없습니다." })
        : Results.NoContent();             // 204 No Content. 돌려줄 본문이 없다
});

// ── 끊김 체험용: 일부러 느린 서버 (기본 3초) ──────────────────────────
app.MapGet("/monsters/slow", async (int ms = 3000) =>
{
    await Task.Delay(ms);
    return new { monsters = LoadAllMonsters(connectionString) };
});

// ── 끊김 체험용: 일부러 큰 데이터 (기본 20,000마리) ──────────────────
app.MapGet("/monsters/big", (int count = 20000) =>
{
    var monsters = LoadAllMonsters(connectionString);
    var big = new List<Monster>(count);
    for (int i = 1; i <= count; i++)
    {
        var src = monsters[(i - 1) % monsters.Count];
        big.Add(new Monster { Id = i, Name = $"{src.Name} #{i}", Hp = src.Hp, Attack = src.Attack, Speed = src.Speed });
    }
    return new { monsters = big };
});

// ── 3회차. 점수 등록 — POST는 "받아라". 본문의 JSON이 ScoreEntry로 들어오고 scores 표에 INSERT ──
app.MapPost("/scores", (ScoreEntry entry) =>
{
    if (string.IsNullOrWhiteSpace(entry.Player) || entry.Score < 0)
        return Results.BadRequest(new { error = "player는 비어 있으면 안 되고 score는 0 이상이어야 합니다." });

    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = """
        INSERT INTO scores (player, score) VALUES ($player, $score);
        SELECT last_insert_rowid();
        """;
    cmd.Parameters.AddWithValue("$player", entry.Player.Trim());
    cmd.Parameters.AddWithValue("$score", entry.Score);
    entry.Id = (int)(long)cmd.ExecuteScalar()!;   // DB가 붙여 준 새 번호

    return Results.Created("/scores", entry);     // 201 Created + 등록된 점수
});

// ── 3회차. 랭킹 — GET은 "달라". 점수 높은 순으로 top개를 SELECT하고 순위를 붙인다 ──
app.MapGet("/scores", (int top = 10) =>
{
    top = Math.Clamp(top, 1, 100);   // 너무 크거나 작은 top은 1~100으로 맞춘다

    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT player, score FROM scores ORDER BY score DESC, id ASC LIMIT $top";
    cmd.Parameters.AddWithValue("$top", top);
    using var reader = cmd.ExecuteReader();

    var ranking = new List<RankEntry>();
    while (reader.Read())   // 점수 높은 순으로 한 줄씩. 순위는 읽은 순서대로 붙인다
    {
        ranking.Add(new RankEntry { Rank = ranking.Count + 1, Player = reader.GetString(0), Score = reader.GetInt32(1) });
    }
    return new { scores = ranking };
});

// ── 3회차. 실행 — PORT가 없으면(내 PC) 5000, 있으면(Render) 그 번호 ──────
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

class ScoreEntry           // POST /scores가 받는 모양. id는 서버가 붙인다
{
    public int Id { get; set; }
    public string Player { get; set; } = "";
    public int Score { get; set; }
}

class RankEntry            // GET /scores가 돌려주는 한 줄
{
    public int Rank { get; set; }
    public string Player { get; set; } = "";
    public int Score { get; set; }
}
