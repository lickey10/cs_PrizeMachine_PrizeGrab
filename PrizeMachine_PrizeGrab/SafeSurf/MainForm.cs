using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using Microsoft.Win32;
using SCTVObjects;
using System.Threading;
using System.Linq;

namespace SCTV
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1301:AvoidDuplicateAccelerators")]
    public partial class MainForm : Form
    {
        private bool loggedIn = false;
        public static string[] blockedTerms;
        public static string[] foundBlockedTerms;
        public static string[] foundBlockedSites;
        public static string blockedTermsPath = "config\\BlockedTerms.txt";
        public static string foundBlockedTermsPath = "config\\FoundBlockedTerms.txt";
        public static string[] blockedSites;
        public static string blockedSitesPath = "config\\BlockedSites.txt";
        public static string foundBlockedSitesPath = "config\\foundBlockedSites.txt";
        public static string loginInfoPath = "config\\LoginInfo.txt";
        public bool adminLock = false;//locks down browser until unlocked by a parent
        public int loggedInTime = 0;
        public bool checkForms = true;
        public bool MonitorActivity = false; //determines whether safesurf monitors page contents, forms, sites, etc...
        int loginMaxTime = 20;//20 minutes
        TabCtlEx tabControlEx = new TabCtlEx();

        bool showVolumeControl = false;
        bool showAddressBar = true;

        private DateTime startTime;
        private string userName;
        string documentString = "";
        bool enterTheContest = false;
        int counterCashstravaganza = 0;
        int counterUnclaimedPrizes = 0;
        string[] videos = null;
        int currentVideoIndex = -1;
        ArrayList videosList = new ArrayList();
        string currentVideoNumberString = "";
        System.Windows.Forms.Timer prizeClickTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer secondsTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer goToURLTimer = new System.Windows.Forms.Timer();
        int secondsCounter = 0;
        ExtendedWebBrowser prizeGrabBrowser;
        //ExtendedWebBrowser swagBucksBrowser;
        string urlToGoTo = "";
        int numberOfPrizesEntered = 0;
        int numberOfCashtravaganzaEntered = 0;
        int numberOfUnclaimedEntered = 0;
        ResultDetails resultDetails = null;
        bool foundPrize = false;
        int numOfLooks = 0;
        int maxedOutPrizesCount = 0;
        int maxSCounter = 0;
        RefreshUtilities.RefreshUtilities refreshUtilities;
        bool foundMaxedOutPrize = false;
        bool foundEnd = false;
        bool enteredPrizeGroup = false;
        Button[] sweepstakesButtons;
        TextBox[] sweepstakesTxtBoxes;
        TextBox[] sweepstakesTxtCountBoxes;
        int currentSweepstakesButtonIndex = -1;
        int lastSCounter = -1;
        bool clickingButton = false;
        List<string> users = new List<string>();
        bool switchingUsers = false;
        string currentUser = "";
        bool loggingIn = false;
        bool allDone = false;
        bool foundError = false;

        public bool LoggedIn
        {
            set
            {
                loggedIn = value;
                
                if (loggedIn)
                {
                    UpdateLoginToolStripMenuItem.Visible = true;
                    parentalControlsToolStripMenuItem.Visible = true;
                    loginToolStripMenuItem.Visible = false;
                    logoutToolStripMenuItem.Visible = true;
                    logoutToolStripButton.Visible = true;
                    LoginToolStripButton.Visible = false;
                    adminToolStripButton.Visible = true;

                    loginTimer.Enabled = true;
                    loginTimer.Start();
                }
                else
                {
                    UpdateLoginToolStripMenuItem.Visible = false;
                    parentalControlsToolStripMenuItem.Visible = false;
                    loginToolStripMenuItem.Visible = true;
                    logoutToolStripMenuItem.Visible = false;
                    logoutToolStripButton.Visible = false;
                    LoginToolStripButton.Visible = true;
                    adminToolStripButton.Visible = false;
                    tcAdmin.Visible = false;

                    loginTimer.Enabled = false;
                    loginTimer.Stop();
                }
            }

            get
            {
                return loggedIn;
            }
        }

        public Uri URL
        {
            set { _windowManager.ActiveBrowser.Url = value; }
        }

        public bool ShowMenuStrip
        {
            set { this.menuStrip.Visible = value; }
        }

        public FormBorderStyle FormBorder
        {
            set { this.FormBorderStyle = value; }
        }

        public bool ShowLoginButton
        {
            set { LoginToolStripButton.Visible = value; }
        }

        public bool ShowJustinRecordButton
        {
            set { JustinRecordtoolStripButton.Visible = value; }
        }

        public bool ShowVolumeControl
        {
            set 
            {
                showVolumeControl = value;
                //volumeControl.Visible = value; 
            }

            get { return showVolumeControl; }
        }

        public bool ShowAddressBar
        {
            set { showAddressBar = value; }

            get { return showAddressBar; }
        }

        public MainForm()
        {
            InitializeComponent();

            try
            {
                useLatestIE();

                Random rnd = new Random();
                int seconds = rnd.Next(50, 80) * 1000;

                //a little over a minute
                //prizeClickTimer.Enabled = true;
                //prizeClickTimer.Interval = seconds;
                //prizeClickTimer.Tick += PrizeClickTimer_Tick;
                //prizeClickTimer.Stop();

                //secondsTimer.Enabled = true;
                //secondsTimer.Interval = 1000;
                //secondsTimer.Tick += SecondsTimer_Tick;
                //secondsTimer.Stop();
                //lblRefreshTimer.Text = "0 seconds";

                //goToURLTimer.Enabled = true;
                //goToURLTimer.Tick += GoToURLTimer_Tick;
                //goToURLTimer.Stop();

                tabControlEx.Name = "tabControlEx";
                tabControlEx.SelectedIndex = 0;
                tabControlEx.Visible = false;
                tabControlEx.OnClose += new TabCtlEx.OnHeaderCloseDelegate(tabEx_OnClose);
                tabControlEx.VisibleChanged += new System.EventHandler(this.tabControlEx_VisibleChanged);

                this.panel1.Controls.Add(tabControlEx);
                tabControlEx.Dock = DockStyle.Fill;

                _windowManager = new WindowManager(tabControlEx);
                _windowManager.CommandStateChanged += new EventHandler<CommandStateEventArgs>(_windowManager_CommandStateChanged);
                _windowManager.StatusTextChanged += new EventHandler<TextChangedEventArgs>(_windowManager_StatusTextChanged);
                _windowManager.DocumentCompleted += _windowManager_DocumentCompleted;
                //_windowManager.ActiveBrowser.Navigating += ActiveBrowser_Navigating;
                _windowManager.ActiveBrowser.ScriptErrorsSuppressed = true;
                _windowManager.ShowAddressBar = showAddressBar;

                showAddressBarToolStripMenuItem.Checked = showAddressBar;

                startTime = DateTime.Now;
                userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                initFormsConfigs();

                


                ////load blocked terms
                //loadBlockedTerms(blockedTermsPath);

                ////load blocked sites
                //loadBlockedSites(blockedSitesPath);

                ////load found blocked terms
                //loadFoundBlockedTerms(foundBlockedTermsPath);

                ////load found blocked sites
                //loadFoundBlockedSites(foundBlockedSitesPath);


                //getDefaultBrowser();

            }
            catch (Exception ex)
            {
                Tools.WriteToFile(ex);
            }
        }

        //private void GoToURLTimer_Tick(object sender, EventArgs e)
        //{
        //    goToURLTimer.Stop();

        //    if (goToURLTimer.Tag != null && goToURLTimer.Tag.ToString().Trim().Length > 0)
        //    {
        //        if(goToURLTimer.Tag.ToString().Trim().Length > 5)
        //            prizeGrabBrowser.Url = new Uri(goToURLTimer.Tag.ToString());

        //        goToURLTimer.Tag = "";
        //    }           
        //}

        //private void SecondsTimer_Tick(object sender, EventArgs e)
        //{
        //    secondsCounter++;

        //    lblRefreshTimer.Text = ((prizeClickTimer.Interval - (secondsCounter * 1000)) / 1000).ToString() + " seconds left";
        //}

        // Starting the app here...
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Open a new browser window

            //_windowManager.ActiveBrowser.u
            prizeGrabBrowser = _windowManager.New(false);
            //prizeGrabBrowser.Url = new Uri("https://prizegrab.com/prizes/popular/");
            prizeGrabBrowser.Url = new Uri("https://prizegrab.com/prizes");

            //swagBucksBrowser = this._windowManager.New();
            //swagBucksBrowser.Url = new Uri("http://www.swagbucks.com/watch/");

            refreshUtilities = new RefreshUtilities.RefreshUtilities();
            refreshUtilities.GoToUrlComplete += RefreshUtilities_GoToUrlComplete;
            refreshUtilities.ClickComplete += RefreshUtilities_ClickComplete;
            
            sweepstakesButtons = new Button[4];
            sweepstakesButtons[0] = btnSweepstakes1;
            sweepstakesButtons[1] = btnSweepstakes2;
            sweepstakesButtons[2] = btnSweepstakes3;
            sweepstakesButtons[3] = btnSweepstakes4;

            sweepstakesTxtBoxes = new TextBox[4];
            sweepstakesTxtBoxes[0] = txtSweepstakes1;
            sweepstakesTxtBoxes[1] = txtSweepstakes2;
            sweepstakesTxtBoxes[2] = txtSweepstakes3;
            sweepstakesTxtBoxes[3] = txtSweepstakes4;

            sweepstakesTxtCountBoxes = new TextBox[4];
            sweepstakesTxtCountBoxes[0] = txtSweepstakes1Count;
            sweepstakesTxtCountBoxes[1] = txtSweepstakes2Count;
            sweepstakesTxtCountBoxes[2] = txtSweepstakes3Count;
            sweepstakesTxtCountBoxes[3] = txtSweepstakes4Count;

            //set button text
            btnSweepstakes1.Text = Properties.Settings.Default.Sweepstakes1;
            btnSweepstakes2.Text = Properties.Settings.Default.Sweepstakes2;
            btnSweepstakes3.Text = Properties.Settings.Default.Sweepstakes3;
            btnSweepstakes4.Text = Properties.Settings.Default.Sweepstakes4;

            //set sweepstakes names
            txtSweepstakes1.Text = Properties.Settings.Default.Sweepstakes1;
            txtSweepstakes2.Text = Properties.Settings.Default.Sweepstakes2;
            txtSweepstakes3.Text = Properties.Settings.Default.Sweepstakes3;
            txtSweepstakes4.Text = Properties.Settings.Default.Sweepstakes4;

            users.Add("lickey10@gmail.com|soccer");
            users.Add("lickeykids@gmail.com|soccer");

            //start looking for sweepstakes
            clickNextButton();
        }

        private void RefreshUtilities_GoToUrlComplete(object sender, EventArgs e)
        {
            foundError = false;

            if (sender != null && sender is RefreshUtilities.TimerInfo && ((RefreshUtilities.TimerInfo)sender).Browser is ExtendedWebBrowser)
            {
                ExtendedWebBrowser tempBrowser = (ExtendedWebBrowser)((RefreshUtilities.TimerInfo)sender).Browser;

                if (tempBrowser.IsBusy)
                    tempBrowser.Stop();

                tempBrowser.Url = new Uri(((RefreshUtilities.TimerInfo)sender).UrlToGoTo);

                foundMaxedOutPrize = false;
                enteredPrizeGroup = false;
                foundEnd = false;
                clickingButton = false;
            }
        }

        private void RefreshUtilities_ClickComplete(object sender, EventArgs e)
        {
            //clickingButton = false;
        }

        private void PrizeClickTimer_Tick(object sender, EventArgs e)
        {
            Random rnd = new Random();
            int seconds = rnd.Next(50, 80) * 1000;

            prizeClickTimer.Stop();
            prizeClickTimer.Interval = seconds;
            secondsTimer.Stop();
            lblRefreshTimer.Text = "0 seconds";

            if (urlToGoTo.Trim().Length > 0)
            {
                if (urlToGoTo.ToString().ToLower().Contains("https://prizegrab.com/prize/"))
                    enterTheContest = true;

                prizeGrabBrowser.Url = new Uri(urlToGoTo);

                foundPrize = false;
            }
        }

        private void ActiveBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            documentString = "";
        }

        private void _windowManager_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            documentString = _windowManager.ActiveBrowser.DocumentText;

            lookForError();

            if (!foundError && prizeGrabBrowser.Url != null && !clickingButton)
            {
                if(prizeGrabBrowser.Url.ToString().ToLower().Contains("login"))
                {
                    prizeClickTimer.Stop();
                    secondsTimer.Stop();
                    lblRefreshTimer.Text = "0 seconds";
                }
                //prizegrab
                //else if (prizeGrabBrowser != null && prizeGrabBrowser.Url.ToString().ToLower().Contains("/cashstravaganza/"))//cashstravaganza
                //{
                //    enterCashstravaganza(documentString);
                //}
                else if (txtSweepstakes1.Text.Trim().Length > 0 && prizeGrabBrowser.Url.ToString().ToLower().Contains("/p/"+ txtSweepstakes1.Text +"/"))
                {
                    enterPrizeGroup(documentString);
                }
                else if (txtSweepstakes2.Text.Trim().Length > 0 && prizeGrabBrowser.Url.ToString().ToLower().Contains("/p/"+ txtSweepstakes2.Text +"/"))
                {
                    enterPrizeGroup(documentString);
                }
                else if (txtSweepstakes3.Text.Trim().Length > 0 && prizeGrabBrowser.Url.ToString().ToLower().Contains("/p/" + txtSweepstakes3.Text + "/"))
                {
                    enterPrizeGroup(documentString);
                }
                else if (txtSweepstakes4.Text.Trim().Length > 0 && prizeGrabBrowser.Url.ToString().ToLower().Contains("/p/" + txtSweepstakes4.Text + "/"))
                {
                    enterPrizeGroup(documentString);
                }
                else if (prizeGrabBrowser.Url.ToString().ToLower().Contains("https://prizegrab.com/prize"))//this is prizegrab prize list
                {
                    if (prizeGrabBrowser.Url.ToString().ToLower().Contains("/prize/"))//this is the prize page
                    {
                        if(enterTheContest)
                            enterContest(documentString);
                    }
                    else if (!enterTheContest && !prizeGrabBrowser.Url.ToString().ToLower().Contains("processing") && !foundPrize)//this is the prizes page
                        iteratePrizes(documentString);
                    else if (prizeGrabBrowser.Url.ToString().ToLower()== "https://prizegrab.com/prize")
                    {
                        enterTheContest = false;
                        foundPrize = false;
                        refreshUtilities.Cancel();
                        prizeGrabBrowser.Url = new Uri("https://prizegrab.com/prizes/popular/");
                    }
                    else if(!refreshUtilities.IsActive)
                    {
                        refreshUtilities.GoToURL("https://prizegrab.com/prizes/popular/", 25, lblRefreshTimer, prizeGrabBrowser);
                    }
                }
            }
        }

        private void lookForError()
        {
            if (!foundError && prizeGrabBrowser.DocumentText.Contains("<center><h1>504 Gateway Time-out</h1></center>") || prizeGrabBrowser.DocumentText.Contains("<center><h1>502 Bad Gateway</h1></center>"))
            {
                foundError = true;
                refreshUtilities.GoToURL(prizeGrabBrowser.Url.ToString(), 15, lblRefreshTimer, prizeGrabBrowser);
            }
        }
        
        private void iteratePrizes(string pageContent)
        {
            string splitString = "<!-- link box -->";
            bool foundNextPrize = false;

            string[] prizes = pageContent.Split(new string[] { splitString }, StringSplitOptions.RemoveEmptyEntries);

            if (prizes.Length > 1)
            {
                foreach (string prize in prizes)
                {
                    int entriesLeft = 0;
                    string entriesLeftString = "";
                    string tempEntriesLeftString = "";
                    urlToGoTo = "";

                    entriesLeftString = findValue(prize, "<p>", "</p>");
                    entriesLeftString = entriesLeftString.Replace(" entries left today", "");
                    entriesLeftString = entriesLeftString.Replace(" entry left today", "");
                    //check for 2x entries - 2X Entries (18 left)
                    //5X Entries (0 left)
                    //entriesLeftString = entriesLeftString.Replace("2X Entries (", "");
                    //entriesLeftString = entriesLeftString.Replace(" left)", "");

                    tempEntriesLeftString = findValue(entriesLeftString, "X Entries (", "left)");

                    if (tempEntriesLeftString.Trim().Length > 0)
                        entriesLeftString = tempEntriesLeftString;
                    else
                        entriesLeftString = findValue(entriesLeftString, ">", "<");

                    if (int.TryParse(entriesLeftString, out entriesLeft) && entriesLeft > 0)
                    {
                        urlToGoTo = "https://prizegrab.com" + findValue(prize, "<a href=\"", "\"");

                        documentString = "";

                        if (urlToGoTo.Contains("/prize/"))
                        {
                            refreshUtilities.GoToURL(urlToGoTo, 10, true, lblRefreshTimer, prizeGrabBrowser);

                            foundPrize = true;
                            foundNextPrize = true;
                            enterTheContest = true;
                            numOfLooks = 0;

                            break;
                        }
                    }
                }

                if (!foundNextPrize && urlToGoTo.Trim().Length > 0)//all the prizes are done
                {
                    numOfLooks++;
                    urlToGoTo = "";
                    enterTheContest = true;

                    //go to prize blizzard
                    numberOfUnclaimedEntered = 0;

                    string[] URLSegments = prizeGrabBrowser.Url.Segments;

                    //refreshUtilities.GoToURL(prizeGrabBrowser.Url.Scheme + "://" + prizeGrabBrowser.Url.Host + URLSegments[0] + URLSegments[1],lblRefreshTimer,prizeGrabBrowser);

                    if (numOfLooks > 2)
                    {
                        btnSweepstakes_Click(null, null);

                        numOfLooks = 0;
                    }

                    //if (prizeGrabBrowser.Url.ToString().ToLower().Contains("/p/prize-madness/"))
                    //    prizeGrabBrowser.Url = new Uri("http://prizegrab.com/p/prize-madness/");

                    if(!switchingUsers)
                        switchUsers();
                }
            }
        }

        private void enterContest(string pageContent)
        {
            string enterContestLink = "";
            enterContestLink = findValue(pageContent, "Official Rules</a>", "Click Here to Enter");
            enterContestLink = findValue(enterContestLink, "<a href=\"/", "\"");

            documentString = "";

            if (enterContestLink.Trim().Length > 0 && enterContestLink.ToLower().Contains("enternow"))
            {
                foundPrize = false;
                enterTheContest = false;
                enterContestLink = "https://prizegrab.com/" + enterContestLink;
                
                numberOfPrizesEntered++;
                txtPrizeCount.Text = numberOfPrizesEntered.ToString();

                refreshUtilities.GoToURL(enterContestLink, true, lblRefreshTimer, prizeGrabBrowser);
                
            }
            else
                prizeGrabBrowser.Refresh();
        }

        private void enterCashstravaganza(string pageContent)
        {
            string enterAndContinueString = "";
            int sCounter = 0;
            string sValue = "";

            enterAndContinueString = findValue(pageContent, "/p/cashstravaganza/processing/", "\"");

            if (enterAndContinueString.Trim().Length > 0)
            {
                sValue = enterAndContinueString.Substring(enterAndContinueString.IndexOf("s=") + 2);
                int.TryParse(sValue, out sCounter);

                if (sCounter > counterCashstravaganza || sValue == "end")
                {
                    counterCashstravaganza = sCounter;
                    
                    urlToGoTo = "https://prizegrab.com/p/cashstravaganza/processing/" + enterAndContinueString;

                    refreshUtilities.GoToURL(urlToGoTo, true, lblRefreshTimer, prizeGrabBrowser);

                    //prizeGrabBrowser.Url = new Uri("https://prizegrab.com/p/cashstravaganza/processing/" + enterAndContinueString);

                    if (sValue == "end")//reset because we can go again
                        counterCashstravaganza = 0;

                    numberOfCashtravaganzaEntered++;
                    //txtCashtravaganzaCount.Text = numberOfCashtravaganzaEntered.ToString();
                }
            }
            else if (counterCashstravaganza == 0 && prizeGrabBrowser.Url.ToString().Contains("s=end"))
            {
                prizeClickTimer.Stop();
                secondsTimer.Stop();
                lblRefreshTimer.Text = "0 seconds";
                urlToGoTo = "";

                refreshUtilities.GoToURL("https://prizegrab.com/p/cashstravaganza/", true, lblRefreshTimer, prizeGrabBrowser);
            }
            else if (counterCashstravaganza == 0 && enterAndContinueString.Trim().Length == 0)//we are done - move on to unclaimedPrizes
            {
                prizeClickTimer.Stop();
                secondsTimer.Stop();
                lblRefreshTimer.Text = "0 seconds";
                urlToGoTo = "";
            }
        }

        private void enterPrizeGroup(string pageContent)
        {
            if (!enteredPrizeGroup)
            {
                string enterAndContinueString = "";
                int sCounter = 0;
                string sValue = "";

                string[] URLSegments = prizeGrabBrowser.Url.Segments;

                enterAndContinueString = findValue(pageContent, URLSegments[0] + URLSegments[1] + URLSegments[2] + "processing/", "\"");

                if (enterAndContinueString.Trim().Length > 0)//found the button
                {
                    //sValue = enterAndContinueString.Substring(enterAndContinueString.IndexOf("s=") + 2);
                    if (prizeGrabBrowser.Url.ToString().ToLower().Contains("s="))
                    {
                        sValue = prizeGrabBrowser.Url.Query.Substring(prizeGrabBrowser.Url.Query.IndexOf("s=") + 2);

                        if (sValue.Contains("&"))
                            sValue = sValue.Substring(0, sValue.IndexOf("&"));// sValue.Replace("&r=None", "");

                        int.TryParse(sValue, out sCounter);
                    }
                    else
                        sCounter = 1;

                    lastSCounter = sCounter;

                    if (sCounter > maxSCounter)
                        maxSCounter = sCounter;

                    if (sCounter > counterUnclaimedPrizes || sValue == "end")
                    {
                        counterUnclaimedPrizes = sCounter;

                        //if (!refreshUtilities.IsActive)
                        //{
                            numberOfUnclaimedEntered++;

                            if (URLSegments[2].ToLower().Contains(txtSweepstakes1.Text.ToLower() + "/"))
                                txtSweepstakes1Count.Text = numberOfUnclaimedEntered.ToString();
                            else if (URLSegments[2].ToLower().Contains(txtSweepstakes2.Text.ToLower() + "/"))
                                txtSweepstakes2Count.Text = numberOfUnclaimedEntered.ToString();
                            else if (URLSegments[2].ToLower().Contains(txtSweepstakes3.Text.ToLower() + "/"))
                                txtSweepstakes3Count.Text = numberOfUnclaimedEntered.ToString();
                            else if (URLSegments[2].ToLower().Contains(txtSweepstakes4.Text.ToLower() + "/"))
                                txtSweepstakes4Count.Text = numberOfUnclaimedEntered.ToString();
                        //}

                        urlToGoTo = prizeGrabBrowser.Url.Scheme + "://" + prizeGrabBrowser.Url.Host + URLSegments[0] + URLSegments[1] + URLSegments[2] + "processing/" + enterAndContinueString;

                        refreshUtilities.GoToURL(urlToGoTo, true, lblRefreshTimer, prizeGrabBrowser);

                        foundEnd = false;
                        enteredPrizeGroup = true;

                        //if (sValue == "end")//reset because we can go again
                        //{
                        //    counterUnclaimedPrizes = 0;
                        //    maxedOutPrizesCount = 0;
                        //}
                    }
                    else
                    {
                        //this was hit when it wasn't needed
                        //clickNextButton();
                    }
                }
                //   /p/cash-elevator/
                else if (pageContent.ToLower().Contains("you've maxed your entries for this prize today"))//maxed out prize
                {
                    if (!foundMaxedOutPrize)
                    {
                        foundMaxedOutPrize = true;
                        foundEnd = false;
                        maxedOutPrizesCount++;
                        enteredPrizeGroup = true;

                        sCounter = 1;
                        string tempCounter = "";

                        if (prizeGrabBrowser.Url.ToString().ToLower().Contains("s="))
                        {
                            tempCounter = prizeGrabBrowser.Url.Query.Substring(prizeGrabBrowser.Url.Query.IndexOf("s=") + 2);
                            int.TryParse(tempCounter, out sCounter);
                        }

                        lastSCounter = sCounter;

                        if (sCounter > maxSCounter)
                            maxSCounter = sCounter;

                        if (maxedOutPrizesCount <= maxSCounter)//we have not maxed out all prizes
                        {
                            string nextSValue = findValue(pageContent.ToLower(), "<a href=\""+ URLSegments[0] + URLSegments[1] + URLSegments[2] + "?s=", "\"");

                            refreshUtilities.GoToURL(prizeGrabBrowser.Url.Scheme + "://" + prizeGrabBrowser.Url.Host + URLSegments[0] + URLSegments[1] + URLSegments[2] + "?s=" + nextSValue, true, lblRefreshTimer, prizeGrabBrowser);

                            //if (sCounter <= maxSCounter && tempCounter != "end" && nextSValue != "end")
                            //    refreshUtilities.GoToURL(prizeGrabBrowser.Url.Scheme + "://" + prizeGrabBrowser.Url.Host + URLSegments[0] + URLSegments[1] + URLSegments[2] + "?s=" + (sCounter + 1), true, lblRefreshTimer, prizeGrabBrowser);
                            //else
                            //    refreshUtilities.GoToURL(prizeGrabBrowser.Url.Scheme + "://" + prizeGrabBrowser.Url.Host + URLSegments[0] + URLSegments[1] + URLSegments[2] + "?s=end", true, lblRefreshTimer, prizeGrabBrowser);
                        }
                        else
                            clickNextButton();
                    }
                }
                

                //else if (!prizeGrabBrowser.Url.ToString().ToLower().Contains("processing"))
                //{
                //    numOfLooks++;

                //    //if (maxedOutPrizesCount < maxSCounter)//we have not maxed out all prizes
                //    //{
                //    //    refreshUtilities.GoToURL(prizeGrabBrowser.Url.Scheme + "://" + prizeGrabBrowser.Url.Host + URLSegments[0] + URLSegments[1] + URLSegments[2] + "?s=" + sCounter + 1, true, lblRefreshTimer, prizeGrabBrowser);


                //    //    //counterUnclaimedPrizes = 0;
                //    //    //maxedOutPrizesCount = 0;
                //    //}
                //    //else if (maxSCounter == 0 && numOfLooks == 1)





                //    //if (numOfLooks == 1)
                //    //{
                //    //    sCounter = 1;
                //    //    maxedOutPrizesCount++;
                //    //    enteredPrizeGroup = true;

                //    //    refreshUtilities.GoToURL(prizeGrabBrowser.Url.Scheme + "://" + prizeGrabBrowser.Url.Host + URLSegments[0] + URLSegments[1] + URLSegments[2] + "?s=2", true, lblRefreshTimer, prizeGrabBrowser);
                //    //}
                //    //else if (numOfLooks > 5)
                //    //{
                //    //    if (URLSegments[2].ToLower().Contains(lblSweepstakes1.Text.ToLower() + "/") && counterUnclaimedPrizes == 0 && txtSweepstakes1Count.Text.Trim().Length > 0)
                //    //        btnCashVault_Click(null, null);
                //    //    else if (counterUnclaimedPrizes == 0 && txtSweepstakes1Count.Text.Trim().Length > 0)
                //    //        btnPrizesStart_Click(null, null);
                //    //    else if (counterUnclaimedPrizes == 0 && txtSweepstakes2Count.Text.Trim().Length > 0)
                //    //        btnSweepstakes3_Click(null, null);

                //    //    numOfLooks = 0;
                //    //    maxSCounter = 0;
                //    //    maxedOutPrizesCount = 0;
                //    //    enteredPrizeGroup = true;
                //    //}
                //}
                else if (!foundEnd && prizeGrabBrowser.Url.ToString().ToLower().Contains("s=end") && !prizeGrabBrowser.Url.ToString().ToLower().Contains("processing"))
                {
                    if (maxedOutPrizesCount < maxSCounter && maxedOutPrizesCount != lastSCounter)//we haven't maxed out all the prizes
                    {
                        refreshUtilities.GoToURL(prizeGrabBrowser.Url.Scheme + "://" + prizeGrabBrowser.Url.Host + URLSegments[0] + URLSegments[1] + URLSegments[2], true, lblRefreshTimer, prizeGrabBrowser);

                        foundEnd = true;
                        counterUnclaimedPrizes = 0;
                        maxedOutPrizesCount = 0;
                        enteredPrizeGroup = true;
                    }
                    else if(maxedOutPrizesCount == maxSCounter && maxSCounter > 0)//all the prizes are maxed out - go to next contest
                    {
                       clickNextButton();
                    }
                }
                else if(prizeGrabBrowser.Url.ToString().ToLower().Contains("processing/?") && !refreshUtilities.IsActive)
                {
                    //make sure page keeps running - sometimes the processing page hanges and doesn't load
                    refreshUtilities.GoToURL(prizeGrabBrowser.Url.ToString(), 25, lblRefreshTimer, prizeGrabBrowser);
                }
            }
        }

        private void clickNextButton()
        {
            if (!clickingButton)
            {
                bool foundButton = false;

                if (currentSweepstakesButtonIndex + 1 < sweepstakesButtons.Length)
                {
                    if (currentSweepstakesButtonIndex > -1)
                    {
                        counterUnclaimedPrizes = 0;
                        maxedOutPrizesCount = 0;
                        foundEnd = true;
                        enteredPrizeGroup = true;
                        foundButton = true;

                        if (sweepstakesButtons[currentSweepstakesButtonIndex + 1].Text == "")
                        {
                            currentSweepstakesButtonIndex++;
                            clickNextButton();
                        }
                        else
                        {
                            clickingButton = true;

                            sweepstakesButtons[currentSweepstakesButtonIndex + 1].PerformClick();
                        }
                    }
                    else
                    {
                        for (int x = 0; x < sweepstakesButtons.Length; x++)
                        {
                            if (currentSweepstakesButtonIndex == -1 || (prizeGrabBrowser.Url.Segments[2].ToLower().Contains(sweepstakesButtons[x].Text.ToLower()) && x >= currentSweepstakesButtonIndex))
                            {
                                counterUnclaimedPrizes = 0;
                                maxedOutPrizesCount = 0;
                                foundEnd = true;
                                enteredPrizeGroup = true;
                                foundButton = true;
                                clickingButton = true;

                                if (x + 1 < sweepstakesButtons.Length)
                                {
                                    if (currentSweepstakesButtonIndex != -1)
                                        sweepstakesButtons[x + 1].PerformClick();
                                    else
                                        sweepstakesButtons[x].PerformClick();
                                }
                                else
                                    btnPrizesStart.PerformClick();

                                break;
                            }
                        }
                    }
                }

                if (!foundButton)//we are done with the last sweepstakes so go to prizes next
                {
                    clickingButton = true;

                    btnPrizesStart.PerformClick();
                }
            }
        }

        private void initFormsConfigs()
        {
            SettingsHelper helper = SettingsHelper.Current;

            checkForms = helper.CheckForms;
        }

        private void useLatestIE()
        {
            try
            {
                string AppName = Application.ProductName;// My.Application.Info.AssemblyName
                int VersionCode = 0;
                string Version = "";
                object ieVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer").GetValue("svcUpdateVersion");

                if (ieVersion == null)
                    ieVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer").GetValue("Version");

                if (ieVersion != null)
                {
                    Version = ieVersion.ToString().Substring(0, ieVersion.ToString().IndexOf("."));
                    switch (Version)
                    {
                        case "7":
                            VersionCode = 7000;
                            break;
                        case "8":
                            VersionCode = 8888;
                            break;
                        case "9":
                            VersionCode = 9999;
                            break;
                        case "10":
                            VersionCode = 10001;
                            break;
                        default:
                            if (int.Parse(Version) >= 11)
                                VersionCode = 11001;
                            else
                                Tools.WriteToFile(Tools.errorFile, "useLatestIE error: IE Version not supported");
                            break;
                    }
                }
                else
                {
                    Tools.WriteToFile(Tools.errorFile, "useLatestIE error: Registry error");
                }

                //'Check if the right emulation is set
                //'if not, Set Emulation to highest level possible on the user machine
                string Root = "HKEY_CURRENT_USER\\";
                string Key = "Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION";
                
                object CurrentSetting = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Key).GetValue(AppName + ".exe");

                if (CurrentSetting == null || int.Parse(CurrentSetting.ToString()) != VersionCode)
                {
                    Microsoft.Win32.Registry.SetValue(Root + Key, AppName + ".exe", VersionCode);
                    Microsoft.Win32.Registry.SetValue(Root + Key, AppName + ".vshost.exe", VersionCode);
                }
            }
            catch (Exception ex)
            {
                Tools.WriteToFile(Tools.errorFile, "useLatestIE error: "+ ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        // Update the status text
        void _windowManager_StatusTextChanged(object sender, TextChangedEventArgs e)
        {
            this.toolStripStatusLabel.Text = e.Text;
        }

        // Enable / disable buttons
        void _windowManager_CommandStateChanged(object sender, CommandStateEventArgs e)
        {
            this.forwardToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Forward) == BrowserCommands.Forward);
            this.backToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Back) == BrowserCommands.Back);
            this.printPreviewToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.PrintPreview) == BrowserCommands.PrintPreview);
            this.printPreviewToolStripMenuItem.Enabled = ((e.BrowserCommands & BrowserCommands.PrintPreview) == BrowserCommands.PrintPreview);
            this.printToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Print) == BrowserCommands.Print);
            this.printToolStripMenuItem.Enabled = ((e.BrowserCommands & BrowserCommands.Print) == BrowserCommands.Print);
            this.homeToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Home) == BrowserCommands.Home);
            this.searchToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Search) == BrowserCommands.Search);
            this.refreshToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Reload) == BrowserCommands.Reload);
            this.stopToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Stop) == BrowserCommands.Stop);
        }

        #region Tools menu
        // Executed when the user clicks on Tools -> Options
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OptionsForm of = new OptionsForm())
            {
                of.ShowDialog(this);
            }
        }

        // Tools -> Show script errors
        private void scriptErrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptErrorManager.Instance.ShowWindow();
        }

        //login to be able to access/modify blockedTerms file
        private void loginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Login login = new Login())
            {
                login.ShowDialog(this);
                if (login.DialogResult == DialogResult.Yes)
                {
                    LoggedIn = true;
                    adminLock = false;
                }
                else if (login.DialogResult == DialogResult.None)
                    adminLock = true;
                else
                    LoggedIn = false;
            }
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoggedIn = false;
        }

        private void UpdateLoginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Login login = new Login())
            {
                login.Update = true;
                login.ShowDialog(this);
            }
        }

        private void modifyBlockedTermsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //display terms
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();

            tcAdmin.SelectedTab = tcAdmin.TabPages["tpChangeLoginInfo"];
        }

        private void modifyBlockedSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpBlockedSites"];
        }

        private void foundBlockedTermsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpFoundBlockedTerms"];
        }

        private void foundBlockedSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpFoundBlockedSites"];
        }
        #endregion

        #region File Menu

        // File -> Print
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Print();
        }

        // File -> Print Preview
        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        // File -> Exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // File -> Open URL
        private void openUrlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenUrlForm ouf = new OpenUrlForm())
            {
                if (ouf.ShowDialog() == DialogResult.OK)
                {
                    ExtendedWebBrowser brw = _windowManager.New(false);
                    brw.Navigate(ouf.Url);
                }
            }
        }

        // File -> Open File
        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = Properties.Resources.OpenFileDialogFilter;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Uri url = new Uri(ofd.FileName);
                    WindowManager.Open(url);
                }
            }
        }
        #endregion

        #region Help Menu

        // Executed when the user clicks on Help -> About
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About();
        }

        /// <summary>
        /// Shows the AboutForm
        /// </summary>
        private void About()
        {
            using (AboutForm af = new AboutForm())
            {
                af.ShowDialog(this);
            }
        }

        #endregion

        /// <summary>
        /// The WindowManager class
        /// </summary>
        public WindowManager _windowManager;

        // This is handy when all the tabs are closed.
        private void tabControlEx_VisibleChanged(object sender, EventArgs e)
        {
            if (tabControlEx.Visible)
            {
                this.panel1.BackColor = SystemColors.Control;
            }
            else
                this.panel1.BackColor = SystemColors.AppWorkspace;
        }

        #region Printing & Print Preview
        private void Print()
        {
            ExtendedWebBrowser brw = _windowManager.ActiveBrowser;
            if (brw != null)
                brw.ShowPrintDialog();
        }

        private void PrintPreview()
        {
            ExtendedWebBrowser brw = _windowManager.ActiveBrowser;
            if (brw != null)
                brw.ShowPrintPreviewDialog();
        }
        #endregion

        #region Toolstrip buttons
        private void closeWindowToolStripButton_Click(object sender, EventArgs e)
        {
            this._windowManager.New();
        }

        private void closeToolStripButton_Click(object sender, EventArgs e)
        {
            //closes browser window
            //this._windowManager.Close();

            //closes admin tabPages
            tcAdmin.Visible = false;
        }

        private void tabEx_OnClose(object sender, CloseEventArgs e)
        {
            //this.userControl11.Controls.Remove(this.userControl11.TabPages[e.TabIndex]);

            //closes browser window
            this._windowManager.Close();
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            Print();
        }

        private void printPreviewToolStripButton_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        private void backToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.CanGoBack)
                _windowManager.ActiveBrowser.GoBack();
        }

        private void forwardToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.CanGoForward)
                _windowManager.ActiveBrowser.GoForward();
        }

        private void stopToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
            {
                _windowManager.ActiveBrowser.Stop();
            }
            stopToolStripButton.Enabled = false;
        }

        private void refreshToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
            {
                _windowManager.ActiveBrowser.Refresh(WebBrowserRefreshOption.Normal);
            }
        }

        private void homeToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
                _windowManager.ActiveBrowser.GoHome();
        }

        private void searchToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
                _windowManager.ActiveBrowser.GoSearch();
        }

        #endregion

        public WindowManager WindowManager
        {
            get { return _windowManager; }
        }

        /// <summary>
        /// load blocked terms from file
        /// </summary>
        /// <param name="path"></param>
        public void loadBlockedTerms(string path)
        {
            blockedTerms = File.ReadAllLines(path);

            if (!validateBlockedTerms())
            {
                //decrypt terms
                blockedTerms = Encryption.Decrypt(blockedTerms);
            }

            if (!validateBlockedTerms())
            {
                //log that terms have been tampered with
                log(blockedTermsPath, "Blocked Terms file has been tampered with.  Reinstall SafeSurf");
                //block all pages
                adminLock = true;
            }

            dgBlockedTerms.Dock = DockStyle.Fill;
            dgBlockedTerms.Anchor = AnchorStyles.Right;
            dgBlockedTerms.Anchor = AnchorStyles.Bottom;
            dgBlockedTerms.Anchor = AnchorStyles.Left;
            dgBlockedTerms.Anchor = AnchorStyles.Top;
            dgBlockedTerms.Columns.Add("Terms", "Terms");
            dgBlockedTerms.Refresh();

            foreach (string term in blockedTerms)
            {
                dgBlockedTerms.Rows.Add(new string[] { term });
            }
        }

        private void loadBlockedSites(string path)
        {
            blockedSites = File.ReadAllLines(path);

            if (!validateBlockedSites())
            {
                //decrypt terms
                blockedSites = Encryption.Decrypt(blockedSites);
            }

            if (!validateBlockedSites())
            {
                //log that terms have been tampered with
                log(blockedSitesPath, "Blocked Sites file has been tampered with.  Reinstall SafeSurf");
                //block all pages
                adminLock = true;
            }

            dgBlockedSites.Dock = DockStyle.Fill;
            dgBlockedSites.Anchor = AnchorStyles.Right;
            dgBlockedSites.Anchor = AnchorStyles.Bottom;
            dgBlockedSites.Anchor = AnchorStyles.Left;
            dgBlockedSites.Anchor = AnchorStyles.Top;
            dgBlockedSites.Columns.Add("Sites", "Sites");

            foreach (string site in blockedSites)
            {
                dgBlockedSites.Rows.Add(new string[] { site });
            }
        }

        public void loadFoundBlockedTerms(string path)
        {
            string fBlockedTerms = "";

            if (File.Exists(path))
                foundBlockedTerms = File.ReadAllLines(path);

            if (foundBlockedTerms != null && foundBlockedTerms.Length > 0)
            {
                //if (!validateFoundBlockedTerms())
                //{
                //decrypt terms
                foundBlockedTerms = Encryption.Decrypt(foundBlockedTerms);
                //}

                if (!validateBlockedTerms())
                {
                    //log that terms have been tampered with
                    log(foundBlockedTermsPath, "Found Blocked Terms file has been tampered with.");
                    //block all pages
                    adminLock = true;
                }

                lbFoundBlockedTerms.DataSource = foundBlockedTerms;
            }
        }

        public void loadFoundBlockedSites(string path)
        {
            if (File.Exists(path))
                foundBlockedSites = File.ReadAllLines(path);

            if (foundBlockedSites != null && foundBlockedSites.Length > 0)
            {

                //if (!validateBlockedTerms())
                //{
                //decrypt terms
                foundBlockedSites = Encryption.Decrypt(foundBlockedSites);
                //}

                //if (!validateBlockedTerms())
                //{
                //    //log that terms have been tampered with
                //    log(blockedTermsPath, "Blocked Terms file has been tampered with.  Reinstall SafeSurf");
                //    //block all pages
                //    adminLock = true;
                //}

                lbFoundBlockedSites.DataSource = foundBlockedSites;
            }
        }

        private bool validateBlockedTerms()
        {
            bool isValid = false;

            foreach (string term in blockedTerms)
            {
                if (term.ToLower() == "fuck")
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        private bool validateBlockedSites()
        {
            bool isValid = false;

            foreach (string site in blockedSites)
            {
                if (site.ToLower() == "pussy.org")
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        private bool validateFoundBlockedTerms()
        {
            bool isValid = true;

            //foreach (string term in foundBlockedTerms)
            //{
            //    if (term.ToLower().Contains("fuck"))
            //    {
            //        isValid = true;
            //        break;
            //    }
            //}

            return isValid;
        }

        #region datagridview events
        private void dgBlockedTerms_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            //make sure values are valid
            //DataGridView dg = (DataGridView)sender;

        }

        private void dgBlockedTerms_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                //update blocked terms file
                ArrayList terms = new ArrayList();
                string value = "";
                DataGridView dg = (DataGridView)sender;
                foreach (DataGridViewRow row in dg.Rows)
                {
                    value = Convert.ToString(row.Cells["Terms"].Value);
                    if (value != null && value.Trim().Length > 0)
                        terms.Add(value);
                }

                blockedTerms = (string[])terms.ToArray(typeof(string));

                //encrypt
                blockedTerms = Encryption.Encrypt(blockedTerms);

                //save blockedTerms
                File.WriteAllLines(blockedTermsPath, blockedTerms);
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        private void logHeader(string path)
        {
            if (startTime.CompareTo(File.GetLastWriteTime(path)) == 1)
            {
                StringBuilder content = new StringBuilder();

                content.AppendLine();
                content.AppendLine("User: " + userName + "  Start Time: " + startTime);

                File.AppendAllText(path, Encryption.Encrypt(content.ToString()));
            }
        }

        public void log(string path, string content)
        {
            logHeader(path);

            File.AppendAllText(path, content);
        }

        public void log(string path, string[] content)
        {
            logHeader(path);

            File.WriteAllLines(path, content);
            //File.WriteAllText(path, content);
        }

        private void tcAdmin_VisibleChanged(object sender, EventArgs e)
        {
            closeToolStripButton.Visible = true;
        }

        private void loginTimer_Tick(object sender, EventArgs e)
        {
            loggedInTime++;

            if (loggedInTime > loginMaxTime)
            {
                loginTimer.Enabled = false;
                LoggedIn = false;
            }
        }

        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            string[] loginInfo = { "username:" + txtNewUserName.Text.Trim(), "password:" + txtNewPassword.Text.Trim() };
            loginInfo = Encryption.Encrypt(loginInfo);
            File.WriteAllLines(MainForm.loginInfoPath, loginInfo);
            lblLoginInfoUpdated.Visible = true;
        }

        private void tpChangeLoginInfo_Leave(object sender, EventArgs e)
        {
            lblLoginInfoUpdated.Visible = false;
        }

        private string getDefaultBrowser()
        {
            //original value on classesroot
            //"C:\Program Files\Internet Explorer\IEXPLORE.EXE" -nohome

            string browser = string.Empty;
            RegistryKey key = null;
            try
            {
                key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command",true);

                //trim off quotes
                //browser = key.GetValue(null).ToString().Replace("\"", "");
                //if (!browser.EndsWith(".exe"))
                //{
                //    //get rid of everything after the ".exe"
                //    browser = browser.Substring(0, browser.ToLower().LastIndexOf(".exe") + 4);
                //}

                browser = key.GetValue(null).ToString();
                
                //key.SetValue(null, (string)@browser);

                string safeSurfBrowser = "\""+ Application.ExecutablePath +"\"";

                key.SetValue(null, (string)@safeSurfBrowser);
            }
            finally
            {
                if (key != null) key.Close();
            }
            return browser;
        }

        private void JustinRecordtoolStripButton_Click(object sender, EventArgs e)
        {
            //need to get channel name from url
            string[] urlSegments = _windowManager.ActiveBrowser.Url.Segments;

            if (urlSegments[1].ToLower() != "directory")//this is a channel
            {
                string channelName = urlSegments[1];
                DialogResult result = MessageBox.Show("Are you sure you want to download from " + channelName, "Download " + channelName, MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    //pop up justin downloader and start downloading
                    //SCTVJustinTV.Downloader downloader = new SCTVJustinTV.Downloader(channelName, "12", Application.StartupPath + "\\JustinDownloads\\");
                    //SCTVJustinTV.Downloader downloader = new SCTVJustinTV.Downloader();
                    //downloader.Channel = channelName;
                    //downloader.Show();
                }
            }
            else
                MessageBox.Show("You must be watching the channel you want to record");
        }

        private void toolStripButtonFavorites_Click(object sender, EventArgs e)
        {
            string url = "";

            //check for url
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.Url.PathAndQuery.Length > 0)
            {
                url = _windowManager.ActiveBrowser.Url.PathAndQuery;

                //add to onlineMedia.xml
                //SCTVObjects.MediaHandler.AddOnlineMedia(_windowManager.ActiveBrowser.Url.Host, _windowManager.ActiveBrowser.Url.PathAndQuery, "Online", "Favorites", "", "");
            }
            else
                MessageBox.Show("You must browse to a website to add it to your favorites");
        }

        private void showAddressBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _windowManager.ShowAddressBar = showAddressBarToolStripMenuItem.Checked;

            showAddressBarToolStripMenuItem.Checked = !showAddressBarToolStripMenuItem.Checked;
        }

        private string findValue(string stringToParse, string startPattern, string endPattern)
        {
            return findValue(stringToParse, startPattern, endPattern, false);
        }

        private string findValue(string stringToParse, string startPattern, string endPattern, bool returnSearchPatterns)
        {
            int start = 0;
            int end = 0;
            string foundValue = "";

            try
            {
                start = stringToParse.IndexOf(startPattern);

                if (start > -1)
                {
                    if (!returnSearchPatterns)
                        stringToParse = stringToParse.Substring(start + startPattern.Length);
                    else
                        stringToParse = stringToParse.Substring(start);

                    end = stringToParse.IndexOf(endPattern);

                    if (end > 0)
                    {
                        if (returnSearchPatterns)
                            foundValue = stringToParse.Substring(0, end + endPattern.Length);
                        else
                            foundValue = stringToParse.Substring(0, end);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
                //Tools.WriteToFile(ex);
            }

            return foundValue;
        }

        private void btnPrizesStart_Click(object sender, EventArgs e)
        {
            numberOfUnclaimedEntered = 0;
            counterUnclaimedPrizes = 0;
            maxedOutPrizesCount = 0;
            enterTheContest = false;
            foundPrize = false;
            clickingButton = true;
            refreshUtilities.Cancel();
            refreshUtilities.GoToURL("https://prizegrab.com/prizes/popular/", 1, 0, true, lblRefreshTimer, prizeGrabBrowser);
            //prizeGrabBrowser.Url = new Uri();
        }

        private void btnSweepstakes_Click(object sender, EventArgs e)
        {
            numberOfUnclaimedEntered = 0;
            counterUnclaimedPrizes = 0;
            maxedOutPrizesCount = 0;
            foundEnd = false;
            enteredPrizeGroup = false;
            clickingButton = true;
            refreshUtilities.Cancel();
            
            string btnName = ((Button)sender).Name;
            btnName = btnName.Replace("btn", "");

            currentSweepstakesButtonIndex = Array.IndexOf(sweepstakesButtons, (Button)sender);

            refreshUtilities.GoToURL("http://prizegrab.com/p/" + sweepstakesTxtBoxes.Where(x => x.Name.EndsWith(btnName)).FirstOrDefault().Text + "/", 1, 0, true, lblRefreshTimer, prizeGrabBrowser);
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void txtSweepstakes1_TextChanged(object sender, EventArgs e)
        {
            btnSweepstakes1.Text = txtSweepstakes1.Text;
            Properties.Settings.Default.Sweepstakes1 = txtSweepstakes1.Text;
            Properties.Settings.Default.Save();
        }

        private void txtSweepstakes2_TextChanged(object sender, EventArgs e)
        {
            btnSweepstakes2.Text = txtSweepstakes2.Text;
            Properties.Settings.Default.Sweepstakes2 = txtSweepstakes2.Text;
            Properties.Settings.Default.Save();
        }

        private void txtSweepstakes3_TextChanged(object sender, EventArgs e)
        {
            btnSweepstakes3.Text = txtSweepstakes3.Text;
            Properties.Settings.Default.Sweepstakes3 = txtSweepstakes3.Text;
            Properties.Settings.Default.Save();
        }

        private void txtSweepstakes4_TextChanged(object sender, EventArgs e)
        {
            TextBox txtChangedText = (TextBox)sender;
            string txtName = txtChangedText.Name;
            txtName = txtName.Replace("txt", "");

            sweepstakesButtons.Where(x => x.Name.EndsWith(txtName)).FirstOrDefault().Text = txtChangedText.Text;

            //System.Configuration.SettingsProperty changedProp = null;

            //foreach (System.Configuration.SettingsProperty prop in Properties.Settings.Default.Properties)
            //{
            //    if (prop.Name.EndsWith(txtName))
            //    {
            //        changedProp = prop;
            //        break;
            //    }
            //}

            //changedProp = txtChangedText.Text;

            //btnSweepstakes4.Text = txtSweepstakes4.Text;
            Properties.Settings.Default.Sweepstakes4 = txtChangedText.Text;
            Properties.Settings.Default.Save();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            //clear all the count boxes
            sweepstakesTxtCountBoxes.ToList().ForEach(x => x.Text = "");

            txtPrizeCount.Text = "";
            numberOfPrizesEntered = 0;
        }

        private void logout()
        {
            refreshUtilities.GoToURL("https://prizegrab.com/account/logout/", 1, 0, lblRefreshTimer, prizeGrabBrowser);

            if (!switchingUsers)
                currentUser = "";
        }

        private void login()
        {
            if (users.Count > 0 && switchingUsers)
            {
                if (currentUser.Length == 0)
                    getCurrentUser();

                if (currentUser.Length > 0)
                {
                    users.Remove(currentUser);

                    if (users.Count > 0)
                        currentUser = users[0];
                }
            }

            if (users.Count > 0)
            {
                //login
                //<a href="#small-dialog" class="nav-item nav-link l-nav__item popup-with-zoom-anim login-link">Login</a>

                HtmlElementCollection elc = prizeGrabBrowser.Document.GetElementsByTagName("a");

                foreach (HtmlElement el in elc)
                {
                    if (el.InnerText != null && el.InnerText == "Login")
                    {
                        refreshUtilities.ClickElement(el, 5, 0, lblRefreshTimer);
                        loggingIn = true;

                        break;
                    }
                }
            }
            else
            {
                allDone = true;

                MessageBox.Show("All Done!");
            }

            switchingUsers = false;
        }

        private void populateUsernamePassword()
        {
            bool foundEmail = false;
            bool foundPassword = false;

            if (currentUser.Length == 0)
            {
                if (users.Count > 0)
                    currentUser = users[0];
            }

            //<div id="login-with-email" style="">
            //  < i class="fa fa-envelope"></i> <span>Login with Email</span>
            //</div>
            HtmlElementCollection elc = prizeGrabBrowser.Document.GetElementsByTagName("div");

            foreach (HtmlElement el in elc)
            {
                if (el.OuterHtml != null && el.OuterHtml.Contains("id=\"login-with-email"))
                {
                    refreshUtilities.ClickElement(el, 1, 0, lblRefreshTimer);

                    break;
                }
            }
            
            //<input type="email" id="login-email" name="email" placeholder="Email Address">
            //<input type="password" id="login-password" name="password" placeholder="Password">

            elc = prizeGrabBrowser.Document.GetElementsByTagName("input");

            foreach (HtmlElement el in elc)
            {
                if (!foundEmail && el.OuterHtml != null && el.OuterHtml.Contains("id=\"login-email\""))
                {
                    el.SetAttribute("value", currentUser.Split('|')[0]);

                    foundEmail = true;
                }

                if (el.OuterHtml != null && el.OuterHtml.Contains("name=\"password\""))
                {
                    el.SetAttribute("value", currentUser.Split('|')[1]);

                    foundPassword = true;

                    break;
                }
            }

            if (foundEmail && foundPassword)
            {
                //click enter now
                //<input type="submit" id="login-button" class="btn btn-info" value="Login">

                elc = prizeGrabBrowser.Document.GetElementsByTagName("input");

                foreach (HtmlElement el in elc)
                {
                    if (el.OuterHtml != null && el.OuterHtml.Contains("id=\"login-button\""))
                    {
                        refreshUtilities.ClickElement(el, 2, 0, true, lblRefreshTimer);
                        loggingIn = false;

                        break;
                    }
                }
            }
        }

        private void switchUsers()
        {
            switchingUsers = true;

            logout();
        }

        private string getCurrentUser()
        {
            string docString = prizeGrabBrowser.DocumentText;

            foreach (string user in users)
            {
                if (docString.Contains(user.Split('|')[0]))
                {
                    currentUser = user;

                    return user;
                }
            }

            return "";
        }

        private void btnSwitchUsers_Click(object sender, EventArgs e)
        {
            switchUsers();
        }
    }
}