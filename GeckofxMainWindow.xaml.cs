using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using Gecko;
using System.Windows.Forms.Integration;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using Org.Mentalis.Files;
using System.Windows.Forms;
using Gecko.DOM;
using System.Runtime.InteropServices;

namespace InstaBot {
    public partial class GeckofxMainWindow : Window {
        private Gecko.GeckoWebBrowser webBrowser = null;
        private Task task;
        private CancellationTokenSource ts;
        private volatile int Likes;
        private DateTime Started = DateTime.MinValue;
        private bool clickComments = false;
        public GeckofxMainWindow() {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Unloaded += (s, e) => {
                if (task != null && task.Status == TaskStatus.Running) {
                    if (ts != null) {
                        ts.Cancel();
                    }
                }
            };

            btStart.Click += BtStart_Click;
        }

        private void StatusMessage(string message) {
            Dispatcher.BeginInvoke(new Action(() => {
                btStart.IsEnabled = true;
                btStart.Content = message;
            }));
        }
        private void Log(string message) {
            Dispatcher.BeginInvoke(new Action(() => {
                File.AppendAllText("log.txt", string.Format("{0}# {1}" + Environment.NewLine, DateTime.Now.ToString(), message));
            }));
        }
        private void BtStart_Click(object sender, RoutedEventArgs e) {
            int sleepTime = 12300;
            btStart.IsEnabled = false;
            webBrowser.Focus();
            if (task == null || task.Status == TaskStatus.Canceled || task.Status == TaskStatus.Created || task.Status == TaskStatus.RanToCompletion) {
                ts = new CancellationTokenSource();
                CancellationToken ct = ts.Token;
                task = new Task(() => {
                    try {
                        Likes = 0;
                        Started = DateTime.Now;
                        while (!ct.IsCancellationRequested) {
                            Dispatcher.Invoke(new Action(() => {
                                sleepTime = Int32.Parse(txtSleep.Text);
                            }));
                            var span = DateTime.Now - Started;
                            StatusMessage("Click to STOP | Alive since " + span.ToString() + " | " + Likes + " Likes | " + Math.Round((double)Likes / span.TotalHours, 0) + " per hours");

                            Dispatcher.Invoke(new Action(() => {
                                if (cbLikes.IsChecked.Value)
                                    Like();
                            }));
                            Thread.Sleep(r.Next(500, 1500));
                            Dispatcher.Invoke(new Action(() => {
                                if (cbCommenti.IsChecked.Value)
                                    Comment();
                            }));

                            Thread.Sleep(sleepTime / 2 + r.Next(sleepTime));

                            if (ct.IsCancellationRequested) {
                                break;
                            }
                            //Console.WriteLine("SEND RIGHT =>");
                            //System.Windows.Forms.SendKeys.SendWait("{RIGHT}");
                            Dispatcher.Invoke(new Action(() => {
                                if (cbLikes.IsChecked.Value || cbCommenti.IsChecked.Value)
                                    Next();
                            }));

                            Thread.Sleep(sleepTime / 2);
                            if (ct.IsCancellationRequested) {
                                break;
                            }
                            if (Likes % 10 == 0) {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                            }
                        }

                        StatusMessage("Start");
                    } catch (Exception ex) {
                        StatusMessage(ex.Message);
                        Log(ex.ToString());
                    } finally {
                        var span = DateTime.Now - Started;
                        Log("Alive=" + span.ToString() + " Likes=" + Likes + " PerHours=" + Math.Round((double)Likes / span.TotalHours, 0));
                    }
                }, ct);
                task.Start();
            } else {
                ts.Cancel();
            }
        }
        private string classCuore = "coreSpriteLikeHeartOpen";
        private string classFreccia = "coreSpriteRightPaginationArrow";
        private string classCommento = "?";
        private string classCommentoArea = "Ypffh";
        private List<string> Commenti = new List<string>();
        private Random r;
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            InitializeXpcom();
            webBrowser = new GeckoWebBrowser();
            webBrowser.CreateWindow += (s, ee) => { ee.Cancel = true; };
            webBrowser.DocumentTitleChanged += (s, ee) => { this.Title = webBrowser.DocumentTitle; };
            webBrowser.Navigated += (s, ee) => { tbTitle.Text = webBrowser.Url.ToString(); };
            B.Child = new WindowsFormsHost() { Child = webBrowser };
            webBrowser.Navigate("https://www.instagram.com/explore/tags/dolomiti/");
            webBrowser.Focus();

