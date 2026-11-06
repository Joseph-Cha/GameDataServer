내 PC

GameDataServer
    - xxx
    - xxx
    - Data
    - GameDataServer.csproj


Git Hub
GameDataServer
    - xxx
    - xxx
    - Data
    - GameDataServer.csproj\

Render

0. GameDataServer를 빌드
    - dotnet publish -c Release -o app

1. GameDataServer에 있는 폴더 및 파일들을 복사
GameDataServer
    - xxx
    - xxx
    - Data
    - GameDataServer.csproj\

2. Docker
    - GameDataServer 데이터를 하나의 이미지로 변경(빌드)
        - Dockerfile에 작성된 내용을 기준으로 함

    - GameDataServer 이미지 -> 컨테이너화
        - GameDataServer를 실제로 실행을 진행
            - 터미널 실행 -> dotnet GameDataServer.dll