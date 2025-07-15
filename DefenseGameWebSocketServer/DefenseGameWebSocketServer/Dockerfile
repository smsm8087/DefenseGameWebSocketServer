# --- Build Stage ---
FROM mcr.microsoft.comdotnetsdk7.0 AS build
WORKDIR app

# 모든 파일 복사
COPY . .

# 복원 및 빌드
RUN dotnet restore
RUN dotnet publish -c Release -o out

# --- Run Stage ---
FROM mcr.microsoft.comdotnetaspnet7.0
WORKDIR app
COPY --from=build appout .

# Railway는 기본 80 포트를 사용하므로 환경변수 지정
ENV ASPNETCORE_URLS=http+80

# 포트 노출
EXPOSE 80

# 시작 명령 실제 DLL 이름으로 바꿔야 함
ENTRYPOINT [dotnet, DefenseGameWebSocketServer.dll]