            var ini = new IniReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
            var section = "Default";
            classCuore = ini.ReadString(section, "ClassCuore", "");
            classFreccia = ini.ReadString(section, "ClassFreccia", "");
            classCommento = ini.ReadString(section, "ClassCommento", "");
            classCommentoArea = ini.ReadString(section, "ClassCommentoTextarea", "");
            txtSleep.Text = ini.ReadString(section, "TempoAttesaMs", "12000");
            Commenti = File.ReadLines("Commenti.txt").ToList();
            r = new Random((int)(DateTime.Now.Ticks) + DateTime.Now.Millisecond);

            tbTitle.PreviewKeyDown += TbTitle_PreviewKeyDown;
            Deactivated += (ss, ee) => {
                clickComments = cbCommenti.IsChecked.HasValue && cbCommenti.IsChecked.Value;
                cbCommenti.IsChecked = false;
            };
            Activated += (ss, ee) => {
                cbCommenti.IsChecked = clickComments;
            };
        }
        private void TbTitle_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                webBrowser.Navigate(tbTitle.Text.Trim());
                e.Handled = true;
            }
        }

        private void Like() {
            var openHearts = webBrowser.DomDocument.GetElementsByClassName(classCuore);
            var node = openHearts.ElementAtOrDefault(1);// openHearts.LastOrDefault();
            if (node != null && node.NodeType == NodeType.Element) {
                var element = (GeckoHtmlElement)node;
                element.Click();
                Likes++;
                if (Debugger.IsAttached)
                    Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " LIKED!!!");
            }
        }
        private void Next() {
            var openHearts = webBrowser.DomDocument.GetElementsByClassName(classFreccia);
            var node = openHearts.FirstOrDefault();
            if (node != null && node.NodeType == NodeType.Element) {
                var element = (GeckoHtmlElement)node;
                element.Click();
                if (Debugger.IsAttached)
                    Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " NEXT");
            }
        }
        private void Comment() {
            string commento = CommentoACaso();
            if (!string.IsNullOrWhiteSpace(commento)) {
                var cmm = GetElement(classCommento);
                if (cmm != null)
                    cmm.Click();
                
                Thread.Sleep(r.Next(300, 600));
                foreach (var c in commento) {
                    Thread.Sleep(r.Next(10, 20));
                    SendKeys.SendWait(c.ToString());
                }
                Thread.Sleep(r.Next(200, 300));
                SendKeys.SendWait("{ENTER}"); 

                

                /*
                     var area = GetElement(classCommentoArea) as GeckoTextAreaElement;
                if (area != null) {
                    Thread.Sleep(r.Next(500, 600));
                    //1area.Click();
                    //Thread.Sleep(r.Next(300, 600));


                    area.Value = commento;
                    //var edit = Process.GetCurrentProcess().MainWindowHandle;
                    //PostMessage(edit, WM_KEYDOWN, (IntPtr)(Keys.A), IntPtr.Zero);

                   

                    Thread.Sleep(r.Next(300, 600));
                    var button = GetElement("sqdOP");
                    if (button != null) {

                        button.Focus();
                       // Thread.Sleep(r.Next(2300, 2600));

                        button.Click();
                    }
                    //Thread.Sleep(2000);

                    var form = GetElement("X7cDz") as GeckoFormElement;
                    if (form != null) {
                        //form.Submit();
                    }
                }*/

                if (Debugger.IsAttached)
                    Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " COMMENTATO: " + commento);

            }
        }


        const uint WM_KEYDOWN = 0x100;
        const uint WM_KEYUP = 0x0101;
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        private string CommentoACaso() {
            var index = r.Next(0, Commenti.Count - 1);
            return Commenti[index];
        }

        private void InitializeXpcom() {
            if (!Xpcom.IsInitialized) {
                string xulrunnerDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Firefox64");
                Xpcom.Initialize(xulrunnerDirectory);
            }
        }


        private GeckoHtmlElement GetElement(string className) {
            var nodes = webBrowser.DomDocument.GetElementsByClassName(className).Where(x => x.NodeType == NodeType.Element);
            var node = nodes.OfType< GeckoHtmlElement>().FirstOrDefault();
            return node;
        }
    }
}