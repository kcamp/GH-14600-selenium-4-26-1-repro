## Prerequisites

Obtain Chrome For Testing to match ChromeDriver reference.
https://storage.googleapis.com/chrome-for-testing-public/131.0.6778.85/win64/chrome-win64.zip

You can invoke `.\Install-Chrome-For-Testing.ps1` to download this for you.

This methodology is provided for convenience.  You can alternately modify the `Directory.Packages.props` to target the correct-for-your-machine
version of ChromeDriver.  However - if you do this, you will need to amend the way we're resolving the `chrome.exe` executable.

```csharp
private static string GetChromeExePath()
{
	// assumes we're in selenium-system-text-json-repro/bin/debug/net48
	var chromeExe = Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\ChromeForTesting\chrome-win64\chrome.exe");
	if (!File.Exists(chromeExe))
	{
		throw new InvalidOperationException($"Could not locate the chrome.exe at '{chromeExe}'");
	}

	return chromeExe;
}
```


## Reproducing the error

I have configured a MS/VSTest project as well as a NUnit project.  We can reproduce the general class of failures indicated in the GH#14600 issue
with these projects.  Reference: https://github.com/SeleniumHQ/selenium/issues/14600

It's practical to enable fusion logging to capture the most data.  https://learn.microsoft.com/en-us/dotnet/framework/tools/fuslogvw-exe-assembly-binding-log-viewer

### Visual Studio 2022 - MSTest.TestFramework
In Test Explorer, execute the sole test in the `-ms` project.  You should see the error relating to `System.Runtime.CompilerServices.Unsafe`
```
Initialization method selenium_system_text_json_repro.SeleniumSystemTextJsonRepro.Setup threw exception. System.IO.FileLoadException: Could not load file or assembly 'System.Runtime.CompilerServices.Unsafe, Version=4.0.4.1, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040).
```

If you are capturing fusion logs, you will see failures to bind both `System.Runtime.CompilerServices.Unsafe` and `System.Text.Json`

You can un-comment the assembly binding redirect(s) in app.config to correct the error.
With the redirect for `System.Runtime.CompilerServices.Unsafe` in place, the test should work.  I observed fusion logs continue to be emitted for `System.Text.Json`, but not
to the detriment of the test.

### Visual Studio 2022 - NUnit
Again, in Test explorer, execute the sole test in the `-nunit` project.  You should see the error relating to `System.Text.Json`.

```
OneTimeSetUp: System.TypeInitializationException : The type initializer for 'OpenQA.Selenium.SeleniumManager' threw an exception.
  ----> System.IO.FileLoadException : Could not load file or assembly 'System.Text.Json, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040)
```

You can un-comment the assembly binding redirect(s) in app.config to correct the error.

### Cake Build, both VSTest.Console and NUnit.ConsoleRunner

Using the commented (non-redirected) app.config files, you can run the Cake build to reproduce failures in both assemblies.
In this case, though, the failure from the `-vs` project will NOT be the `System.Runtime.CompilerServices.Unsafe` error, but will more closely mirror the error from NUnit that
we saw in Visual Studio Test Explorer.

```
PS > .\build.ps1 [-Verbosity Diagnostic]
```

