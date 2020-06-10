package main

import (
	"build-tools/dotnet"
)

func main() {
	dotnet.Build("Uptick.Platform.PubSub.Sdk.Tests", "./obj/Docker/publish")
	dotnet.Build("Uptick.Platform.PubSub.Sdk.Extenstions.Tests", "./obj/Docker/publish")
	dotnet.Build("Uptick.Platform.PubSub.Sdk.RabbitMQ.Tests", "./obj/Docker/publish")
	dotnet.Build("Uptick.Platform.PubSub.Sdk.Management.RabbitMQ.Tests", "./obj/Docker/publish")
	dotnet.Build("Uptick.Platform.PubSub.Sdk.ComponentTests", "./obj/Docker/publish")
	dotnet.RunUnitTestsWithReport()
	dotnet.RunIntergrationTests("PubSub", "uptick.platform.pubsub.sdk.componenttests", "docker-compose.yml", "docker-compose.tests.yml")
}
