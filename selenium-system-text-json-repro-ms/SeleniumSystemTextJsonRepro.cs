using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Chrome;

namespace selenium_system_text_json_repro_ms
{
	[TestClass]
	public class SeleniumSystemTextJsonRepro
	{
		private ChromeDriver? browser = null;

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

		[TestMethod]
		public void BasicChromeTest()
		{
			this.browser!.Navigate().GoToUrl("https://www.github.com");
		}

		[TestInitialize]
		public void Setup()
		{
			ChromeOptions headlessChromeOptions = new ChromeOptions
			{
				AcceptInsecureCertificates = true
			};
			
			headlessChromeOptions.AddArguments(
				"--headless", 
				"window-size=1920x1080", 
				"--start-maximized", 
				"chrome.switches", 
				"--disable-extensions", 
				"--disable-notifications", 
				"disable-infobars", 
				"--test-type");

			headlessChromeOptions.BinaryLocation = GetChromeExePath();
			
			this.browser = new ChromeDriver(ChromeDriverService.CreateDefaultService(), headlessChromeOptions, TimeSpan.FromSeconds(600));
		}

		[TestCleanup]
		public void TearDown()
		{
			this.browser!.Close();
			this.browser.Dispose();
		}
	}
}