We can see the failures:
```
========================================
Test-VSTest
========================================
Executing task: Test-VSTest
Executing: "C:/Users/kcamp/source/repos/selenium-repro/tools/Microsoft.TestPlatform.17.12.0/tools/net462/Common7/IDE/Extensions/TestPlatform/vstest.console.exe" "C:/Users/kcamp/source/repos/selenium-repro/selenium-system-text-json-repro-ms/bin/Debug/net48/selenium-system-text-json-repro-ms.dll"
VSTest version 17.12.0 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
  Failed BasicChromeTest [317 ms]
  Error Message:
   Initialization method selenium_system_text_json_repro_ms.SeleniumSystemTextJsonRepro.Setup threw exception. System.IO.FileLoadException: Could not load file or assembly 'System.Text.Json, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040).
TestCleanup method selenium_system_text_json_repro_ms.SeleniumSystemTextJsonRepro.TearDown threw exception. System.NullReferenceException: Object reference not set to an instance of an object..
  Stack Trace:
      at OpenQA.Selenium.SeleniumManager..cctor()

TestCleanup Stack Trace
   at selenium_system_text_json_repro_ms.SeleniumSystemTextJsonRepro.TearDown() in C:\Users\kcamp\source\repos\selenium-repro\selenium-system-text-json-repro-ms\SeleniumSystemTextJsonRepro.cs:line 55


Test Run Failed.
Total tests: 1
     Failed: 1
 Total time: 1.7491 Seconds
An error occurred when executing task 'Test-VSTest'.

========================================
Test-NUnit
========================================
Executing task: Test-NUnit
Executing: "C:/Users/kcamp/source/repos/selenium-repro/tools/NUnit.ConsoleRunner.3.18.3/tools/nunit3-console.exe" "C:/Users/kcamp/source/repos/selenium-repro/selenium-system-text-json-repro-nunit/bin/Debug/net48/selenium-system-text-json-repro-nunit.dll"
NUnit Console Runner 3.18.3 (Release)
Copyright (c) 2022 Charlie Poole, Rob Prouse
Friday, November 22, 2024 10:32:24 AM

Runtime Environment
   OS Version: Microsoft Windows NT 6.2.9200.0
   Runtime: .NET Framework CLR v4.0.30319.42000

Test Files
    C:/Users/kcamp/source/repos/selenium-repro/selenium-system-text-json-repro-nunit/bin/Debug/net48/selenium-system-text-json-repro-nunit.dll


Errors, Failures and Warnings

1) TearDown Error : selenium_system_text_json_repro_nunit.SeleniumSystemTextJsonRepro
System.TypeInitializationException : The type initializer for 'OpenQA.Selenium.SeleniumManager' threw an exception.
  ----> System.IO.FileLoadException : Could not load file or assembly 'System.Text.Json, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040)
TearDown : System.NullReferenceException : Object reference not set to an instance of an object.
   at OpenQA.Selenium.SeleniumManager.BinaryPaths(String arguments)
   at OpenQA.Selenium.DriverFinder.GetDriverPath()
   at OpenQA.Selenium.Chromium.ChromiumDriver.GenerateDriverServiceCommandExecutor(DriverService service, DriverOptions options, TimeSpan commandTimeout)
   at OpenQA.Selenium.Chromium.ChromiumDriver..ctor(ChromiumDriverService service, ChromiumOptions options, TimeSpan commandTimeout)
   at OpenQA.Selenium.Chrome.ChromeDriver..ctor(ChromeDriverService service, ChromeOptions options, TimeSpan commandTimeout)
   at selenium_system_text_json_repro_nunit.SeleniumSystemTextJsonRepro.Setup() in C:\Users\kcamp\source\repos\selenium-repro\selenium-system-text-json-repro-nunit\SeleniumSystemTextJsonRepro.cs:line 49
--FileLoadException
   at OpenQA.Selenium.SeleniumManager..cctor()
--TearDown
   at selenium_system_text_json_repro_nunit.SeleniumSystemTextJsonRepro.TearDown() in C:\Users\kcamp\source\repos\selenium-repro\selenium-system-text-json-repro-nunit\SeleniumSystemTextJsonRepro.cs:line 55

2) Error : selenium_system_text_json_repro_nunit.SeleniumSystemTextJsonRepro.BasicChromeTest
OneTimeSetUp: System.TypeInitializationException : The type initializer for 'OpenQA.Selenium.SeleniumManager' threw an exception.
  ----> System.IO.FileLoadException : Could not load file or assembly 'System.Text.Json, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040)
   at OpenQA.Selenium.SeleniumManager.BinaryPaths(String arguments)
   at OpenQA.Selenium.DriverFinder.BinaryPaths()
   at OpenQA.Selenium.DriverFinder.GetDriverPath()
   at OpenQA.Selenium.Chromium.ChromiumDriver.GenerateDriverServiceCommandExecutor(DriverService service, DriverOptions options, TimeSpan commandTimeout)
   at OpenQA.Selenium.Chromium.ChromiumDriver..ctor(ChromiumDriverService service, ChromiumOptions options, TimeSpan commandTimeout)
   at OpenQA.Selenium.Chrome.ChromeDriver..ctor(ChromeDriverService service, ChromeOptions options, TimeSpan commandTimeout)
   at selenium_system_text_json_repro_nunit.SeleniumSystemTextJsonRepro.Setup() in C:\Users\kcamp\source\repos\selenium-repro\selenium-system-text-json-repro-nunit\SeleniumSystemTextJsonRepro.cs:line 49
--FileLoadException
   at OpenQA.Selenium.SeleniumManager..cctor()

Run Settings
    DisposeRunners: True
    WorkDirectory: C:\Users\kcamp\source\repos\selenium-repro\selenium-system-text-json-repro-nunit\bin\Debug\net48
    ImageRuntimeVersion: 4.0.30319
    ImageTargetFrameworkName: .NETFramework,Version=v4.8
    ImageRequiresX86: False
    ImageRequiresDefaultAppDomainAssemblyResolver: False
    TargetRuntimeFramework: net-4.8
    NumberOfTestWorkers: 8

Test Run Summary
  Overall result: Failed
  Test Count: 1, Passed: 0, Failed: 1, Warnings: 0, Inconclusive: 0, Skipped: 0
    Failed Tests - Failures: 0, Errors: 1, Invalid: 0
  Start time: 2024-11-22 15:32:24Z
    End time: 2024-11-22 15:32:26Z
    Duration: 1.485 seconds

Results (nunit3) saved as TestResult.xml
An error occurred when executing task 'Test-NUnit'.
```

