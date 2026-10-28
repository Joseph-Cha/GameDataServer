# GameDataServer

폴리텍 서버·바이브 코딩 3일차 실습 서버입니다. ASP.NET(.NET 10) 최소 API로 몬스터 데이터와 점수 랭킹을 제공합니다.

- 배포 주소: https://gamedataserver-joseph-cha.onrender.com
- 로컬 실행: `dotnet run` (기본 5000번, macOS는 `PORT=5050 dotnet run`)
- Postman: `GameDataServer.postman_collection.json`을 Import하고 `baseUrl` 변수를 바꿔서 씁니다

| 메서드 | 경로 | 설명 |
|---|---|---|
| GET | `/hello` | 살아 있는지 확인 |
| GET | `/monsters` | 몬스터 전체 목록 |
| GET | `/monsters/{id}` | 몬스터 하나. 없으면 404 |
| GET | `/monsters/slow?ms=3000` | 일부러 느린 응답 |
| GET | `/monsters/big?count=20000` | 일부러 큰 응답 |
| POST | `/scores` | 점수 등록 `{ "player": "이름", "score": 100 }` |
| GET | `/scores?top=10` | 점수 높은 순 랭킹 |

`git push`를 하면 Render가 `Dockerfile`로 다시 빌드해 자동 배포합니다. 무료 서비스는 15분 동안 요청이 없으면 잠들고, 첫 요청에 1분쯤 걸려 깨어납니다.
