FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["PersonalExpenseTracker.csproj", "."]
RUN dotnet restore "PersonalExpenseTracker.csproj"
COPY . .
RUN dotnet publish "PersonalExpenseTracker.csproj" -c Release -o /out --no-restore /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/App_Data/keys
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "PersonalExpenseTracker.dll"]
