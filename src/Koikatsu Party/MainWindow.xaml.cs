﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using System.Linq;
using Microsoft.Win32;
using MessageBox = System.Windows.Forms.MessageBox;

namespace InitDialog
{
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWow64Process([In] IntPtr hProcess, out bool lpSystemInfo);

        [DllImport("user32.dll")]
        static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("User32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr rect, MainWindow.EnumDisplayMonitorsCallback callback, IntPtr dwData);

        [DllImport("User32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MainWindow.MonitorInfoEx info);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        IntPtr InBuffer, int nInBufferSize,
        IntPtr OutBuffer, int nOutBufferSize,
        out int pBytesReturned, IntPtr lpOverlapped);

        public MainWindow()
        {
            InitializeComponent();
            //if (!DoubleStartCheck())
            //{
            //    System.Windows.Application.Current.MainWindow.Close();
            //    return;
            //}

            // Check for duplicate launches

            Process process = Process.GetCurrentProcess();
            var dupl = (Process.GetProcessesByName(process.ProcessName));
            if (true)
            {
                foreach (var p in dupl)
                {
                    if (p.Id != process.Id)
                        p.Kill();
                }
            }

            startup = true;

            Directory.CreateDirectory(m_strCurrentDir + m_customDir);

            // Updater stuffs

            kkmanExist = File.Exists(m_strCurrentDir + m_customDir + kkmdir);
            updatelocExists = File.Exists(m_strCurrentDir + m_customDir + updateLoc);
            if (kkmanExist)
            {
                var kkmanFileStream = new FileStream(m_strCurrentDir + m_customDir + kkmdir, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(kkmanFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        kkman = line;
                    }
                    //UpdateButton.Visibility = Visibility.Visible;
                }
                kkmanFileStream.Close();
                if (updatelocExists)
                {
                    var updFileStream = new FileStream(m_strCurrentDir + m_customDir + updateLoc, FileMode.Open, FileAccess.Read);
                    using (var streamReader = new StreamReader(updFileStream, Encoding.UTF8))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            updated = line;
                        }
                    }
                    updFileStream.Close();
                }
                else
                {
                    updated = "https://mega.nz/#F!fkYzQa5K!nSc7wkY82OUqZ4Hlff7Rlg ftp://kkshare:ayaya@144.91.89.152";
                }
            }
            else
            {
                UpdateButton.Visibility = Visibility.Hidden;
            }

            // Check if Party fonts are present

            bool lang1en = File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\1\\font.unit-y3d");
            bool lang1di = File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\1\\font.unity3d");
            bool lang2en = File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\2\\font.unit-y3d");
            bool lang2di = File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\2\\font.unity3d");
            bool lang3en = File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\3\\font.unit-y3d");
            bool lang3di = File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\3\\font.unity3d");

            if (!lang1en && !lang1di && !lang2en && !lang2di && !lang3en && !lang3di)
            {
                FontBG.Visibility = Visibility.Hidden;
                font.Visibility = Visibility.Hidden;
            }
            else if (lang1en || lang2en || lang3en)
            {
                font.IsChecked = true;
            }

                // Check if dev mode is active

                if (!File.Exists(m_strCurrentDir + "/Bepinex/config/BepInEx.cfg"))
            {
                modeDev.IsEnabled = false;
                File.Delete(m_strCurrentDir + m_customDir + "/devMode");
            }

            DevExists = File.Exists(m_strCurrentDir + m_customDir + "/devMode");
            if (DevExists)
            {
                modeDev.IsChecked = true;
            }

            startup = false;

            LangExists = File.Exists(m_strCurrentDir + m_customDir + decideLang);
            if (LangExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + decideLang, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        lang = line;
                    }
                }
                verFileStream.Close();
            }

            TransCred.Visibility = Visibility.Hidden;

            // MessageBox.Show($"Chinese is {chnActive}", "Debug");

            // Template for new translations
            //if (lang == "en-US")
            //{
            //    mainApp.Title = "Koikatsu Launcher";
            //    warnBox.Header = "Notice!";
            //    warningText.Text = "This game is intended for adult audiences, no person under the age of 18 (or equivalent according to local law) are supposed to play or be in possession of this game.\n\nThis game contains content of a sexual nature, and some of the actions depicted within may be illegal to replicate in real life. Aka, it's all fun and games in the game, let's keep it that way shall we? (~.~)v";
            //    GameFBox.Header = "Game folders";
            //    InstallDirectory.Content = "Install";
            //    KoikatuCharaDirectory.Content = "Character Cards";
            //    SceneDirectory.Content = "Scenes";
            //    KoikatuScreenShotDirectory.Content = "ScreenShots";
            //    GameSBox.Header = "Game Startup";
            //    PLAY.Content = "Start Koikatsu";
            //    Manual_Open.Content = "Koikatsu Manual";
            //    PLAY_Studio.Content = "Start Studio";
            //    Manual_s_Open.Content = "Studio Manual";
            //    PLAY_VR.Content = "Start Koikatsu VR";
            //    Manual_v_Open.Content = "VR Manual";
            //    SettingsBox.Header = "Settings";
            //    modeFenetre.Content = "Run Game in Fullscreen";
            //    SystemInfo.Content = "System Info";
            //    EXIT.Content = "Exit";
            //    Versioning.Text = "Unknown Install Method";
            //    TransCred.Text = "Launcher translated by: <Insert Name>";
            //    translationString = "Do you want to restore Japanese language in-game?";
            //    q_performance = "Performance";
            //    q_normal = "Normal";
            //    q_quality = "Quality";
            //    s_primarydisplay = "PrimaryDisplay";
            //    s_subdisplay = "SubDisplay";
            //}

            // Translations
            if (lang == "zh-CN") // By @Madevil#1103 & @𝐄𝐀𝐑𝐓𝐇𝐒𝐇𝐈𝐏 💖#4313 
            {
                TransCred.Visibility = Visibility.Visible;

                mainApp.Title = "恋爱活动启动器";
                warnBox.Header = "声明";
                warningText.Text = "此游戏适用于成人用户，任何未满18岁的人（或根据当地法律规定的同等人）都不得遊玩或拥有此游戏。\n\n这个游戏包含性相关的内容，某些行为在现实生活中可能是非法的。所以，游戏中的所有乐趣请保留在游戏中，让我们保持这种方式吧? (~.~)v";
                GameFBox.Header = "文件夹";
                InstallDirectory.Content = "游戏主目录";
                KoikatuCharaDirectory.Content = "人物卡";
                SceneDirectory.Content = "工作室场景";
                KoikatuScreenShotDirectory.Content = "截图";
                GameSBox.Header = "启动";
                PLAY.Content = "恋爱活动";
                Manual_Open.Content = "说明文件";
                PLAY_Studio.Content = "工作室";
                Manual_s_Open.Content = "工作室说明";
                PLAY_VR.Content = "恋爱活动 VR";
                Manual_v_Open.Content = "VR 说明";
                SettingsBox.Header = "设置";
                modeFenetre.Content = "全屏执行";
                modeDev.Content = "开发者模式";
                SystemInfo.Content = "系统资讯";
                EXIT.Content = "关闭";
                Versioning.Text = "未知版本";
                TransCred.Text = "Launcher translated by: Madevil & Earthship";
                q_performance = "性能";
                q_normal = "标准";
                q_quality = "高画质";
                s_primarydisplay = "主显示器";
                s_subdisplay = "次显示器";
            }

            m_astrQuality = new string[]
            {
                q_performance,
                q_normal,
                q_quality
            };

            // Do checks

            is64bitOS = Is64BitOS();
            isStudio = File.Exists(m_strCurrentDir + m_strStudioExe);
            isVR = File.Exists(m_strCurrentDir + m_strVRExe);
            isMainGame = File.Exists(m_strCurrentDir + m_strGameExe);

            // Customization options

            CharExists = File.Exists(m_strCurrentDir + m_customDir + charLoc);
            BackgExists = File.Exists(m_strCurrentDir + m_customDir + backgLoc);
            WarningExists = File.Exists(m_strCurrentDir + m_customDir + warningLoc);
            PatreonExists = File.Exists(m_strCurrentDir + m_customDir + patreonLoc);

            // Launcher Customization: Grabbing versioning of install method

            versionAvail = File.Exists(m_strCurrentDir + "version");
            if (versionAvail)
            {
                var verFileStream = new FileStream(m_strCurrentDir + "version", FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        Versioning.Text = line;
                    }
                }
                verFileStream.Close();
            }

            // Launcher Customization: Defining Warning, background and character

            if (WarningExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + warningLoc, FileMode.Open, FileAccess.Read);
                try
                {
                    using (StreamReader sr = new StreamReader(m_strCurrentDir + m_customDir + warningLoc))
                    {
                        String line = sr.ReadToEnd();
                        warningText.Text = line;
                    }
                }
                catch (IOException e)
                {
                    warningText.Text = e.Message;
                }
            }
            if (CharExists)
            {
                Uri urich = new Uri(m_strCurrentDir + m_customDir + charLoc, UriKind.RelativeOrAbsolute);
                PackChara.Source = BitmapFrame.Create(urich);
            }
            if (BackgExists)
            {
                Uri uribg = new Uri(m_strCurrentDir + m_customDir + backgLoc, UriKind.RelativeOrAbsolute);
                appBG.Source = BitmapFrame.Create(uribg);
            }
            if (PatreonExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + patreonLoc, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        patreonURL = line;
                    }
                }
                verFileStream.Close();
            }
            else
            {
                PatreonButton.Visibility = Visibility.Collapsed;
            }

            int num = Screen.AllScreens.Length;
            getDisplayMode_EnumDisplaySettings(num);
            m_Setting.m_strSizeChoose = "1280 x 720 (16 : 9)";
            m_Setting.m_nWidthChoose = 1280;
            m_Setting.m_nHeightChoose = 720;
            m_Setting.m_nQualityChoose = 1;
            m_Setting.m_nLangChoose = 0;
            m_Setting.m_nDisplay = 0;
            m_Setting.m_bFullScreen = false;
            m_Setting.m_nLangChoose = 1;
            if (num == 2)
            {
                DisplayBox.Items.Add(s_primarydisplay);
                DisplayBox.Items.Add($"{s_subdisplay} : 1");
            }
            else
            {
                for (int i = 0; i < num; i++)
                {
                    string newItem = (i == 0) ? s_primarydisplay : ($"{s_subdisplay} : " + i);
                    DisplayBox.Items.Add(newItem);
                }
            }
            foreach (string newItem2 in m_astrQuality)
            {
                QualityBox.Items.Add(newItem2);
            }

            SetEnableAndVisible();

            string path = m_strCurrentDir + m_strSaveDir;
        CheckConfigFile:
            if (File.Exists(path))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(path, FileMode.Open))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigSetting));
                        m_Setting = (ConfigSetting)xmlSerializer.Deserialize(fileStream);
                    }

                    m_Setting.m_nDisplay = Math.Min(m_Setting.m_nDisplay, num - 1);
                    setDisplayComboBox(m_Setting.m_bFullScreen);
                    var flag = false;
                    for (var k = 0; k < ResolutionBox.Items.Count; k++)
                    {
                        if (ResolutionBox.Items[k].ToString() == m_Setting.m_strSizeChoose)
                            flag = true;
                    }
                    ResolutionBox.Text = flag ? m_Setting.m_strSizeChoose : "1280 x 720 (16 : 9)";
                    modeFenetre.IsChecked = m_Setting.m_bFullScreen;
                    QualityBox.Text = m_astrQuality[m_Setting.m_nQualityChoose];
                    string text = m_Setting.m_nDisplay == 0 ? s_primarydisplay : $"{s_subdisplay} : " + m_Setting.m_nDisplay;
                    if (num == 2)
                    {
                        text = new[]
                        {
                        s_primarydisplay,
                        $"{s_subdisplay} : 1"
                        }[m_Setting.m_nDisplay];
                    }
                    if (DisplayBox.Items.Contains(text))
                        DisplayBox.Text = text;
                    else
                    {
                        DisplayBox.Text = s_primarydisplay;
                        m_Setting.m_nDisplay = 0;
                    }
                }
                catch (Exception)
                {
                    System.Windows.Forms.MessageBox.Show("/UserData/setup.xml file was corrupted, settings will be reset.");
                    File.Delete(path);
                    goto CheckConfigFile;
                }
            }
            else
            {
                setDisplayComboBox(false);
                ResolutionBox.Text = m_Setting.m_strSizeChoose;
                QualityBox.Text = m_astrQuality[m_Setting.m_nQualityChoose];
                DisplayBox.Text = s_primarydisplay;
            }
        }

        void SetEnableAndVisible()
        {
            if (!isMainGame)
            {
                PLAY.IsEnabled = false;
                Manual_Open.IsEnabled = false;
                InstallDirectory.IsEnabled = false;
                KoikatuCharaDirectory.IsEnabled = false;
                KoikatuScreenShotDirectory.IsEnabled = false;
            }
            if (!isVR)
            {
                PLAY_VR.IsEnabled = false;
                Manual_v_Open.IsEnabled = false;
            }
            if (!isStudio)
            {
                PLAY_Studio.IsEnabled = false;
                Manual_s_Open.IsEnabled = false;
                SceneDirectory.IsEnabled = false;
            }
        }

        void SaveRegistry()
        {
            using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(m_strGameRegistry))
            {
                registryKey.SetValue("Screenmanager Is Fullscreen mode_h3981298716", m_Setting.m_bFullScreen ? 1 : 0);
                registryKey.SetValue("Screenmanager Resolution Height_h2627697771", m_Setting.m_nHeightChoose);
                registryKey.SetValue("Screenmanager Resolution Width_h182942802", m_Setting.m_nWidthChoose);
                registryKey.SetValue("UnityGraphicsQuality_h1669003810", 2);
                registryKey.SetValue("UnitySelectMonitor_h17969598", m_Setting.m_nDisplay);
            }
            if (isStudio)
            {
                using (RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey(m_strStudioRegistry))
                {
                    registryKey2.SetValue("Screenmanager Is Fullscreen mode_h3981298716", m_Setting.m_bFullScreen ? 1 : 0);
                    registryKey2.SetValue("Screenmanager Resolution Height_h2627697771", m_Setting.m_nHeightChoose);
                    registryKey2.SetValue("Screenmanager Resolution Width_h182942802", m_Setting.m_nWidthChoose);
                    registryKey2.SetValue("UnityGraphicsQuality_h1669003810", 2);
                    registryKey2.SetValue("UnitySelectMonitor_h17969598", m_Setting.m_nDisplay);
                }
            }
            if (isVR)
            {
                using (RegistryKey registryKey3 = Registry.CurrentUser.CreateSubKey(m_strVRRegistry))
                {
                    registryKey3.SetValue("Screenmanager Is Fullscreen mode_h3981298716", m_Setting.m_bFullScreen ? 1 : 0);
                    registryKey3.SetValue("Screenmanager Resolution Height_h2627697771", m_Setting.m_nHeightChoose);
                    registryKey3.SetValue("Screenmanager Resolution Width_h182942802", m_Setting.m_nWidthChoose);
                    registryKey3.SetValue("UnityGraphicsQuality_h1669003810", 2);
                    registryKey3.SetValue("UnitySelectMonitor_h17969598", m_Setting.m_nDisplay);
                }
            }
        }

        void PlayFunc(string strExe)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            SaveRegistry();
            if (!is64bitOS)
            {
                new MessageWindow().SetupWindow("Warning", "This application requires a x64 version of windows.", new object[0]);
                return;
            }
            string text = m_strCurrentDir + strExe;
            if (File.Exists(text))
            {
                Process.Start(new ProcessStartInfo(text) { WorkingDirectory = m_strCurrentDir });
                System.Windows.Application.Current.MainWindow.Close();
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nCould not find the executable.", new object[0]);
        }

        void PLAY_Click(object sender, RoutedEventArgs e)
        {
            PlayFunc(m_strGameExe);
        }

        void PLAY_Studio_Click(object sender, RoutedEventArgs e)
        {
            PlayFunc(m_strStudioExe);
        }

        void PLAY_VR_Click(object sender, RoutedEventArgs e)
        {
            PlayFunc(m_strVRExe);
        }

        void Exit_Click(object sender, RoutedEventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            ReleaseMutex();
            System.Windows.Application.Current.MainWindow.Close();
        }

        void Resolution_Change(object sender, SelectionChangedEventArgs e)
        {
            if (-1 == ResolutionBox.SelectedIndex)
            {
                return;
            }
            ComboBoxCustomItem comboBoxCustomItem = (ComboBoxCustomItem)ResolutionBox.SelectedItem;
            m_Setting.m_strSizeChoose = comboBoxCustomItem.text;
            m_Setting.m_nWidthChoose = comboBoxCustomItem.width;
            m_Setting.m_nHeightChoose = comboBoxCustomItem.height;
        }

        void Quality_Change(object sender, SelectionChangedEventArgs e)
        {
            string a = QualityBox.SelectedItem.ToString();
            if (a == q_performance)
            {
                m_Setting.m_nQualityChoose = 0;
                return;
            }
            if (a == q_normal)
            {
                m_Setting.m_nQualityChoose = 1;
                return;
            }
            if (!(a == q_quality))
            {
                return;
            }
            m_Setting.m_nQualityChoose = 2;
        }

        void windowUnChecked(object sender, RoutedEventArgs e)
        {
            setDisplayComboBox(false);
            ResolutionBox.Text = m_Setting.m_strSizeChoose;
            m_Setting.m_bFullScreen = false;
        }

        void windowChecked(object sender, RoutedEventArgs e)
        {
            setDisplayComboBox(true);
            m_Setting.m_bFullScreen = true;
            setFullScreenDevice();
        }

        void ManualOpen(object sender, RoutedEventArgs e)
        {
            string text = m_strCurrentDir + m_strManualDir;
            if (File.Exists(text))
            {
                Process.Start(text);
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nThe manual could not be found.", new object[0]);
        }

        void ManualOpenS(object sender, RoutedEventArgs e)
        {
            string text = m_strCurrentDir + m_strStudioManualDir;
            if (File.Exists(text))
            {
                Process.Start(text);
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nThe manual could not be found.", new object[0]);
        }

        void ManualOpenV(object sender, RoutedEventArgs e)
        {
            string text = m_strCurrentDir + m_strVRManualDir;
            if (File.Exists(text))
            {
                Process.Start(text);
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nThe manual could not be found.", new object[0]);
        }

        void Display_Change(object sender, SelectionChangedEventArgs e)
        {
            if (-1 == DisplayBox.SelectedIndex)
            {
                return;
            }
            m_Setting.m_nDisplay = DisplayBox.SelectedIndex;
            if (m_Setting.m_bFullScreen)
            {
                setDisplayComboBox(true);
                setFullScreenDevice();
            }
        }

        void InstallDir_Open(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2);
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nCan't find the folder, please launch the game once.", new object[0]);
        }

        void SceneDir_Open(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\Studio\\scene";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nCan't find the folder, please launch the game once.", new object[0]);
        }

        void KoikatuSSDir_Open(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\cap";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nCan't find the folder, please launch the game once.", new object[0]);
        }

        void KoikatuCharaDir_Open(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\chara";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nCan't find the folder, please launch the game once.", new object[0]);
        }

        void SystemInfo_Open(object sender, RoutedEventArgs e)
        {
            string text = Environment.ExpandEnvironmentVariables("%windir%") + "/System32/dxdiag.exe";
            if (File.Exists(text))
            {
                Process.Start(text);
                return;
            }
            new MessageWindow().SetupWindow("Warning", "\nCan't find the folder, please launch the game once.", new object[0]);
        }

        bool DoubleStartCheck()
        {
            bool flag;
            mutex = new Mutex(true, "Koikatu", out flag);
            bool v = !flag;
            if (v)
            {
                if (mutex != null)
                {
                    mutex.Close();
                }
                mutex = null;
                return false;
            }
            return true;
        }

        bool ReleaseMutex()
        {
            if (mutex == null)
            {
                return false;
            }
            mutex.ReleaseMutex();
            mutex.Close();
            mutex = null;
            return true;
        }

        void setDisplayComboBox(bool _bFullScreen)
        {
            ResolutionBox.Items.Clear();
            int nDisplay = m_Setting.m_nDisplay;
            foreach (MainWindow.DisplayMode displayMode in (_bFullScreen ? m_listCurrentDisplay[nDisplay].list : m_listDefaultDisplay))
            {
                ComboBoxCustomItem newItem = new ComboBoxCustomItem
                {
                    text = displayMode.text,
                    width = displayMode.Width,
                    height = displayMode.Height
                };
                ResolutionBox.Items.Add(newItem);
            }
        }

        void saveConfigFile(string _strFilePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(_strFilePath)))
            {
                return;
            }
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(_strFilePath, FileMode.Create);
                if (fileStream != null)
                {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.GetEncoding("UTF-16")))
                    {
                        XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
                        xmlSerializerNamespaces.Add(string.Empty, string.Empty);
                        new XmlSerializer(typeof(ConfigSetting)).Serialize(streamWriter, m_Setting, xmlSerializerNamespaces);
                        fileStream = null;
                    }
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
        }

        void getDisplayMode_CIM_VideoControllerResolution()
        {
            ManagementObjectCollection instances = new ManagementClass("CIM_VideoControllerResolution").GetInstances();
            List<MainWindow.DisplayMode> list = new List<MainWindow.DisplayMode>();
            uint num = 0u;
            uint num2 = 0u;
            foreach (ManagementBaseObject managementBaseObject in instances)
            {
                ManagementObject managementObject = (ManagementObject)managementBaseObject;
                uint nXX = (uint)managementObject["HorizontalResolution"];
                uint nYY = (uint)managementObject["VerticalResolution"];
                if ((num != nXX || num2 != nYY) && (ulong)managementObject["NumberOfColors"] == 4294967296UL)
                {
                    MainWindow.DisplayMode displayMode = m_listDefaultDisplay.Find((MainWindow.DisplayMode i) => (long)i.Width == (long)((ulong)nXX) && (long)i.Height == (long)((ulong)nYY));
                    if (displayMode.Width != 0)
                    {
                        list.Add(displayMode);
                    }
                    num = nXX;
                    num2 = nYY;
                }
            }
            MainWindow.DisplayModes item = default(MainWindow.DisplayModes);
            item.list = list;
            m_listCurrentDisplay.Add(item);
            if (instances.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("Failed to list screens");
                return;
            }
            if (m_listCurrentDisplay.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("Failed to list supported resolutions");
            }
        }

        void getDisplayMode_EnumDisplaySettings(int numDisplay)
        {
            DISPLAY_DEVICE display_DEVICE = default(DISPLAY_DEVICE);
            display_DEVICE.cb = Marshal.SizeOf(display_DEVICE);
            List<string> list = new List<string>();
            MainWindow.MonitorInfoEx[] monitors = MainWindow.GetMonitors();
            uint num = 0u;
            while (MainWindow.EnumDisplayDevices(null, num, ref display_DEVICE, 1u))
            {
                if ((display_DEVICE.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) == DisplayDeviceStateFlags.AttachedToDesktop)
                {
                    list.Add(display_DEVICE.DeviceName);
                }
                num += 1u;
            }
            int num2 = 0;
            int num3 = -1;
            foreach (string text in list)
            {
                int num4 = 0;
                int num5 = 0;
                DEVMODE devmode = default(DEVMODE);
                List<MainWindow.DisplayMode> list2 = new List<MainWindow.DisplayMode>();
                int num6 = 0;
                while (MainWindow.EnumDisplaySettings(text, num6, ref devmode))
                {
                    int nXX = devmode.dmPelsWidth;
                    int nYY = devmode.dmPelsHeight;
                    if ((num4 != nXX || num5 != nYY) && devmode.dmBitsPerPel == 32)
                    {
                        MainWindow.DisplayMode displayMode = m_listDefaultDisplay.Find((MainWindow.DisplayMode dis) => dis.Width == nXX && dis.Height == nYY);
                        if (displayMode.Width != 0)
                        {
                            list2.Add(displayMode);
                        }
                        num4 = nXX;
                        num5 = nYY;
                    }
                    num6++;
                }
                MainWindow.DisplayModes item = default(MainWindow.DisplayModes);
                foreach (MainWindow.MonitorInfoEx monitorInfoEx in monitors)
                {
                    if (monitorInfoEx.szDevice == text)
                    {
                        item.x = monitorInfoEx.rcWork.Left;
                        item.y = monitorInfoEx.rcWork.Top;
                        if (monitorInfoEx.dwFlags == 1)
                        {
                            num3 = num2;
                        }
                    }
                }
                item.list = list2;
                num2++;
                m_listCurrentDisplay.Add(item);
            }
            if (m_listCurrentDisplay.Count == 0 || m_listCurrentDisplay.Count != numDisplay)
            {
                System.Windows.Forms.MessageBox.Show("Failed to list supported resolutions");
            }
            m_listCurrentDisplay.Insert(0, m_listCurrentDisplay[num3]);
            m_listCurrentDisplay.RemoveAt(num3 + 1);
        }

        static int DisplaySort(MainWindow.DisplayModes a, MainWindow.DisplayModes b)
        {
            if (a.x < b.x)
            {
                return -1;
            }
            if (a.x > b.x)
            {
                return 1;
            }
            if (a.y < b.y)
            {
                return -1;
            }
            if (a.y > b.y)
            {
                return 1;
            }
            return 0;
        }

        static MainWindow.MonitorInfoEx[] GetMonitors()
        {
            List<MainWindow.MonitorInfoEx> list = new List<MainWindow.MonitorInfoEx>();
            MainWindow.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
            {
                MainWindow.MonitorInfoEx item = new MainWindow.MonitorInfoEx
                {
                    cbSize = Marshal.SizeOf(typeof(MainWindow.MonitorInfoEx))
                };
                MainWindow.GetMonitorInfo(hMonitor, ref item);
                list.Add(item);
            }, IntPtr.Zero);
            return list.ToArray();
        }

        void setFullScreenDevice()
        {
            int nDisplay = m_Setting.m_nDisplay;
            if (m_listCurrentDisplay[nDisplay].list.Count == 0)
            {
                m_Setting.m_bFullScreen = false;
                modeFenetre.IsChecked = new bool?(false);
                System.Windows.Forms.MessageBox.Show("This monitor doesn't support fullscreen.");
                return;
            }
            if (m_listCurrentDisplay[nDisplay].list.Find((MainWindow.DisplayMode x) => x.text.Contains(m_Setting.m_strSizeChoose)).Width == 0)
            {
                m_Setting.m_strSizeChoose = m_listCurrentDisplay[nDisplay].list[0].text;
                m_Setting.m_nWidthChoose = m_listCurrentDisplay[nDisplay].list[0].Width;
                m_Setting.m_nHeightChoose = m_listCurrentDisplay[nDisplay].list[0].Height;
            }
            ResolutionBox.Text = m_Setting.m_strSizeChoose;
        }

        public bool IsWow64()
        {
            bool flag;
            return MainWindow.GetProcAddress(MainWindow.GetModuleHandle("Kernel32.dll"), "IsWow64Process") != IntPtr.Zero && MainWindow.IsWow64Process(Process.GetCurrentProcess().Handle, out flag) && flag;
        }

        public bool Is64BitOS()
        {
            if (IntPtr.Size == 4)
            {
                return IsWow64();
            }
            return IntPtr.Size == 8;
        }

        void MenuCloseButton(object sender, EventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            ReleaseMutex();
        }

        const int MONITORINFOF_PRIMARY = 1;

        string[] m_astrQuality;
        string[] s_EnglishTL;

        string m_strGameRegistry = "Software\\illusion\\Koikatsu\\Koikatsu Party\\";
        string m_strStudioRegistry = "Software\\illusion\\Koikatu\\CharaStudio\\";
        string m_strVRRegistry = "Software\\illusion\\Koikatu\\KoikatuVR\\";
        string m_strGameExe = "Koikatsu Party.exe";
        string m_strStudioExe = "CharaStudio.exe";
        string m_strVRExe = "Koikatsu Party VR.exe";
        string m_strManualDir = "/manual/English/README.html";
        string m_strStudioManualDir = "/manual_s/お読み下さい.html";
        string m_strVRManualDir = "/manual_v/English/README.html";

        const string m_strSaveDir = "/UserData/setup.xml";
        const string m_customDir = "/UserData/LauncherEN";

        const string m_strDefSizeText = "1280 x 720 (16 : 9)";
        const int m_nDefQuality = 1;
        const int m_nDefWidth = 1280;
        const int m_nDefHeight = 720;
        const bool m_bDefFullScreen = false;

        string m_strCurrentDir = Environment.CurrentDirectory + "\\";

        ConfigSetting m_Setting = new ConfigSetting();

        bool is64bitOS;

        bool isStudio;
        bool isVR;
        bool isMainGame;

        string lang = "en"; 
        bool startup;

        bool versionAvail;
        bool WarningExists;
        bool CharExists;
        bool BackgExists;
        bool PatreonExists;
        bool LangExists;
        bool DevExists;
        bool kkmanExist;
        bool updatelocExists;

        string kkman;
        string updated;

        string q_performance = "Performance";
        string q_normal = "Normal";
        string q_quality = "Quality";
        string s_primarydisplay = "PrimaryDisplay";
        string s_subdisplay = "SubDisplay";

        const string decideLang = "/lang";
        const string versioningLoc = "/version";
        const string warningLoc = "/warning.txt";
        const string charLoc = "/Chara.png";
        const string backgLoc = "/LauncherBG.png";
        const string patreonLoc = "/patreon.txt";
        const string kkmdir = "/kkman.txt";
        const string updateLoc = "/updater.txt";

        string patreonURL;

        List<MainWindow.DisplayMode> m_listDefaultDisplay = new List<MainWindow.DisplayMode>
        {
            new MainWindow.DisplayMode
            {
                Width = 854,
                Height = 480,
                text = "854 x 480 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1024,
                Height = 576,
                text = "1024 x 576 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1136,
                Height = 640,
                text = "1136 x 640 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1280,
                Height = 720,
                text = "1280 x 720 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1366,
                Height = 768,
                text = "1366 x 768 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1536,
                Height = 864,
                text = "1536 x 864 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1600,
                Height = 900,
                text = "1600 x 900 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1920,
                Height = 1080,
                text = "1920 x 1080 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 2048,
                Height = 1152,
                text = "2048 x 1152 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 2560,
                Height = 1440,
                text = "2560 x 1440 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 3200,
                Height = 1800,
                text = "3200 x 1800 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 3840,
                Height = 2160,
                text = "3840 x 2160 (16 : 9)"
            }
        };

        List<MainWindow.DisplayModes> m_listCurrentDisplay = new List<MainWindow.DisplayModes>();

        const int m_nQualityCount = 3;

        



        Mutex mutex;

        delegate void EnumDisplayMonitorsCallback(IntPtr hMonir, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

        internal struct MonitorInfoEx
        {
            public int cbSize;

            public MainWindow.Rect rcMonitor;

            public MainWindow.Rect rcWork;

            public int dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        public struct Rect
        {
            public int Left;

            public int Top;

            public int Right;

            public int Bottom;
        }

        struct DisplayMode
        {
            public int Width;

            public int Height;

            public string text;
        }

        struct DisplayModes
        {
            public int x;

            public int y;

            public List<MainWindow.DisplayMode> list;
        }

        void discord_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start("https://universalhentai.com/KoiLauncher");
        }
        void patreon_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start(patreonURL);
        }

        void update_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            SaveRegistry();

            string marcofix = m_strCurrentDir.TrimEnd('\\', '/', ' ');
            kkman = kkman.TrimEnd('\\', '/', ' ');
            string finaldir;

            if (!File.Exists($@"{kkman}\StandaloneUpdater.exe"))
            {
                finaldir = $@"{m_strCurrentDir}{kkman}";
            }
            else
            {
                finaldir = kkman;
            }

            string text = $@"{finaldir}\StandaloneUpdater.exe";

            string argdir = $"\u0022{marcofix}\u0022";
            string argloc = updated;
            string args = $"{argdir} {argloc}";

            if (File.Exists(text))
            {
                Process.Start(new ProcessStartInfo(text) { WorkingDirectory = $@"{finaldir}", Arguments = args });
                return;
            }
        }

        void langEnglish(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetupLang("en");
        }
        void langChinese(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetupLang("zh-CN");
        }
        void langChineseTrad(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetupLang("zh-CN2");
        }

        void WriteLangIni(string language)
        {
            if (File.Exists(m_strCurrentDir + "BepInEx/Config/AutoTranslatorConfig.ini"))
            {
                if (System.Windows.MessageBox.Show("Do you want the ingame language to reflect this language choice?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    helvete(language);
                }
                // Borrowed from Marco
            }
            //MessageBox.Show($"{curAutoTLOut}", "Debug");
        }

        void helvete(string language)
        {
            if(File.Exists("BepInEx/Config/AutoTranslatorConfig.ini"))
            {
                var ud = Path.Combine(m_strCurrentDir, @"BepInEx/Config/AutoTranslatorConfig.ini");

                try
                {
                    var contents = File.ReadAllLines(ud).ToList();

                    // Fix VMD for darkness
                    var setToLanguage = contents.FindIndex(s => s.ToLower().Contains("[General]".ToLower()));
                    if (setToLanguage >= 0)
                    {
                        var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Language"));
                        if (i > setToLanguage)
                            contents[i] = $"Language={language}";
                        else
                        {
                            contents.Insert(setToLanguage + 1, $"Language={language}");
                        }
                    }
                    else
                    {
                        contents.Add("");
                        contents.Add("[General]");
                        contents.Add($"Language={language}");
                    }

                    File.WriteAllLines(ud, contents.ToArray());
                }
                catch (Exception e)
                {
                    MessageBox.Show("Something went wrong: " + e);
                }
            }
        }

        void deactivateTL(int i)
        {
            s_EnglishTL = new string[]
            {
                "BepInEx/XUnity.AutoTranslator.Plugin.BepIn",
                "BepInEx/XUnity.AutoTranslator.Plugin.Core",
                "BepInEx/XUnity.AutoTranslator.Plugin.ExtProtocol",
                "BepInEx/XUnity.RuntimeHooker.Core",
                "BepInEx/XUnity.RuntimeHooker",
                "BepInEx/KK_Subtitles",
                "BepInEx/ExIni"
            };

            if (i == 0)
            {
                foreach (var file in s_EnglishTL)
                {
                    if (File.Exists(m_strCurrentDir + file + ".dll"))
                    {
                        File.Move(m_strCurrentDir + file + ".dll", m_strCurrentDir + file + "._ll");
                    }
                }
            }
            else
            {
                foreach (var file in s_EnglishTL)
                {
                    if (File.Exists(m_strCurrentDir + file + "._ll"))
                    {
                        File.Move(m_strCurrentDir + file + "._ll", m_strCurrentDir + file + ".dll");
                    }
                    helvete("en");
                }
            }
        }

        void SetupLang(string langstring)
        {
            if (File.Exists(m_strCurrentDir + m_customDir + decideLang))
            {
                File.Delete(m_strCurrentDir + m_customDir + decideLang);
            }
            using (StreamWriter writetext = new StreamWriter(m_strCurrentDir + m_customDir + decideLang))
            {
                if(langstring == "zh-CN2")
                {
                    writetext.WriteLine("zh-CN");
                }
                else
                {
                    writetext.WriteLine(langstring);
                }
            }

            if(langstring == "en")
            {
                partyTL(1);
            }
            else if(langstring == "zh-CN")
            {
                partyTL(2);
            }
            else if (langstring == "zh-CN2")
            {
                partyTL(3);
            }
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            System.Windows.Forms.Application.Restart();
        }

        private void modeDev_Checked(object sender, RoutedEventArgs e)
        {
            using (StreamWriter writetext = new StreamWriter(m_strCurrentDir + m_customDir + "/devMode"))
            {
                writetext.WriteLine("devmode: True");
            }
            if (!startup)
            {
                devMode(true);
            }
        }

        private void modeDev_Unchecked(object sender, RoutedEventArgs e)
        {
            devMode(false);
            if (DevExists)
            {
                File.Delete(m_strCurrentDir + m_customDir + "/devMode");
            }
            if (!startup)
            {
                devMode(false);
            }
        }

        private void font_Checked(object sender, RoutedEventArgs e)
        {
            if(File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\1\\font.unity3d"))
            {
                File.Move(m_strCurrentDir + "abdata\\localize\\translate\\1\\font.unity3d", m_strCurrentDir + "abdata\\localize\\translate\\1\\font.unit-y3d");
            }
            if (File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\2\\font.unity3d"))
            {
                File.Move(m_strCurrentDir + "abdata\\localize\\translate\\2\\font.unity3d", m_strCurrentDir + "abdata\\localize\\translate\\2\\font.unit-y3d");
            }
            if (File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\3\\font.unity3d"))
            {
                File.Move(m_strCurrentDir + "abdata\\localize\\translate\\3\\font.unity3d", m_strCurrentDir + "abdata\\localize\\translate\\3\\font.unit-y3d");
            }
        }

        private void font_Unchecked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\1\\font.unit-y3d"))
            {
                File.Move(m_strCurrentDir + "abdata\\localize\\translate\\1\\font.unity3d", m_strCurrentDir + "abdata\\localize\\translate\\1\\font.unity3d");
            }
            if (File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\2\\font.unit-y3d"))
            {
                File.Move(m_strCurrentDir + "abdata\\localize\\translate\\2\\font.unity3d", m_strCurrentDir + "abdata\\localize\\translate\\2\\font.unity3d");
            }
            if (File.Exists(m_strCurrentDir + "abdata\\localize\\translate\\3\\font.unit-y3d"))
            {
                File.Move(m_strCurrentDir + "abdata\\localize\\translate\\3\\font.unity3d", m_strCurrentDir + "abdata\\localize\\translate\\3\\font.unity3d");
            }
        }

        void devMode(bool setDev)
        {
            var ud = Path.Combine(m_strCurrentDir, @"BepInEx\config\BepInEx.cfg");

            try
            {
                var contents = File.ReadAllLines(ud).ToList();

                var setToLanguage = contents.FindIndex(s => s.ToLower().Contains("[Logging.Console]".ToLower()));
                if (setToLanguage >= 0 && setDev)
                {
                    var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Enabled"));
                    if (i > setToLanguage)
                        contents[i] = $"Enabled = true";
                }
                else
                {
                    var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Enabled"));
                    if (i > setToLanguage)
                        contents[i] = $"Enabled = false";
                }

                File.WriteAllLines(ud, contents.ToArray());
            }
            catch (Exception e)
            {
                MessageBox.Show("Something went wrong: " + e);
            }
        }
        void partyTL(int x)
        {
            m_Setting.m_nLangChoose = x;
        }
    }
}