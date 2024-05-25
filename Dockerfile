FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY . .

RUN dotnet publish CastorDJ.csproj -c Release -o out

RUN COMMIT=$(git rev-parse HEAD) && \
    BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ") && \
    echo "COMMIT=$COMMIT" > /app/out/version.txt && \
    echo "BUILD_DATE=$BUILD_DATE" >> /app/out/version.txt

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/out .

RUN export $(cat /app/version.txt | xargs)

ENTRYPOINT ["dotnet", "CastorDJ.dll"]