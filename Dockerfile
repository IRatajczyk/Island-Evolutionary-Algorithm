# .NET build environment
FROM --platform=amd64 mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /src
COPY ["IEA.csproj", "./"]
RUN dotnet restore "./IEA.csproj"

COPY . .

RUN dotnet build "IEA.csproj" -c Release -o /app/build

# Runtime environment
FROM --platform=amd64 mcr.microsoft.com/dotnet/runtime:6.0 AS runtime

WORKDIR /app
COPY --from=build /app/build .
COPY /Data /app/Data

ENTRYPOINT ["dotnet", "IEA.dll"]
