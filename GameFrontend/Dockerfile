FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY . .
RUN dotnet build GameFrontend -c Release
WORKDIR /src/GameFrontend
ENTRYPOINT ["dotnet", "run", "-c", "Release", "--no-build", "--no-launch-profile", "--urls", "http://0.0.0.0:3000"]
