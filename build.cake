#tool "nuget:?package=Microsoft.TestPlatform&version=17.12.0"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.18.3"

var Configuration = "Debug";
var SolutionPath = "./selenium-repro.sln";

Task("Build")
	.Does(() => {
		MSBuild(
			SolutionPath, 
			new MSBuildSettings {
					ToolVersion = MSBuildToolVersion.VS2022,
					Verbosity = Verbosity.Minimal
			}.WithTarget("Restore").WithTarget("Build"));
	});

Task("Test-VSTest")
	.IsDependentOn("Build")
	.ContinueOnError()
	.Does(() => {
		VSTest(
			"./selenium-system-text-json-repro-ms/bin/Debug/net48/selenium-system-text-json-repro-ms.dll", 
			new VSTestSettings {
				InIsolation = false
			});
	});

Task("Test-NUnit")
	.IsDependentOn("Build")
	.ContinueOnError()
	.Does(() => {
		NUnit3(
			"./selenium-system-text-json-repro-nunit/bin/Debug/net48/selenium-system-text-json-repro-nunit.dll",
			new NUnit3Settings {
				// make this consistent with Visual Studio and use the output path as the working directory
				WorkingDirectory = "./selenium-system-text-json-repro-nunit/bin/Debug/net48"
			});
	});

Task("Run-All")
	.IsDependentOn("Test-VSTest")
	.IsDependentOn("Test-NUnit");

RunTarget("Run-All");