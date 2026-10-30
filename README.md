# GameDataServer

폴리텍 서버·바이브 코딩 3일차 실습 서버입니다. ASP.NET(.NET 10) 최소 API로 몬스터 데이터와 점수 랭킹을 제공합니다. 3일차 워크북 "02. 프로젝트 작업"의 완성 코드(참고본)와 같습니다.

- 배포 주소: https://gamedataserver-joseph-cha.onrender.com
- 로컬 실행: `dotnet run` (기본 5000번, macOS는 `PORT=5050 dotnet run`)
- Postman: `GameDataServer.postman_collection.json`을 Import하고 `baseUrl` 변수를 바꿔서 씁니다

## 데이터베이스

데이터는 SQLite 파일 `Data/game.db` 하나에 들어 있습니다. 서버를 켤 때 `monsters`와 `scores` 표가 없으면 만들고, `monsters` 표가 비어 있으면 `Data/monsters.json`의 다섯 마리를 씨앗으로 넣습니다. 그래서 서버를 껐다 켜도 몬스터와 점수가 남고, 재시작해도 씨앗이 중복되지 않습니다. `game.db`는 `.gitignore`로 저장소에서 빼 두었습니다.

| 표 | 변수명 | 비고 |
|---|---|---|
| `monsters` | `id`, `name`, `hp`, `attack`, `speed` | `id`는 DB가 붙인다 |
| `scores` | `id`, `player`, `score`, `created_at` | `id`와 `created_at`은 DB가 채운다. 씨앗 없음 |

## 엔드포인트

| 메서드 | 경로 | 설명 |
|---|---|---|
| GET | `/hello` | 살아 있는지 확인 |
| GET | `/monsters` | 몬스터 전체 목록 (DB에서 읽음) |
| GET | `/monsters/{id}` | 몬스터 하나. 없으면 404 |
| POST | `/monsters` | 몬스터 추가 `{ "name": "독 좀비", "hp": 80, "attack": 25, "speed": 2.5 }`. 201과 새 id |
| PUT | `/monsters/{id}` | 몬스터 통째로 고치기. 없으면 404 |
| DELETE | `/monsters/{id}` | 몬스터 삭제. 204, 없으면 404 |
| GET | `/monsters/slow?ms=3000` | 일부러 느린 응답 |
| GET | `/monsters/big?count=20000` | 일부러 큰 응답 |
| POST | `/scores` | 점수 등록 `{ "player": "kim", "score": 1200 }`. 201과 `{ "id", "player", "score" }`. 빈 이름이나 음수 점수는 400 |
| GET | `/scores?top=10` | 점수 높은 순 랭킹 `{ "scores": [ { "rank", "player", "score" } ] }`. `top`은 1~100 |

## 배포

`git push`를 하면 Render가 `Dockerfile`로 다시 빌드해 자동 배포합니다. 무료 서비스는 15분 동안 요청이 없으면 잠들고, 첫 요청에 1분쯤 걸려 깨어납니다. `game.db`는 컨테이너 안에 있어 재배포하거나 잠들었다 깨어나면 점수는 비고 몬스터는 씨앗 다섯 마리로 돌아갑니다. 남기려면 바깥 DB(Render PostgreSQL 등)에 저장해야 합니다.