If we uncomment the assembly binding redirects, we see that we don't fully solve the `MSTest.TestFramework` flavor of tests as fully as we did while in Visual Studio.
The NUnit tests are fully addressed, but we still have a `System.Text.Json` error in VSTest.

```
========================================
Test-VSTest
========================================
Executing task: Test-VSTest
Executing: "C:/Users/kcamp/source/repos/selenium-repro/tools/Microsoft.TestPlatform.17.12.0/tools/net462/Common7/IDE/Extensions/TestPlatform/vstest.console.exe" "C:/Users/kcamp/source/repos/selenium-repro/selenium-system-text-json-repro-ms/bin/Debug/net48/selenium-system-text-json-repro-ms.dll"
VSTest version 17.12.0 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
  Failed BasicChromeTest [321 ms]
  Error Message:
   Initialization method selenium_system_text_json_repro_ms.SeleniumSystemTextJsonRepro.Setup threw exception. System.IO.FileLoadException: Could not load file or assembly 'System.Text.Json, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040).
TestCleanup method selenium_system_text_json_repro_ms.SeleniumSystemTextJsonRepro.TearDown threw exception. System.NullReferenceException: Object reference not set to an instance of an object..
  Stack Trace:
      at OpenQA.Selenium.SeleniumManager..cctor()

TestCleanup Stack Trace
   at selenium_system_text_json_repro_ms.SeleniumSystemTextJsonRepro.TearDown() in C:\Users\kcamp\source\repos\selenium-repro\selenium-system-text-json-repro-ms\SeleniumSystemTextJsonRepro.cs:line 55


Test Run Failed.
Total tests: 1
     Failed: 1
 Total time: 1.8654 Seconds
An error occurred when executing task 'Test-VSTest'.
```

We have to put a redirect in place for `System.Text.Json` in addition to the `System.Runtime.CompilerServices.Unsafe` assembly to make the test suite work for `vstest.console.exe`.