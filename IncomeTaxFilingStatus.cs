using Beezlabs.RPAHive.Lib;
using Beezlabs.RPAHive.Lib.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Beezlabs.RPA.Bots
{

    public class IncomeTaxData {
        public string AssesmentYear { get; set; }
        public string FilingType { get; set; }
        public string ITR { get; set; }
        public string AcknowledgementNo{ get; set; }
        public string FiledBy { get; set; }
        public string FilingDate { get; set; }
        public string FilingSection { get; set; }

        public List<Status> IncomeTaxStatus = new List<Status>();
    }

    public class Status { 
        public string Key { get; set; }
        public string Value { get; set; }

        public Status(String key, String value)
        {
            this.Key = key;
            this.Value = value;
        }
    }


    public class IncomeTaxFilingStatus : RPABotTemplate
    {
        IWebDriver driver;
      
        protected override void BotLogic(BotExecutionModel botExecutionModel)
        {
            try {
                 driver = new ChromeDriver();

                String WebsiteUrl = botExecutionModel.proposedBotInputs["url"].value.ToString();
                LogMessage(this.GetType().FullName, $"Getting website url : {WebsiteUrl}");
                String IncomeTaxLoginUserName = botExecutionModel.proposedBotInputs["incometaxusername"].value.ToString();
                LogMessage(this.GetType().FullName, $"Getting IncomeTax Login UserName : { IncomeTaxLoginUserName}");
                String IncomeTaxLoginPassword = botExecutionModel.proposedBotInputs["incometaxpassword"].value.ToString();
                LogMessage(this.GetType().FullName, $"Getting IncomeTax Login Password");
                driver.Navigate().GoToUrl(WebsiteUrl);
                LogMessage(this.GetType().FullName, "Chrome Driver Started Successfully");
                driver.Manage().Window.Maximize();
                LogMessage(this.GetType().FullName, "Chrome window screen has been maximized");
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(300));
                wait.Until(loginbtn => loginbtn.FindElement(By.CssSelector("a.btn.login"))).Click();
                LogMessage(this.GetType().FullName, $"Clicked Login Button");

                var InputUserName = wait.Until(username => username.FindElement(By.CssSelector("input#panAdhaarUserId.input-upper.mat-input-element.mat-form-field-autofill-control.cdk-text-field-autofill-monitored.ng-pristine.ng-invalid.ng-touched")));
                InputUserName.Click();

                LogMessage(this.GetType().FullName,$"Input User Name is Clicked");
                InputUserName.SendKeys(IncomeTaxLoginUserName);
                LogMessage(this.GetType().FullName,$"Enter User Input Name : {InputUserName}");

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("document.querySelector('#panAdhaarUserId').value=arguments[0];",IncomeTaxLoginUserName);

                LogMessage(this.GetType().FullName, $"Sending Input in Login UserName input field");
                wait.Until(continuebtn => continuebtn.FindElement(By.CssSelector("button.large-button-primary.width.marTop16"))).Click();
                LogMessage(this.GetType().FullName, $"Clicked Continue Button");

                var ClickCheckBox = driver.FindElement(By.CssSelector("input.mat-checkbox-input.cdk-visually-hidden"));
                js.ExecuteScript("document.querySelector('input.mat-checkbox-input.cdk-visually-hidden').click();");
                LogMessage(this.GetType().FullName, $"Clicked Check Box");

                var LoginPassword = driver.FindElement(By.CssSelector("#loginPasswordField"));
                LoginPassword.SendKeys(IncomeTaxLoginPassword);

                LogMessage(this.GetType().FullName, $"Sending Password in password input field");
                var ContinueBtn = driver.FindElement(By.CssSelector("button.large-button-primary.width.marTop26"));
                ContinueBtn.Click();
                LogMessage(this.GetType().FullName, $"Click Continue Button");

                DualLoginDetected(wait,driver);

                List<IncomeTaxData> MakeData = new List<IncomeTaxData>();

                GetScrapeData(MakeData, driver);
                Logout(wait,driver);               
                driver.Quit();
                LogMessage(this.GetType().FullName,$"Driver Quit Successfully");
                LogMessage(this.GetType().FullName,$"Bot Execution Successfully");
                Success("Bot Execution Success");

                } catch (Exception e) {

                driver.Quit();
                LogMessage(this.GetType().FullName,$"Driver Quit Successfully");
                LogMessage(this.GetType().FullName,$"Bot Failed : {e.Message} , {e.StackTrace}");
                Failure($"Bot Failed : {e.Message},{e.StackTrace}");
            }
            

        }
        public void GetScrapeData(List<IncomeTaxData> MakeData,IWebDriver driver) {

            Actions action = new Actions(driver);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(200));
            
            List<IWebElement> Search_EFile = wait.Until(e_file => e_file.FindElements(By.CssSelector("a.dropdown-content.menuText.cursorLink.forMediumAndAboveDevices.mat-button.ng-star-inserted"))).ToList();
            
            foreach (IWebElement el in Search_EFile) {
                String Text = el.FindElement(By.TagName("span")).Text.Trim();
                LogMessage(this.GetType().FullName,$"Getting Text Value : {Text}");

                if (el.FindElement(By.TagName("span")).Text.Trim().Contains("e-File")) {
                    action.MoveToElement(el).Build().Perform();
                    LogMessage(this.GetType().FullName,"Mouse Over in e-file ");
                    break;
                }

            }

            List<IWebElement> Search_IncomeTaxReturns = wait.Until(el => el.FindElements(By.CssSelector("div.mat-menu-content>span>span>div>button"))).ToList();
            foreach (IWebElement el in Search_IncomeTaxReturns) {
                if (el.Text.Trim().Contains("Income Tax Returns")) {
                    el.Click();
                    LogMessage(this.GetType().FullName,$"Clicked Income Tax Returns");
                    break;
                }
            }


            List<IWebElement> Search_ViewFiledIncomeReturns = driver.FindElements(By.CssSelector("div.mat-menu-content>span>span>button")).ToList();
            foreach (IWebElement el in Search_ViewFiledIncomeReturns) {
                if (el.Text.Trim().Contains("View Filed Returns")) {
                    el.Click();
                    LogMessage(this.GetType().FullName,$"Clicked in view filed returns");
                    break;
                }
            }

            String GettingTotalPage = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[5]/div/div/div/div[2]/div/div[3]")).Text.Split(' ')[2]; 
            int TotalPage = Int32.Parse(GettingTotalPage);
            String[] TotalItemsHelper = driver.FindElement(By.CssSelector("div.color-code-087.body-2-text.text-align-left.line-height-15.pl-2.pagination_border_left")).Text.Split(' ');
            int TotalItems = Int32.Parse(TotalItemsHelper[ TotalItemsHelper.Length - 2 ]);

            int card = 1;
            for (int next = 1; next <= TotalPage; next++)
            {
                
                List<IWebElement> ListOfMatCard = wait.Until(cards=> cards.FindElements(By.CssSelector("mat-card.mat-card.contextBox.ng-star-inserted"))).ToList();
                
                for ( ; card <=TotalItems ; card++)
                {
                    IncomeTaxData data = new IncomeTaxData();
                    List<Status> StatusList = new List<Status>();

                    data.AssesmentYear = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div[" + card + "]/div/mat-card/mat-card-header/div/mat-card-title/div/mat-label")).Text;
                    data.FilingType = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div[" + card + "]/div/mat-card/div/div[1]/div[2]/mat-label")).Text;
                    data.ITR = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div[" + card + "]/div/mat-card/div/div[3]/div[1]/mat-label[2]")).Text;
                    data.AcknowledgementNo = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div[" + card + "]/div/mat-card/div/div[3]/div[2]/mat-label[2]")).Text;
                    data.FiledBy = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div[" + card + "]/div/mat-card/div/div[3]/div[3]/mat-label[2]")).Text;
                    data.FilingDate = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div[" + card + "]/div/mat-card/div/div[3]/div[4]/mat-label[2]")).Text;
                    data.FilingSection = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div[" + card + "]/div/mat-card/div/div[3]/div[5]/mat-label[2]")).Text;

                    //checking if view more button present 
                    try
                    {

                        IReadOnlyCollection<IWebElement> SearchViewMoreBtn = driver.FindElements(By.CssSelector("span.hyperLink"));
                        foreach (IWebElement el in SearchViewMoreBtn)
                        {
                            if (el.Text.Contains("View More"))
                            {
                                el.Click();
                                LogMessage(this.GetType().FullName, $"View More Button is Clicked");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        LogMessage(this.GetType().FullName, $"view more button is not present");
                    }

                    IReadOnlyCollection<IWebElement> ListOfStatus = driver.FindElements(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div[" + card + "]/div/mat-card/div/div[2]/mat-vertical-stepper/div"));
                    String Key = null, Value = null;

                    List<String> Keys = new List<string>();
                    List<String> Values = new List<string>();

                    IReadOnlyCollection<IWebElement> ListOfStatusKeys = driver.FindElements(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div["+card+"]/div/mat-card/div/div[2]/mat-vertical-stepper/div/mat-step-header/div[3]/div[1]"));
                    IReadOnlyCollection<IWebElement> ListOfStatusValues = driver.FindElements(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[4]/div["+card+"]/div/mat-card/div/div[2]/mat-vertical-stepper/div/mat-step-header/div[3]/div[2]"));

                    AssertEqual(ListOfStatusKeys.Count !=0  && ListOfStatusValues.Count!=0);

                    foreach (IWebElement val in ListOfStatusKeys) {
                        Keys.Add(val.Text);
                    }
                    foreach (IWebElement val in ListOfStatusValues) {
                        Values.Add( val.Text );
                    }

                    for (int i = 0; i < Keys.Count; i++) {
                        Key = Keys[i];
                        Value = Values[i];
                        StatusList.Add( new Status(Key,Value) );
                    }

                    data.IncomeTaxStatus = StatusList; 
                    MakeData.Add(data);
                    LogMessage(this.GetType().FullName,$"StatusList is Clear");

                    if (card % 3 == 0) {
                        break;
                    }

                }
                card++;
                
                //Click next Button 
                var nextbtn = driver.FindElement(By.XPath("//*[@id='maincontentid']/app-dashboard/app-itr-status/div[5]/div/div/div/div[2]/div/div[4]/img"));
                nextbtn.Click();
                LogMessage(this.GetType().FullName,$"Clicked Next button");
            }

        }

        public void Logout(WebDriverWait wait ,IWebDriver driver) {

            var profileBtn = wait.Until(Driver => Driver.FindElement(By.CssSelector("button.profileMenubtn.mat-icon-button")));
            profileBtn.Click();
            LogMessage(this.GetType().FullName,$"Clicked Profile Button");
            IReadOnlyCollection<IWebElement> SearchLogoutBtn = driver.FindElements(By.CssSelector("button.mat-menu-item.ng-star-inserted"));
            foreach (IWebElement btn in SearchLogoutBtn) {

                if (btn.Text.Contains("Log Out")) {
                    btn.Click();
                    LogMessage(this.GetType().FullName,$"Clicked Logout button");
                    break;
                }
            }
        }

        public void DualLoginDetected(WebDriverWait wait , IWebDriver driver) {
            try
            {
                var ForceLoginBtn  = driver.FindElement(By.CssSelector("button.defaultButton.primaryButton.primaryBtnMargin"));
                ForceLoginBtn.Click();
                LogMessage(this.GetType().FullName,$"Force Login button is clicked");

            } catch (Exception) { 
            }
        }

        public void AssertEqual(bool  isTrue)  {
            if (!isTrue) {
                throw new Exception("Getting Status Details Error");
            }
        }


    }
}