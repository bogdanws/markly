FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY markly.csproj ./
RUN dotnet restore "./markly.csproj"
COPY . ./
RUN dotnet publish "./markly.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "markly.dll"]
