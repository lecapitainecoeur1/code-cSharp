FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY publish/ .
COPY lang/ ./lang/
COPY config.json .
ENTRYPOINT ["dotnet", "rpg.dll"]
