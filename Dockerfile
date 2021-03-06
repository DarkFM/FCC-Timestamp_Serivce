# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

EXPOSE 80

ENV ASPNETCORE_URLS="http://+" ASPNETCORE_ENVIRONMENT="Production"

# copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app ./
COPY --from=build /source/*.html ./

CMD ASPNETCORE_URLS=http://*:$PORT dotnet TimestampMicroservice.dll
