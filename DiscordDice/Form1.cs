using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections.Immutable;
using Quobject.EngineIoClientDotNet.Client.Transports;

namespace DiscordDice
{

    public partial class Form1 : Form
    {
        //Настройки "Коробочек" кубиков
        public int xcoord = 0; //
        public int ycoord = 0; //верхний левый угол "коробочек"
        public int xsize = 200; //
        public int ysize = 200; //размер "коробочек"
        public int downtime = 40; //пауза между цифрами
        public int downtimeadd = 0; //увеличение паузы с каждой цифрой
        public int downtimemultiply = 1; //множитель паузы с каждой цифрой
        public int slowtime = 1000; //время работы
        public int cnt = 3; //количество "коробочек"
        public int target = 2000, width = 600;

        //настройки полосочек
        public int stripxcoord = 0; //
        public int stripycoord = 0; //верхний левый угол "полосочек"
        public int stripxsize = 600; //
        public int stripysize = 30; //размер "полосочек"

        public Socket socket = IO.Socket(new Uri("http://socket.donationalerts.ru/"), new IO.Options() { AutoConnect = true, Port = 3001, Transports = ImmutableList.Create(new string[] { WebSocket.NAME, Polling.NAME }) });

        //костыль для реализации обновления
        private bool isformal = false;

        //текущая группа
        private Group acolytes = new Group();

        //список "коробочек"
        private List<Tuple<PictureBox, Label>> dice = new List<Tuple<PictureBox, Label>>(), sideslist = new List<Tuple<PictureBox, Label>>();


        bool connected = false;
        Bitmap bmp1 = new Bitmap(600, 30), bmp2 = new Bitmap(600, 30);

