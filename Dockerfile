FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY markly.csproj ./
RUN dotnet restore "./markly.csproj"
COPY . ./
RUN dotnet publish "./markly.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 9.*
COPY --from=build /app/publish /app/publish
COPY . /src
COPY docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh
ENTRYPOINT ["/app/docker-entrypoint.sh"]
