FROM registry.uptick.team/devops/images/uptick-dotnet-sdk:2.2.105
ARG source
WORKDIR /app
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "vstest", "Uptick.Platform.PubSub.Sdk.ComponentTests.dll"]