        public Form1()
        {
            InitializeComponent();
            acolytes.names = new List<Tuple<string, int>>();
            acolytes.name = "";
            acolytes.sides = new List<Tuple<string, int, int>>();
            sideslist.Add(new Tuple<PictureBox, Label>(pictureBox1, label3));
            sideslist.Add(new Tuple<PictureBox, Label>(pictureBox2, label4));
            using (Graphics gfx = Graphics.FromImage(bmp1))
            using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0)))
            {
                gfx.FillRectangle(brush, 0, 0, 600, 30);
            }
            using (Graphics gfx = Graphics.FromImage(bmp2))
            using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(0, 128, 0)))
            {
                gfx.FillRectangle(brush, 0, 0, 600, 30);
            }
            var jobj = new JObject();
            jobj.Add("token", "token");
            jobj.Add("type", "minor");
            //bool connected = false;
            socket.On(Socket.EVENT_CONNECT, () =>
            {
                socket.Emit("add-user", jobj);
                connected = true;
                MessageBox.Show("Connected!");
            });
            //socket.Connect();
            //while (!connected)
           // {
          //      Thread.Sleep(100);
          //      socket.Connect();
          //      socket.Emit("add-user", jobj);
         //       MessageBox.Show("Еще коннект?");
           // //    MessageBox.Show("Я не смогло законнектиться, но я попробую еще раз...");
            //}
            socket.On(Socket.EVENT_CONNECT_ERROR, () => { MessageBox.Show("Коннект чот не коннект, давай перезагрузимся?"); });
            socket.On(Socket.EVENT_RECONNECT_FAILED, () => { MessageBox.Show("РЕконнект чот не РЕконнект, давай перезагрузимся?"); });
            socket.On(Socket.EVENT_CONNECT_TIMEOUT, () => { MessageBox.Show("Коннект чот упал по таймауту, давай перезагрузимся?"); });

            socket.On("donation", (data) =>
            {
                try
                {
                    //var str = Encoding.UTF8.GetString((byte[])data);
                    Donate d = JsonConvert.DeserializeObject<Donate>((string)data);
                    //MessageBox.Show((string)data);
                   // MessageBox.Show(d.username+" ацки башляет "+d.amount+" и говорит: "+d.message);
                    processDonation(d);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            createdice();
            refill();
            update();
            //Animation();
            //using (Graphics gfx = Graphics.FromImage(bmp1))
            //using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(255, 5, 5)))
            //{
            //    gfx.FillRectangle(brush, 0, 0, 600, 20);
            //}
            //using (Graphics gfx = Graphics.FromImage(bmp2))
            //using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(255, 5, 5)))
            //{
            //    gfx.FillRectangle(brush, 0, 0, 600, 20);
            //}
            StreamLabsDotNet.Client.Client client = new StreamLabsDotNet.Client.Client();
            client.Connect("discord secret");
            client.OnError += Client_OnError;
            client.OnDonation += Client_OnDonation;
        }


        private void newDie()
        {
            var picturebox = new PictureBox();
            //picturebox.Hide();
            picturebox.BackgroundImage = Properties.Resources.TMP__2;
            picturebox.BackgroundImageLayout = ImageLayout.Stretch;
            int x = xcoord, y = ycoord;
            if (dice.Count != 0)
            {
                x = dice[dice.Count - 1].Item1.Bounds.X + dice[dice.Count - 1].Item1.Bounds.Width;
                y = dice[dice.Count - 1].Item1.Bounds.Y;
                if (x + dice[dice.Count - 1].Item1.Bounds.X >= this.Size.Width)
                {
                    x = 0;
                    y += dice[dice.Count - 1].Item1.Bounds.Height;
                }
            }
            picturebox.SetBounds(x, y, xsize, ysize);
            var label1 = new Label();
            label1.Parent = picturebox;
            float xx = (float)72 / 200 * xsize, yy = (float)72 / 200 * ysize;
            //MessageBox.Show(xx.ToString());
            label1.Location = new Point((int)xx, (int)yy);
            label1.AutoSize = true;
            label1.BackColor = System.Drawing.Color.Transparent;
            label1.ForeColor = System.Drawing.ColorTranslator.FromHtml("#70ebfd");
            label1.Text = "00";
            label1.Font = new Font("Arial", 30, FontStyle.Bold);
            label1.BringToFront();
            this.Controls.Add(picturebox);
            //this.Controls.Add(label1);
            dice.Add(new Tuple<PictureBox, Label>(picturebox, label1));
        }

        private void update()
        {
            /*int max = -100500, i = 1;
            string maxx = "";
            foreach (var t in acolytes.names)
                if (t.Item2 > max)
                {
                    max = t.Item2;
                    maxx = t.Item1;
                }
            foreach (var t in acolytes.names)
                if (t.Item2 == max && t.Item1 != maxx)
                {
                    maxx += ", " + t.Item1;
                    i++;
                }
            var ttt = "";
            if (i > 3 || i < 1)
                ttt = "временная(?) ничья";
            else ttt = maxx + " с пулом в " + max.ToString() + " руб.";
            Invoke((Action)delegate { label2.Text = ttt; });*/
            //MessageBox.Show("Обновляем полосочки!");
            foreach (var s in acolytes.sides)
                SetProgress(Helper.FLTUC(s.Item1), s.Item2, sideslist[s.Item3].Item2, sideslist[s.Item3].Item1, s.Item1 == acolytes.sides[0].Item1 ? bmp1 : bmp2);
        }

        private void Client_OnError(object sender, string e)
        =>  MessageBox.Show(e);

        public void refill()
        {
            toolStripComboBox1.Items.Clear();
            System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Qubach\\");
            foreach (var s in Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Qubach\\"))
                if (s.IndexOf(".json") > -1 && s.IndexOf(".bck") == -1 && s.IndexOf(".err") == -1)
                    toolStripComboBox1.Items.Add(s.Substring(s.LastIndexOf('\\') + 1, s.Length - s.LastIndexOf('\\') - 5 - 1));
        }

        public async void createdice()
        {
            this.Size = new Size(xcoord + xsize * cnt + 20, ycoord + ysize + 170);
            foreach (var s in dice)
            {
                Controls.Remove(s.Item1);
                Controls.Remove(s.Item2);
            }
            dice.Clear();
            for (int i = 0; i < cnt; i++)
                newDie();

            await MainAsync();
        }

        public async Task MainAsync()
        {
            var client = new DiscordSocketClient();
            client.MessageReceived += MessageReceived;
            string token = "secret"; // Remember to keep this private!
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private void SetLabel1Text(string text, Label label1)
        => Invoke((Action)delegate { label1.Text = text; });

        public void SetProgress(String name, int progress, Label label, PictureBox picture, Bitmap bmp, bool recursing = false)
        {
            int tier = 1 + (progress / 2000), excess = progress % 2000;
            SetLabel1Text(name + " (" + excess.ToString() + "/2000 руб.) " + tier.ToString() + " тир", label);
            int a = (int)(((float)excess / target) * width);
            a = a <= 0 ? 1 : a;
            picture.Image = bmp.Clone(new Rectangle(0, 0, a, 30), System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        }

        private async Task RndNums(int time, int die, Label label, int dienum)
        {
            var r = new Random();            ;
            die = die == dienum ? 0 : die;
            int t = 0, a = 0, tt = downtime;
            string format = "";
            if (dienum != 0)
            {
                try
                {
                    for (int i = 0; i < Math.Log10(dienum); i++)
                        format += "0";
                    for (int i = 0; i < (2 - Math.Log10(dienum)); i++)
                        format = " " + format;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
                while (t < time)
                {
                    a = r.Next(0, dienum);
                    SetLabel1Text(a.ToString(format), label);
                    await Task.Delay(tt);
                    t += tt;
                    tt *= downtimemultiply;
                    tt += downtimeadd;
                }
            }
            SetLabel1Text(die.ToString(format), label);
        }

        private async Task MessageReceived(SocketMessage message)
        {
            Regex a = new Regex("[0-9]{1,3}");
            //Regex b = new Regex("\\([0123456789\\+ ]+\\)");
            Regex c = new Regex("d[0-9]+");
            //Regex d = new Regex("\\[[0123456789, ]+\\]");
            Regex full = new Regex("[\\[(](\\d+[,\\s+]*)+[\\])]");
            if (message.Channel.Name == "stream_dice")
                if ((message.Author.Username == "Sidekick" || message.Author.Username == "rollem"))
                {
                    string content = message.Content.Replace("*", "");
                    int yyy = 0, dienum = Int32.Parse(c.Match(content).Value.Substring(1));
                    if (dienum == 0) return;
                    var tasks = new Task[cnt];
                    //MessageBox.Show(b.Match(content).Value);
                    List<Match> matches = new List<Match>();
                    foreach (Match u in full.Matches(content))
                            foreach (Match uu in a.Matches(u.Value))
                                matches.Add(uu);
                    int shift = dice.Count - matches.Count;
                    for (int i = 1; i <= shift; i++)
                        SetLabel1Text(dice[dice.Count - i - 1].Item2.Text, dice[dice.Count - i].Item2);
                    foreach (Match s in matches)
                    {
                        tasks[yyy] = RndNums(slowtime, Int32.Parse(s.Value), dice[yyy].Item2, dienum);
                        if (++yyy >= cnt) break;
                    }
                    await Task.WhenAll(tasks);
                    return;
                }
        }

        private void Client_OnDonation(object sender, StreamLabsDotNet.Client.Models.StreamlabsEvent<StreamLabsDotNet.Client.Models.DonationMessage> e)
        {
            MessageBox.Show(e.Message[0].From + " вкидывает " + e.Message[0].Amount + " руб. с сообщением: \"" + e.Message[0].Message + "\"");
            foreach (var ss in e.Message)
            {
                Donate d = new Donate();
                d.amount = float.Parse(ss.Amount);
                d.message = ss.Message;
                processDonation(d);
            }

        }

        private void processDonation(Donate d)
        {
            int amount = (int)d.amount;
            var content = d.message;
            MessageBox.Show("Пришел донат на " + amount + " руб. с сообщением: \"" + content + "\"");
            //var t = "";
            Regex donate = new Regex("#[a-zA-ZА-Яа-я~*]+ *");
            //bool name = false, side = false;
            foreach (Match s in donate.Matches(content))
            {
                //MessageBox.Show(s.Value);
                MessageBox.Show("Нашли хэштэг: "+s.Value);
                string search = s.Value.Substring(1).TrimEnd(' ');
                int indexside = acolytes.sides.FindIndex(x => (Helper.LevenshteinDistance(x.Item1.ToLower(), search.ToLower()) < 3));
                //int index = acolytes.names.FindIndex(x => (LevenshteinDistance(x.Item1.ToLower(), search.ToLower()) < 3));
                //if (index > -1 && !name)
                //{
                //    acolytes.names[index] = new Tuple<string, int>(acolytes.names[index].Item1, acolytes.names[index].Item2 + amount);
                //    name = true;
                //    //MessageBox.Show(t);
                //}
                //else
                if (indexside > -1 /*&& !side*/)
                {
                    acolytes.sides[indexside] = new Tuple<string, int, int>(acolytes.sides[indexside].Item1, acolytes.sides[indexside].Item2 + amount, acolytes.sides[indexside].Item3);
                    //side = true;
                    MessageBox.Show("Он принадлежит полосочке " + acolytes.sides[indexside].Item1 + ", поэтому дальше не смотрели.");
                    break;
                    //MessageBox.Show(t);
                }
            }
            //if (name && !side) acolytes.sides[1] = new Tuple<string, int, int>(acolytes.sides[1].Item1, acolytes.sides[1].Item2 + amount, acolytes.sides[1].Item3);
            //Это фундаментальный костыль - донат аколитцу без имени уходил в "помиловать".
            update();
        }

        private void toolStripComboBox1_TextChanged(object sender, EventArgs e)
        {
            if (acolytes.name != "")
                acolytes.Save(acolytes.name + ".json");
            if (!isformal)
                acolytes = Group.Load(toolStripComboBox1.Text + ".json");
            isformal = false;
            update();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            acolytes.Save(acolytes.name + ".json");
        }

        private void всеСломалосьГдеБэкапToolStripMenuItem_Click(object sender, EventArgs e)
        {
            acolytes.Save(acolytes.name + ".json.err");
            acolytes = Group.Load(acolytes.name + ".json.bck");

        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            acolytes.names.Add(new Tuple<string, int>(toolStripTextBox1.Text, 0));
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            if (acolytes.name is null)
                acolytes.Save(acolytes.name + ".json");
            acolytes = new Group();
            acolytes.name = toolStripTextBox4.Text;
            acolytes.names = new List<Tuple<string, int>>();
            acolytes.sides = new List<Tuple<string, int, int>>();
            //MessageBox.Show(acolytes.name);
            toolStripComboBox1.Items.Add(toolStripTextBox4.Text);
            isformal = true;
            toolStripComboBox1.SelectedIndex = toolStripComboBox1.Items.Count - 1;

        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {
                acolytes.sides[0] = new Tuple<string, int, int>(acolytes.sides[0].Item1, Int32.Parse(toolStripTextBox2.Text), acolytes.sides[0].Item3);
                update();
            }
            catch (Exception u)
            {
                MessageBox.Show(u.ToString());
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            try
            {
                acolytes.sides[1] = new Tuple<string, int, int>(acolytes.sides[1].Item1, Int32.Parse(toolStripTextBox3.Text), acolytes.sides[1].Item3);
                update();
            }
            catch (Exception u)
            {
                MessageBox.Show(u.ToString());
            }
        }

        private void обнулитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < acolytes.sides.Count; i++)
                acolytes.sides[i] = new Tuple<string, int, int>(acolytes.sides[i].Item1, 0, acolytes.sides[i].Item3);
            for (int i = 0; i < acolytes.names.Count; i++)
                acolytes.names[i] = new Tuple<string, int>(acolytes.names[i].Item1, 0);
            update();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (acolytes.name != "")
                acolytes.Save(acolytes.name + ".json");
        }

    }

    public class Donate
    {
        public int id;
        public int alert_type;
        public string additional_data;
        public string username;
        public float amount;
        public string amount_formatted;
        public float amount_main;
        public string currency;
        public string message;
        public string date_paid;
        public string emotes;
        public bool _is_test_alert;
    }

    class Group
    {
        public string name;
        public List<Tuple<string, int>> names;
        public List<Tuple<string, int, int>> sides;
        public static Group Load(string str)
        {
            str = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Qubach\\" + str;
            using (StreamReader r = new StreamReader(str))
            {
                string json = r.ReadToEnd();
                Group items = JsonConvert.DeserializeObject<Group>(json);
                return items;
            }
        }

        public void Save(string str)
        {
            str = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Qubach\\" + str;
            if (File.Exists(str))
            {
                File.Delete(str + ".bck");
                File.Copy(str, str + ".bck");
                File.Delete(str);
            }
            using (StreamWriter r = new StreamWriter(str))
            {
                r.Write(JsonConvert.SerializeObject(this));
            }
        }
    }
}
