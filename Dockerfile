# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY src/Listenfy.csproj ./src/
RUN dotnet restore ./src/Listenfy.csproj

# Copy everything else and build
COPY src/ ./src/
WORKDIR /app/src
RUN dotnet publish Listenfy.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Railway uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:$PORT

# Expose the port (Railway will override this with $PORT)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Listenfy.dll"]
