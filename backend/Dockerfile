FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/HappyPaws.Domain/HappyPaws.Domain.csproj src/HappyPaws.Domain/
COPY src/HappyPaws.Application/HappyPaws.Application.csproj src/HappyPaws.Application/
COPY src/HappyPaws.Infrastructure/HappyPaws.Infrastructure.csproj src/HappyPaws.Infrastructure/
COPY src/HappyPaws.Api/HappyPaws.Api.csproj src/HappyPaws.Api/
RUN dotnet restore src/HappyPaws.Api/HappyPaws.Api.csproj

COPY src/ src/
RUN dotnet publish src/HappyPaws.Api/HappyPaws.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV TZ=Asia/Bangkok
COPY --from=build /app ./
USER $APP_UID
ENTRYPOINT ["dotnet", "HappyPaws.Api.dll"]
