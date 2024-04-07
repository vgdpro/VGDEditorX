﻿/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 5月18 星期日
 * 时间: 20:22
 * 
 */
using DataEditorX.Common;
using DataEditorX.Config;
using DataEditorX.Core;
using DataEditorX.Core.Mse;
using DataEditorX.Language;
using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using static DataEditorX.ResetForm;

namespace DataEditorX
{
    public partial class DataEditForm : DockContent, IDataForm
    {
        private string addrequire_str;

        public string Addrequire
        {
            get
            {
                if (!string.IsNullOrEmpty(this.addrequire_str))
                {
                    return this.addrequire_str;
                }
                else if (CheckOpen())
                {
                    FileInfo fi = new FileInfo(this.nowCdbFile);
                    string lua = $"{fi.DirectoryName}\\{Path.GetFileNameWithoutExtension(this.nowCdbFile)}.lua";
                    if (File.Exists(lua))
                    {
                        return lua;
                    }
                }
                return "";
            }
            set
            {
                this.addrequire_str = value;
            }
        }

        #region 成员变量/构造
        TaskHelper tasker = null;
        string taskname;
        //目录
        YgoPath ygopath;
        /// <summary>当前卡片</summary>
        Card oldCard = new Card(0);
        /// <summary>搜索条件</summary>
        Card srcCard = new Card(0);
        //卡片编辑
        CardEdit cardedit;
        string[] strs = null;
        /// <summary>
        /// 对比的id集合
        /// </summary>
        List<string> tmpCodes;
        //初始标题
        string title;
        string nowCdbFile = "";
        int maxRow = 20;
        int page = 1, pageNum = 1;
        /// <summary>
        /// 卡片总数
        /// </summary>
        int cardcount;

        /// <summary>
        /// 搜索结果
        /// </summary>
        readonly List<Card> cardlist = new List<Card>();

        //setcode正在输入
        readonly bool[] setcodeIsedit = new bool[5];
        readonly CommandManager cmdManager = new CommandManager();

        Image cover;
        MSEConfig msecfg;

        string datapath, confcover;

        public DataEditForm(string datapath, string cdbfile)
        {
            this.Initialize(datapath);
            this.nowCdbFile = cdbfile;
        }

        public DataEditForm(string datapath)
        {
            this.Initialize(datapath);
        }
        public DataEditForm()
        {//默认启动
            string dir = DEXConfig.ReadString(DEXConfig.TAG_DATA);
            if (string.IsNullOrEmpty(dir))
            {
                Application.Exit();
            }
            this.datapath = MyPath.Combine(Application.StartupPath, dir);

            this.Initialize(this.datapath);
        }
        void Initialize(string datapath)
        {
            this.cardedit = new CardEdit(this);
            this.tmpCodes = new List<string>();
            this.ygopath = new YgoPath(Application.StartupPath);
            this.InitPath(datapath);
            this.InitializeComponent();
            this.title = this.Text;
            this.nowCdbFile = "";
            this.cmdManager.UndoStateChanged += delegate (bool val)
            {
                if (val)
                {
                    this.btn_undo.Enabled = true;
                }
                else
                {
                    this.btn_undo.Enabled = false;
                }
            };
        }

        #endregion

        #region 接口
        public void SetActived()
        {
            this.Activate();
        }
        public string GetOpenFile()
        {
            return this.nowCdbFile;
        }
        public bool CanOpen(string file)
        {
            return YGOUtil.IsDataBase(file);
        }
        public bool Create(string file)
        {
            return this.Open(file);
        }
        public bool Save()
        {
            return true;
        }
        #endregion

        #region 窗体
        //窗体第一次加载
        void DataEditFormLoad(object sender, EventArgs e)
        {
            //InitListRows();//调整卡片列表的函数
            this.HideMenu();//是否需要隐藏菜单
            this.SetTitle();//设置标题
                            //加载
            this.msecfg = new MSEConfig(this.datapath);
            this.tasker = new TaskHelper(this.datapath, this.bgWorker1, this.msecfg);
            //设置空白卡片
            this.oldCard = new Card(0);
            this.SetCard(this.oldCard);
            //删除资源
            this.menuitem_operacardsfile.Checked = DEXConfig.ReadBoolean(DEXConfig.TAG_DELETE_WITH);
            //用CodeEditor打开脚本
            this.menuitem_openfileinthis.Checked = DEXConfig.ReadBoolean(DEXConfig.TAG_OPEN_IN_THIS);
            //自动检查更新
            this.menuitem_autocheckupdate.Checked = DEXConfig.ReadBoolean(DEXConfig.TAG_AUTO_CHECK_UPDATE);
            //add require automatically
            this.Addrequire = DEXConfig.ReadString(DEXConfig.TAG_ADD_REQUIRE_STRING);
            this.menuitem_addrequire.Checked = DEXConfig.ReadBoolean(DEXConfig.TAG_ADD_REQUIRE);
            if (this.nowCdbFile != null && File.Exists(this.nowCdbFile))
            {
                this.Open(this.nowCdbFile);
            }
            //获取MSE配菜单
            this.AddMenuItemFormMSE();
            //
            this.GetLanguageItem();
            //   CheckUpdate(false);//检查更新
        }
        //窗体关闭
        void DataEditFormFormClosing(object sender, FormClosingEventArgs e)
        {
            //当前有任务执行，是否结束
            if (this.tasker != null && this.tasker.IsRuning())
            {
                if (!this.CancelTask())
                {
                    e.Cancel = true;
                    return;
                }
            }
        }
        //窗体激活
        void DataEditFormEnter(object sender, EventArgs e)
        {
            this.SetTitle();
        }
        #endregion

        #region 初始化设置
        //隐藏菜单
        void HideMenu()
        {
            if (this.MdiParent == null)
            {
                return;
            }

            this.mainMenu.Visible = false;
            this.menuitem_file.Visible = false;
            this.menuitem_file.Enabled = false;
            //this.SuspendLayout();
            this.ResumeLayout(true);
            foreach (Control c in this.Controls)
            {
                if (c.GetType() == typeof(MenuStrip))
                {
                    continue;
                }

                Point p = c.Location;
                c.Location = new Point(p.X, p.Y - 25);
            }
            this.ResumeLayout(false);
            //this.PerformLayout();
        }
        //移除Tag
        string RemoveTag(string text)
        {
            int t = text.LastIndexOf(" (");
            if (t > 0)
            {
                return text.Substring(0, t);
            }
            return text;
        }
        //设置标题
        void SetTitle()
        {
            string str = this.title;
            string str2 = this.RemoveTag(this.title);
            if (!string.IsNullOrEmpty(this.nowCdbFile))
            {
                str = this.nowCdbFile + "-" + str;
                str2 = Path.GetFileName(this.nowCdbFile);
            }
            if (this.MdiParent != null) //父容器不为空
            {
                this.Text = str2;
                if (this.tasker != null && this.tasker.IsRuning())
                {
                    if (this.DockPanel.ActiveContent == this)
                    {
                        this.MdiParent.Text = str;
                    }
                }
                else
                {
                    this.MdiParent.Text = str;
                }
            }
            else
            {
                this.Text = str;
            }
        }
        //按cdb路径设置目录
        void SetCDB(string cdb)
        {
            this.nowCdbFile = cdb;
            this.SetTitle();
            string path = Application.StartupPath;
            if (cdb.Length > 0)
            {
                path = Path.GetDirectoryName(cdb);
            }
            this.ygopath.SetPath(path);
        }
        //初始化文件路径
        void InitPath(string datapath)
        {
            this.datapath = datapath;
            this.confcover = MyPath.Combine(datapath, "cover.jpg");
            if (File.Exists(this.confcover))
            {
                this.cover = MyBitmap.ReadImage(this.confcover);
            }
            else
            {
                this.cover = null;
            }
        }
        #endregion
        DataConfig dataConfig;
        #region 界面控件
        //初始化控件
        public void InitControl(DataConfig datacfg)
        {
            if (datacfg == null)
            {
                return;
            }

            dataConfig = datacfg;

            List<long> setcodes = DataManager.GetKeys(datacfg.dicSetnames);
            string[] setnames = DataManager.GetValues(datacfg.dicSetnames);
            try
            {
                this.InitComboBox(this.cb_cardrace, datacfg.dicCardRaces);
                this.InitComboBox(this.cb_cardattribute, datacfg.dicCardAttributes);
                this.InitComboBox(this.cb_cardrule, datacfg.dicCardRules);
                this.InitComboBox(this.cb_cardlevel, datacfg.dicCardLevels);
                this.InitComboBox(this.cb_cardMainCountry, datacfg.dicCardMainCountry);
                this.InitCheckPanel(this.pl_cardtype, datacfg.dicCardTypes);
                this.InitCheckPanel(this.pl_markers, datacfg.dicLinkMarkers);
                this.InitCheckPanel(this.pl_category, datacfg.dicCardcategorys);
                for(int i = 0; i < filtered.Length; i++)
                {
                    filtered[i] = false;
                }
                this.InitSetCode(this.cb_setname1, datacfg.dicSetnames);
                this.InitSetCode(this.cb_setname2, datacfg.dicSetnames);
                this.InitSetCode(this.cb_setname3, datacfg.dicSetnames);
                this.InitSetCode(this.cb_setname4, datacfg.dicSetnames);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "启动错误");
            }
        }
        //初始化FlowLayoutPanel
        void InitCheckPanel(FlowLayoutPanel fpanel, Dictionary<long, string> dic)
        {
            fpanel.SuspendLayout();
            fpanel.Controls.Clear();
            foreach (long key in dic.Keys)
            {
                string value = dic[key];
                if (value != null && value.StartsWith("NULL"))
                {
                    Label lab = new Label();
                    string[] sizes = value.Split(',');
                    if (sizes.Length >= 3)
                    {
                        lab.Size = new Size(int.Parse(sizes[1]), int.Parse(sizes[2]));
                    }
                    lab.AutoSize = false;
                    lab.Margin = fpanel.Margin;
                    fpanel.Controls.Add(lab);
                }
                else
                {
                    CheckBox _cbox = new CheckBox
                    {
                        //_cbox.Name = fpanel.Name + key.ToString("x");
                        Tag = key,//绑定值
                        Text = value,
                        AutoSize = true,
                        Margin = fpanel.Margin
                    };
                    //_cbox.Click += PanelOnCheckClick;
                    fpanel.Controls.Add(_cbox);
                }
            }
            fpanel.ResumeLayout(false);
            fpanel.PerformLayout();
        }
        class SetCodeItem
        {
            public long Key;
            public string Name;

            public SetCodeItem(long key, string name)
            {
                Key = key;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
                /*
                if (Name == "自定义")
                {
                    return "自定义";
                }
                return $"{Name}(0x{Key:x})";
                */
            }
        }
        //初始化ComboBox
        void InitSetCode(ComboBox cb, Dictionary<long, string> tempdic)
        {
            int index = int.Parse(cb.Name.Substring(cb.Name.Length - 1));
            setcodeIsedit[index] = true;
            cb.Items.Clear();
            foreach (var kv in tempdic)
            {
                SetCodeItem sci = new SetCodeItem(kv.Key, kv.Value);
                cb.Items.Add(sci);
            }
            setcodeIsedit[index] = false;
            if (cb.Items.Count > 0)
            {
                cb.SelectedIndex = 0;
            }
        }
        //初始化ComboBox
        void InitComboBox(ComboBox cb, Dictionary<long, string> tempdic)
        {
            this.InitComboBox(cb, DataManager.GetKeys(tempdic),
                         DataManager.GetValues(tempdic));
        }
        //初始化ComboBox
        void InitComboBox(ComboBox cb, List<long> keys, string[] values)
        {
            cb.Items.Clear();
            cb.Tag = keys;
            cb.Items.AddRange(values);
            if (cb.Items.Count > 0)
            {
                cb.SelectedIndex = 0;
            }
        }
        //计算list最大行数
        void InitListRows()
        {
            bool addTest = this.lv_cardlist.Items.Count == 0;
            if (addTest)
            {
                ListViewItem item = new ListViewItem
                {
                    Text = "Test"
                };
                this.lv_cardlist.Items.Add(item);
            }
            int headH = this.lv_cardlist.Items[0].GetBounds(ItemBoundsPortion.ItemOnly).Y;
            int itemH = this.lv_cardlist.Items[0].GetBounds(ItemBoundsPortion.ItemOnly).Height;
            if (itemH > 0)
            {
                int n = (this.lv_cardlist.Height - headH) / itemH;
                if (n > 0)
                {
                    this.maxRow = n;
                }
                //MessageBox.Show("height="+lv_cardlist.Height+",item="+itemH+",head="+headH+",max="+MaxRow);
            }
            if (addTest)
            {
                this.lv_cardlist.Items.Clear();
            }
            if (this.maxRow < 10)
            {
                this.maxRow = 20;
            }
        }
        //设置checkbox
        void SetCheck(FlowLayoutPanel fpl, long number)
        {
            long temp;
            //string strType = "";
            foreach (Control c in fpl.Controls)
            {
                if (c is CheckBox cbox)
                {
                    if (cbox.Tag == null)
                    {
                        temp = 0;
                    }
                    else
                    {
                        temp = (long)cbox.Tag;
                    }

                    if ((temp & number) == temp && temp != 0)
                    {
                        cbox.Checked = true;
                        //strType += "/" + c.Text;
                    }
                    else
                    {
                        cbox.Checked = false;
                    }
                }
            }
            //return strType;
        }
        void SetEnabled(FlowLayoutPanel fpl, bool set)
        {
            foreach (Control c in fpl.Controls)
            {
                if (c is CheckBox cbox)
                {
                    cbox.Enabled = set;
                }
            }
        }
        //设置combobox
        void SetSelect(ComboBox cb, long k)
        {
            if (cb.Tag == null)
            {
                cb.SelectedIndex = 0;
                return;
            }
            List<long> keys = (List<long>)cb.Tag;
            int index = keys.IndexOf(k);
            if (index >= 0 && index < cb.Items.Count)
            {
                cb.SelectedIndex = index;
            }
        }
        //得到所选值
        long GetSelect(ComboBox cb)
        {
            if (cb.Tag == null)
            {
                return 0;
            }
            List<long> keys = (List<long>)cb.Tag;
            int index = cb.SelectedIndex;
            if (index >= keys.Count || index < 0)
            {
                return 0;
            }
            else
            {
                return keys[index];
            }
        }
        //得到checkbox的总值
        long GetCheck(FlowLayoutPanel fpl)
        {
            long number = 0;
            long temp;
            foreach (Control c in fpl.Controls)
            {
                if (c is CheckBox cbox)
                {
                    if (cbox.Tag == null)
                    {
                        temp = 0;
                    }
                    else
                    {
                        temp = (long)cbox.Tag;
                    }

                    if (cbox.Checked)
                    {
                        number += temp;
                    }
                }
            }
            return number;
        }
        //添加列表行
        void AddListView(int p)
        {
            int i, j, istart, iend;

            if (p <= 0)
            {
                p = 1;
            }
            else if (p >= this.pageNum)
            {
                p = this.pageNum;
            }

            istart = (p - 1) * this.maxRow;
            iend = p * this.maxRow;
            if (iend > this.cardcount)
            {
                iend = this.cardcount;
            }

            this.page = p;
            this.lv_cardlist.BeginUpdate();
            this.lv_cardlist.Items.Clear();
            if ((iend - istart) > 0)
            {
                ListViewItem[] items = new ListViewItem[iend - istart];
                Card mcard;
                for (i = istart, j = 0; i < iend; i++, j++)
                {
                    mcard = this.cardlist[i];
                    items[j] = new ListViewItem
                    {
                        Tag = i,
                        Text = mcard.id.ToString()
                    };
                    if (mcard.id == this.oldCard.id)
                    {
                        items[j].Checked = true;
                    }

                    if (i % 2 == 0)
                    {
                        items[j].BackColor = Color.GhostWhite;
                    }
                    else
                    {
                        items[j].BackColor = Color.White;
                    }

                    items[j].SubItems.Add(mcard.name);
                }
                this.lv_cardlist.Items.AddRange(items);
            }
            this.lv_cardlist.EndUpdate();
            this.tb_page.Text = this.page.ToString();

        }
        #endregion

        #region 设置卡片
        public YgoPath GetPath()
        {
            return this.ygopath;
        }
        public Card GetOldCard()
        {
            return this.oldCard;
        }

        private void setLinkMarks(long mark, bool setCheck = false)
        {
            if (setCheck)
            {
                this.SetCheck(this.pl_markers, mark);
            }
            this.tb_link.Text = Convert.ToString(mark, 2).PadLeft(9, '0');
        }

        public void SetCard(Card c)
        {
            this.oldCard = c;

            this.tb_cardname.Text = c.name;
            this.tb_cardtext.Text = c.desc;

            this.strs = new string[c.Str.Length];
            Array.Copy(c.Str, this.strs, Card.STR_MAX);
            this.lb_scripttext.Items.Clear();
            this.lb_scripttext.Items.AddRange(c.Str);
            this.tb_edittext.Text = "";
            //data
            this.SetSelect(this.cb_cardrule, c.ot);
            this.SetSelect(this.cb_cardattribute, c.attribute);
            this.SetSelect(this.cb_cardlevel, (c.level & 0xff));
            this.SetSelect(this.cb_cardrace, c.race);
            //setcode
            long[] setcodes = c.GetSetCode();
            this.tb_setcode1.Text = setcodes[0].ToString("x");
            this.tb_setcode2.Text = setcodes[1].ToString("x");
            this.tb_setcode3.Text = setcodes[2].ToString("x");
            this.tb_setcode4.Text = setcodes[3].ToString("x");
            //type,category
            this.SetCheck(this.pl_cardtype, c.type);
            if (c.IsType(Core.Info.CardType.TYPE_LINK))
            {
                this.setLinkMarks(c.def, true);
            }
            else
            {
                this.tb_link.Text = "";
                this.SetCheck(this.pl_markers, 0);
            }
            this.SetCheck(this.pl_category, c.category);
            //Pendulum
            this.tb_pleft.Text = ((c.level >> 24) & 0xff).ToString();
            this.tb_pright.Text = ((c.level >> 16) & 0xff).ToString();
            //atk，def
            this.tb_atk.Text = (c.atk < 0) ? "?" : c.atk.ToString();
            if (c.IsType(Core.Info.CardType.TYPE_LINK))
            {
                this.tb_def.Text = "0";
            }
            else
            {
                this.tb_def.Text = (c.def < 0) ? "?" : c.def.ToString();
            }

            Dictionary<long, int> country_indexer = new Dictionary<long, int>();
            country_indexer[0] = 0;
            country_indexer[1] = 1;
            country_indexer[2] = 2;
            country_indexer[4] = 3;
            country_indexer[8] = 4;
            country_indexer[10] = 5;
            country_indexer[16] = 6;
            country_indexer[20] = 7;
            country_indexer[24] = 8;
            country_indexer[32] = 9;
            country_indexer[34] = 10;
            country_indexer[36] = 11;
            country_indexer[64] = 12;
            country_indexer[128] = 13;
            country_indexer[256] = 14;
            country_indexer[512] = 15;
            country_indexer[1024] = 16;
            country_indexer[2048] = 17;

            this.cb_cardMainCountry.SelectedIndex = country_indexer[c.country % 4096];
            this.cb_CardSecondCountry.SelectedIndex = (int)c.country >> 12;

            this.tb_cardcode.Text = c.id.ToString();
            this.tb_cardalias.Text = c.alias.ToString();
            this.SetImage(c.id.ToString());
        }
        #endregion

        #region 获取卡片
        public Card GetCard()
        {
            Card c = new Card(0)
            {
                name = this.tb_cardname.Text,
                desc = this.tb_cardtext.Text
            };

            Array.Copy(this.strs, c.Str, Card.STR_MAX);

            c.ot = (int)this.GetSelect(this.cb_cardrule);
            c.attribute = (int)this.GetSelect(this.cb_cardattribute);
            c.level = (int)this.GetSelect(this.cb_cardlevel);
            c.race = (int)this.GetSelect(this.cb_cardrace);
            //系列
            c.SetSetCode(
                this.tb_setcode1.Text,
                this.tb_setcode2.Text,
                this.tb_setcode3.Text,
                this.tb_setcode4.Text);

            c.type = this.GetCheck(this.pl_cardtype);
            c.category = this.GetCheck(this.pl_category);

            int.TryParse(this.tb_pleft.Text, out int temp);
            c.level += (temp << 24);
            int.TryParse(this.tb_pright.Text, out temp);
            c.level += (temp << 16);
            if (this.tb_atk.Text == "?" || this.tb_atk.Text == "？")
            {
                c.atk = -2;
            }
            else if (this.tb_atk.Text == ".")
            {
                c.atk = -1;
            }
            else
            {
                int.TryParse(this.tb_atk.Text, out c.atk);
            }

            if (c.IsType(Core.Info.CardType.TYPE_LINK))
            {
                c.def = (int)this.GetCheck(this.pl_markers);
            }
            else
            {
                if (this.tb_def.Text == "?" || this.tb_def.Text == "？")
                {
                    c.def = -2;
                }
                else if (this.tb_def.Text == ".")
                {
                    c.def = -1;
                }
                else
                {
                    int.TryParse(this.tb_def.Text, out c.def);
                }
            }
            long.TryParse(this.tb_cardcode.Text, out c.id);
            long.TryParse(this.tb_cardalias.Text, out c.alias);

            Dictionary<long, int> country_indexer = new Dictionary<long, int>();
            country_indexer[0] = 0;
            country_indexer[1] = 1;
            country_indexer[2] = 2;
            country_indexer[3] = 4;
            country_indexer[4] = 8;
            country_indexer[5] = 10;
            country_indexer[6] = 16;
            country_indexer[7] = 20;
            country_indexer[8] = 24;
            country_indexer[9] = 32;
            country_indexer[10] = 34;
            country_indexer[11] = 36;
            country_indexer[12] = 64;
            country_indexer[13] = 128;
            country_indexer[14] = 256;
            country_indexer[15] = 512;
            country_indexer[16] = 1024;
            country_indexer[17] = 2048;
            c.country = country_indexer[cb_cardMainCountry.SelectedIndex];
            Dictionary<string, int> second_country_indexer = new Dictionary<string, int>();
            second_country_indexer["无集团"] = 0;
            second_country_indexer["光辉骑士团"] = 0x1000;
            second_country_indexer["占卜魔法团"] = 0x2000;
            second_country_indexer["天使之羽"] = 0x3000;
            second_country_indexer["暗影骑士团"] = 0x4000;
            second_country_indexer["黄金骑士团"] = 0x5000;
            second_country_indexer["创世"] = 0x6000;
            second_country_indexer["阳炎"] = 0x1000;
            second_country_indexer["射干玉"] = 0x2000;
            second_country_indexer["太刀风"] = 0x3000;
            second_country_indexer["丛云"] = 0x4000;
            second_country_indexer["鸣神"] = 0x5000;
            second_country_indexer["搏击新星"] = 0x1000;
            second_country_indexer["次元警察"] = 0x2000;
            second_country_indexer["链环傀儡"] = 0x3000;
            second_country_indexer["钢钉兄弟会"] = 0x1000;
            second_country_indexer["黑暗不法者"] = 0x2000;
            second_country_indexer["黯月"] = 0x3000;
            second_country_indexer["齿轮编年史"] = 0x4000;
            second_country_indexer["百群"] = 0x1000;
            second_country_indexer["大自然"] = 0x2000;
            second_country_indexer["永生蜜酒"] = 0x3000;
            second_country_indexer["雄伟深蓝"] = 0x4000;
            second_country_indexer["苍海军势"] = 0x5000;
            second_country_indexer["百慕大三角"] = 0x1000;
            c.country += second_country_indexer[cb_CardSecondCountry.Text];

            return c;
        }
        #endregion

        #region 卡片列表
        //列表选择
        void Lv_cardlistSelectedIndexChanged(object sender, EventArgs e)
        {
            if (filtered[0])
            {
                filtered[0] = false;
                InitSetCode(cb_setname1, dataConfig.dicSetnames);
            }
            if (filtered[1])
            {
                filtered[1] = false;
                InitSetCode(cb_setname2, dataConfig.dicSetnames);
            }
            if (filtered[2])
            {
                filtered[2] = false;
                InitSetCode(cb_setname3, dataConfig.dicSetnames);
            }
            if (filtered[3])
            {
                filtered[3] = false;
                InitSetCode(cb_setname4, dataConfig.dicSetnames);
            }
            if (this.lv_cardlist.SelectedItems.Count > 0)
            {
                int sel = this.lv_cardlist.SelectedItems[0].Index;
                int index = (this.page - 1) * this.maxRow + sel;
                if (index < this.cardlist.Count)
                {
                    Card c = this.cardlist[index];
                    this.SetCard(c);
                }
            }
        }
        //列表按键
        void Lv_cardlistKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    this.cmdManager.ExcuteCommand(this.cardedit.delCard, this.menuitem_operacardsfile.Checked);
                    break;
                case Keys.Right:
                    this.Btn_PageDownClick(null, null);
                    break;
                case Keys.Left:
                    this.Btn_PageUpClick(null, null);
                    break;
            }
        }
        //上一页
        void Btn_PageUpClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            this.page--;
            this.AddListView(this.page);
        }
        //下一页
        void Btn_PageDownClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            this.page++;
            this.AddListView(this.page);
        }
        //跳转到指定页数
        void Tb_pageKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                int.TryParse(this.tb_page.Text, out int p);
                if (p > 0)
                {
                    this.AddListView(p);
                }
            }
        }
        #endregion

        #region 卡片搜索，打开
        //检查是否打开数据库
        public bool CheckOpen()
        {
            if (File.Exists(this.nowCdbFile))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //打开数据库
        public bool Open(string file)
        {
            this.SetCDB(file);
            if (!File.Exists(file))
            {
                MyMsg.Error(LMSG.FileIsNotExists);
                return false;
            }
            //清空
            this.tmpCodes.Clear();
            this.cardlist.Clear();
            //检查表是否存在
            DataBase.CheckTable(file);
            this.srcCard = new Card();
            this.SetCards(DataBase.Read(file, true, ""), false);

            return true;
        }
        //setcode的搜索
        public bool CardFilter(Card c, Card sc)
        {
            bool res = true;
            if (sc.setcode != 0)
            {
                res &= c.IsSetCode(sc.setcode & 0xffff);
            }

            return res;
        }
        //设置卡片列表的结果
        public void SetCards(Card[] cards, bool isfresh)
        {
            if (cards != null)
            {
                this.cardlist.Clear();
                foreach (Card c in cards)
                {
                    if (this.CardFilter(c, this.srcCard))
                    {
                        this.cardlist.Add(c);
                    }
                }
                this.cardcount = this.cardlist.Count;
                this.pageNum = this.cardcount / this.maxRow;
                if (this.cardcount % this.maxRow > 0)
                {
                    this.pageNum++;
                }
                else if (this.cardcount == 0)
                {
                    this.pageNum = 1;
                }

                this.tb_pagenum.Text = this.pageNum.ToString();

                if (isfresh)//是否跳到之前页数
                {
                    this.AddListView(this.page);
                }
                else
                {
                    this.AddListView(1);
                }
            }
            else
            {//结果为空
                this.cardcount = 0;
                this.page = 1;
                this.pageNum = 1;
                this.tb_page.Text = this.page.ToString();
                this.tb_pagenum.Text = this.pageNum.ToString();
                this.cardlist.Clear();
                this.lv_cardlist.Items.Clear();
                //SetCard(new Card(0));
            }
        }
        //搜索卡片
        public void Search(bool isfresh)
        {
            this.Search(this.srcCard, isfresh);
        }
        void Search(Card c, bool isfresh)
        {
            if (!this.CheckOpen())
            {
                return;
            }
            //如果临时卡片不为空，则更新，这个在搜索的时候清空
            if (this.tmpCodes.Count > 0)
            {
                _ = DataBase.Read(this.nowCdbFile,
                                              true, this.tmpCodes.ToArray());
                this.SetCards(this.getCompCards(), true);
            }
            else
            {
                this.srcCard = c;
                string sql = DataBase.GetSelectSQL(c);
                this.SetCards(DataBase.Read(this.nowCdbFile, true, sql), isfresh);
            }
            if (this.lv_cardlist.Items.Count > 0)
            {
                this.lv_cardlist.SelectedIndices.Clear();
                this.lv_cardlist.SelectedIndices.Add(0);
            }
        }
        //更新临时卡片
        public void Reset()
        {
            this.oldCard = new Card(0);
            this.SetCard(this.oldCard);
            this.InitSetCode(this.cb_setname1, dataConfig.dicSetnames);
            this.InitSetCode(this.cb_setname2, dataConfig.dicSetnames);
            this.InitSetCode(this.cb_setname3, dataConfig.dicSetnames);
            this.InitSetCode(this.cb_setname4, dataConfig.dicSetnames);
            for (int i = 0; i < filtered.Length; i++)
            {
                filtered[i] = false;
            }
            this.tb_setcode1.Text = "0";
            this.tb_setcode2.Text = "0";
            this.tb_setcode3.Text = "0";
            this.tb_setcode4.Text = "0";
        }
        #endregion

        #region 按钮
        //搜索卡片
        void Btn_searchClick(object sender, EventArgs e)
        {
            this.tmpCodes.Clear();//清空临时的结果
            this.Search(this.GetCard(), false);
        }
        //重置卡片
        void Btn_resetClick(object sender, EventArgs e)
        {
            this.Reset();
        }
        //添加
        void Btn_addClick(object sender, EventArgs e)
        {
            if (this.cardedit != null)
            {
                this.cmdManager.ExcuteCommand(this.cardedit.addCard);
            }
        }
        //修改
        void Btn_modClick(object sender, EventArgs e)
        {
            if (this.cardedit != null)
            {
                this.cmdManager.ExcuteCommand(this.cardedit.modCard, this.menuitem_operacardsfile.Checked);
            }
        }
        //打开脚本
        void Btn_luaClick(object sender, EventArgs e)
        {
            if (this.cardedit != null)
            {
                this.cardedit.OpenScript(this.menuitem_openfileinthis.Checked, this.Addrequire);
            }
        }
        //删除
        void Btn_delClick(object sender, EventArgs e)
        {
            if (this.cardedit != null)
            {
                this.cmdManager.ExcuteCommand(this.cardedit.delCard, this.menuitem_operacardsfile.Checked);
            }
        }
        //撤销
        void Btn_undoClick(object sender, EventArgs e)
        {
            if (!MyMsg.Question(LMSG.UndoConfirm))
            {
                return;
            }
            if (this.cardedit != null)
            {
                this.cmdManager.Undo();
                this.Search(true);
            }
        }
        //导入卡图
        void Btn_imgClick(object sender, EventArgs e)
        {
            this.ImportImageFromSelect();
        }
        #endregion

        #region 文本框
        //卡片密码搜索
        void Tb_cardcodeKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                Card c = new Card(0);
                long.TryParse(this.tb_cardcode.Text, out c.id);
                if (c.id > 0)
                {
                    this.tmpCodes.Clear();//清空临时的结果
                    this.Search(c, false);
                }
            }
        }
        //卡片名称搜索、编辑
        void Tb_cardnameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Card c = new Card(0)
                {
                    name = this.tb_cardname.Text
                };
                if (c.name.Length > 0)
                {
                    this.tmpCodes.Clear();//清空临时的结果
                    this.Search(c, false);
                }
            }
            if (e.KeyCode == Keys.R && e.Control)
            {
                this.Btn_resetClick(null, null);
            }
        }
        //卡片描述编辑
        void Setscripttext(string str)
        {
            int index;
            try
            {
                index = this.lb_scripttext.SelectedIndex;
            }
            catch
            {
                index = -1;
                MyMsg.Error(LMSG.NotSelectScriptText);
            }
            if (index >= 0)
            {
                this.strs[index] = str;

                this.lb_scripttext.Items.Clear();
                this.lb_scripttext.Items.AddRange(this.strs);
                this.lb_scripttext.SelectedIndex = index;
            }
        }

        string Getscripttext()
        {
            int index;
            try
            {
                index = this.lb_scripttext.SelectedIndex;
            }
            catch
            {
                index = -1;
                MyMsg.Error(LMSG.NotSelectScriptText);
            }
            if (index >= 0)
            {
                return this.strs[index];
            }
            else
            {
                return "";
            }
        }
        //脚本文本
        void Lb_scripttextSelectedIndexChanged(object sender, EventArgs e)
        {
            this.tb_edittext.Text = this.Getscripttext();
        }

        //脚本文本
        void Tb_edittextTextChanged(object sender, EventArgs e)
        {
            this.Setscripttext(this.tb_edittext.Text);
        }
        #endregion

        #region 帮助菜单
        void Menuitem_aboutClick(object sender, EventArgs e)
        {
            MyMsg.Show(
                LanguageHelper.GetMsg(LMSG.About) + "\t" + Application.ProductName + "\n"
                + LanguageHelper.GetMsg(LMSG.Version) + "\t" + Application.ProductVersion + "\n"
                + LanguageHelper.GetMsg(LMSG.Author) + "\tNanahira & JoyJ");
        }

        void Menuitem_checkupdateClick(object sender, EventArgs e)
        {
            this.CheckUpdate(true);
        }
        public void CheckUpdate(bool showNew)
        {
            if (!this.isRun())
            {
                this.tasker.SetTask(MyTask.CheckUpdate, null, showNew.ToString());
                this.Run(LanguageHelper.GetMsg(LMSG.checkUpdate));
            }
        }
        bool CancelTask()
        {
            bool bl = false;
            if (this.tasker != null && this.tasker.IsRuning())
            {
                bl = MyMsg.Question(LMSG.IfCancelTask);
                if (bl)
                {
                    if (this.tasker != null)
                    {
                        this.tasker.Cancel();
                    }

                    if (this.bgWorker1.IsBusy)
                    {
                        this.bgWorker1.CancelAsync();
                    }
                }
            }
            return bl;
        }
        void Menuitem_cancelTaskClick(object sender, EventArgs e)
        {
            this.CancelTask();
        }
        void Menuitem_githubClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(DEXConfig.ReadString(DEXConfig.TAG_SOURCE_URL));
        }
        #endregion

        #region 文件菜单
        //打开文件
        void Menuitem_openClick(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.Open(dlg.FileName);
                }
            }
        }
        //新建文件
        void Menuitem_newClick(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (DataBase.Create(dlg.FileName))
                    {
                        if (MyMsg.Question(LMSG.IfOpenDataBase))
                        {
                            this.Open(dlg.FileName);
                        }
                    }
                }
            }
        }
        //读取ydk
        void Menuitem_readydkClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.SelectYdkPath);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.ydkType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.tmpCodes.Clear();
                    string[] ids = YGOUtil.ReadYDK(dlg.FileName);
                    this.tmpCodes.AddRange(ids);
                    this.SetCards(DataBase.Read(this.nowCdbFile, true,
                                           ids), false);
                }
            }
        }
        //从图片文件夹读取
        void Menuitem_readimagesClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            using (FolderBrowserDialog fdlg = new FolderBrowserDialog())
            {
                fdlg.Description = LanguageHelper.GetMsg(LMSG.SelectImagePath);
                if (fdlg.ShowDialog() == DialogResult.OK)
                {
                    this.tmpCodes.Clear();
                    string[] ids = YGOUtil.ReadImage(fdlg.SelectedPath);
                    this.tmpCodes.AddRange(ids);
                    this.SetCards(DataBase.Read(this.nowCdbFile, true,
                                           ids), false);
                }
            }
        }
        //关闭
        void Menuitem_quitClick(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region 线程
        //是否在执行
        bool isRun()
        {
            if (this.tasker != null && this.tasker.IsRuning())
            {
                MyMsg.Warning(LMSG.RunError);
                return true;
            }
            return false;
        }
        //执行任务
        void Run(string name)
        {
            if (this.isRun())
            {
                return;
            }

            this.taskname = name;
            this.title = this.title + " (" + this.taskname + ")";
            this.SetTitle();
            this.bgWorker1.RunWorkerAsync();
        }
        //线程任务
        void BgWorker1DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            this.tasker.Run();
        }
        void BgWorker1ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            this.title = string.Format("{0} ({1}-{2})",
                                  this.RemoveTag(this.title),
                                  this.taskname,
                                  // e.ProgressPercentage,
                                  e.UserState);
            this.SetTitle();
        }
        //任务完成
        void BgWorker1RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //还原标题
            int t = this.title.LastIndexOf(" (");
            if (t > 0)
            {
                this.title = this.title.Substring(0, t);
                this.SetTitle();
            }
            if (e.Error != null)
            {//出错
                if (this.tasker != null)
                {
                    this.tasker.Cancel();
                }

                if (this.bgWorker1.IsBusy)
                {
                    this.bgWorker1.CancelAsync();
                }

                MyMsg.Show(LanguageHelper.GetMsg(LMSG.TaskError) + "\n" + e.Error);
            }
            else if (this.tasker.IsCancel() || e.Cancelled)
            {//取消任务
                MyMsg.Show(LMSG.CancelTask);
            }
            else
            {
                MyTask mt = this.tasker.GetLastTask();
                switch (mt)
                {
                    case MyTask.CheckUpdate:
                        break;
                    case MyTask.ExportData:
                        MyMsg.Show(LMSG.ExportDataOK);
                        break;
                    case MyTask.CutImages:
                        MyMsg.Show(LMSG.CutImageOK);
                        break;
                    case MyTask.SaveAsMSE:
                        MyMsg.Show(LMSG.SaveMseOK);
                        break;
                    case MyTask.ConvertImages:
                        MyMsg.Show(LMSG.ConvertImageOK);
                        break;
                    case MyTask.ReadMSE:
                        //保存读取的卡片
                        this.SaveCards(this.tasker.CardList);
                        MyMsg.Show(LMSG.ReadMSEisOK);
                        break;
                }
            }
        }
        #endregion

        #region 复制卡片
        //得到卡片列表，是否是选中的
        public Card[] GetCardList(bool onlyselect)
        {
            if (!this.CheckOpen())
            {
                return null;
            }

            List<Card> cards = new List<Card>();
            if (onlyselect)
            {
                foreach (ListViewItem lvitem in this.lv_cardlist.SelectedItems)
                {
                    int index;
                    if (lvitem.Tag != null)
                    {
                        index = (int)lvitem.Tag;
                    }
                    else
                    {
                        index = lvitem.Index + (this.page - 1) * this.maxRow;
                    }

                    if (index >= 0 && index < this.cardlist.Count)
                    {
                        cards.Add(this.cardlist[index]);
                    }
                }
            }
            else
            {
                cards.AddRange(this.cardlist.ToArray());
            }

            if (cards.Count == 0)
            {
                //MyMsg.Show(LMSG.NoSelectCard);
            }
            return cards.ToArray();
        }
        void Menuitem_copytoClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            this.CopyTo(this.GetCardList(false));
        }

        void Menuitem_copyselecttoClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            this.CopyTo(this.GetCardList(true));
        }
        //保存卡片到当前数据库
        public void SaveCards(Card[] cards)
        {
            this.cmdManager.ExcuteCommand(this.cardedit.copyCard, cards);
            this.Search(this.srcCard, true);
        }
        //卡片另存为
        void CopyTo(Card[] cards)
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }
            //select file
            bool replace = false;
            string filename = null;
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    filename = dlg.FileName;
                    replace = MyMsg.Question(LMSG.IfReplaceExistingCard);
                }
            }
            if (!string.IsNullOrEmpty(filename))
            {
                DataBase.CopyDB(filename, !replace, cards);
                MyMsg.Show(LMSG.CopyCardsToDBIsOK);
            }

        }
        #endregion

        #region MSE存档/裁剪图片
        //裁剪图片
        void Menuitem_cutimagesClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            if (this.isRun())
            {
                return;
            }

            bool isreplace = MyMsg.Question(LMSG.IfReplaceExistingImage);
            this.tasker.SetTask(MyTask.CutImages, this.cardlist.ToArray(),
                           this.ygopath.picpath, isreplace.ToString());
            this.Run(LanguageHelper.GetMsg(LMSG.CutImage));
        }
        void Menuitem_saveasmse_selectClick(object sender, EventArgs e)
        {
            //选择
            this.SaveAsMSE(true);
        }

        void Menuitem_saveasmseClick(object sender, EventArgs e)
        {
            //全部
            this.SaveAsMSE(false);
        }
        void SaveAsMSE(bool onlyselect)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            if (this.isRun())
            {
                return;
            }

            Card[] cards = this.GetCardList(onlyselect);
            if (cards == null)
            {
                return;
            }
            //select save mse-set
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.selectMseset);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.MseType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    bool isUpdate = false;
#if DEBUG
                    isUpdate = MyMsg.Question(LMSG.OnlySet);
#endif
                    this.tasker.SetTask(MyTask.SaveAsMSE, cards,
                                   dlg.FileName, isUpdate.ToString());
                    this.Run(LanguageHelper.GetMsg(LMSG.SaveMse));
                }
            }
        }
        #endregion

        #region 导入卡图
        void ImportImageFromSelect(bool fromClipboard = false)
        {
            string tid = this.tb_cardcode.Text;
            if (tid == "0" || tid.Length == 0)
            {
                return;
            }
            if (fromClipboard && Clipboard.GetImage() != null)
            {
                this.ImportImage(Clipboard.GetImage(), tid);
            }
            else
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Title = LanguageHelper.GetMsg(LMSG.SelectImage) + "-" + this.tb_cardname.Text;
                    try
                    {
                        dlg.Filter = LanguageHelper.GetMsg(LMSG.ImageType);
                    }
                    catch { }
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        //dlg.FileName;
                        this.ImportImage(dlg.FileName, tid);
                    }
                }
            }
        }
        private void pl_image_DoubleClick(object sender, EventArgs e)
        {
            if (ModifierKeys.Equals(Keys.Shift))
            {
                this.ImportImageFromSelect(true);
            }
            else
            {
                this.ImportImageFromSelect();
            }
        }
        void Pl_imageDragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (File.Exists(files[0]))
            {
                this.ImportImage(files[0], this.tb_cardcode.Text);
            }
        }

        void Pl_imageDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link; //重要代码：表明是链接类型的数据，比如文件路径
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void menuitem_importmseimg_Click(object sender, EventArgs e)
        {
            string tid = this.tb_cardcode.Text;
            this.menuitem_importmseimg.Checked = !this.menuitem_importmseimg.Checked;
            this.SetImage(tid);
        }
        void ImportImage(Image image, string tid)
        {
            string file = "temp" + new Random().Next(10000000, 99999999) + ".jpg";
            image.Save(file);
            ImportImage(file, tid);
            File.Delete(file);
        }
        void ImportImage(string file, string tid)
        {
            string f;
            if (this.pl_image.BackgroundImage != null
                && this.pl_image.BackgroundImage != this.cover)
            {//释放图片资源
                this.pl_image.BackgroundImage.Dispose();
                this.pl_image.BackgroundImage = this.cover;
            }
            if (this.menuitem_importmseimg.Checked)
            {
                if (!Directory.Exists(this.tasker.MSEImagePath))
                {
                    Directory.CreateDirectory(this.tasker.MSEImagePath);
                }

                f = MyPath.Combine(this.tasker.MSEImagePath, tid + ".jpg");
                File.Copy(file, f, true);
            }
            else
            {
                //	tasker.ToImg(file, ygopath.GetImage(tid),
                //				 ygopath.GetImageThum(tid));
                this.tasker.ToImg(file, this.ygopath.GetImage(tid));
            }
            this.SetImage(tid);
        }
        public void SetImage(string id)
        {
            long.TryParse(id, out long t);
            this.SetImage(t);
        }
        public void SetImage(long id)
        {
            string pic = this.ygopath.GetImage(id);
            if (this.menuitem_importmseimg.Checked)//显示MSE图片
            {
                string msepic = MseMaker.GetCardImagePath(this.tasker.MSEImagePath, this.oldCard);
                if (File.Exists(msepic))
                {
                    this.pl_image.BackgroundImage = MyBitmap.ReadImage(msepic);
                }
            }
            else if (File.Exists(pic))
            {
                this.pl_image.BackgroundImage = MyBitmap.ReadImage(pic);
            }
            else
            {
                this.pl_image.BackgroundImage = this.cover;
            }
        }
        void Menuitem_convertimageClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            if (this.isRun())
            {
                return;
            }

            using (FolderBrowserDialog fdlg = new FolderBrowserDialog())
            {
                fdlg.Description = LanguageHelper.GetMsg(LMSG.SelectImagePath);
                if (fdlg.ShowDialog() == DialogResult.OK)
                {
                    bool isreplace = MyMsg.Question(LMSG.IfReplaceExistingImage);
                    this.tasker.SetTask(MyTask.ConvertImages, null,
                                   fdlg.SelectedPath, this.ygopath.gamepath, isreplace.ToString());
                    this.Run(LanguageHelper.GetMsg(LMSG.ConvertImage));
                }
            }
        }
        #endregion

        #region 导出数据包
        void Menuitem_exportdataClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            if (this.isRun())
            {
                return;
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.InitialDirectory = this.ygopath.gamepath;
                try
                {
                    dlg.Filter = "Zip|(*.zip|All Files(*.*)|*.*";
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.tasker.SetTask(MyTask.ExportData,
                                   this.GetCardList(false),
                                   this.ygopath.gamepath,
                                   dlg.FileName,
                                   this.GetOpenFile(),
                                   this.Addrequire);
                    this.Run(LanguageHelper.GetMsg(LMSG.ExportData));
                }
            }

        }
        #endregion

        #region 对比数据
        /// <summary>
        /// 数据一致，返回true，不存在和数据不同，则返回false
        /// </summary>
        bool CheckCard(Card[] cards, Card card, bool checkinfo)
        {
            foreach (Card c in cards)
            {
                if (c.id != card.id)
                {
                    continue;
                }
                //data数据不一样
                if (checkinfo)
                {
                    return card.EqualsData(c);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
        //读取将要对比的数据
        Card[] getCompCards()
        {
            if (this.tmpCodes.Count == 0)
            {
                return null;
            }

            if (!this.CheckOpen())
            {
                return null;
            }

            return DataBase.Read(this.nowCdbFile, true, this.tmpCodes.ToArray());
        }
        public void CompareCards(string cdbfile, bool checktext)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            this.tmpCodes.Clear();
            this.srcCard = new Card();
            Card[] mcards = DataBase.Read(this.nowCdbFile, true, "");
            Card[] cards = DataBase.Read(cdbfile, true, "");
            foreach (Card card in mcards)
            {
                if (!this.CheckCard(cards, card, checktext))//添加到id集合
                {
                    this.tmpCodes.Add(card.id.ToString());
                }
            }
            if (this.tmpCodes.Count == 0)
            {
                this.SetCards(null, false);
                return;
            }
            this.SetCards(this.getCompCards(), false);
        }
        #endregion

        #region MSE配置菜单
        //把文件添加到菜单
        void AddMenuItemFormMSE()
        {
            if (!Directory.Exists(this.datapath))
            {
                return;
            }

            this.menuitem_mseconfig.DropDownItems.Clear();//清空
            string[] files = Directory.GetFiles(this.datapath);
            foreach (string file in files)
            {
                string name = MyPath.GetFullFileName(MSEConfig.TAG, file);
                //是否是MSE配置文件
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                //菜单文字是语言
                ToolStripMenuItem tsmi = new ToolStripMenuItem(name)
                {
                    ToolTipText = file//提示文字为真实路径
                };
                tsmi.Click += this.SetMseConfig_Click;
                if (this.msecfg.configName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    tsmi.Checked = true;//如果是当前，则打勾
                }

                this.menuitem_mseconfig.DropDownItems.Add(tsmi);
            }
        }
        void SetMseConfig_Click(object sender, EventArgs e)
        {
            if (this.isRun())//正在执行任务
            {
                return;
            }

            if (sender is ToolStripMenuItem tsmi)
            {
                //读取新的配置
                this.msecfg.SetConfig(tsmi.ToolTipText, this.datapath);
                //刷新菜单
                this.AddMenuItemFormMSE();
                //保存配置
                XMLReader.Save(DEXConfig.TAG_MSE, tsmi.Text);
            }
        }
        #endregion

        #region 查找lua函数
        private void menuitem_findluafunc_Click(object sender, EventArgs e)
        {
            string funtxt = MyPath.Combine(this.datapath, DEXConfig.FILE_FUNCTION);
            using (FolderBrowserDialog fd = new FolderBrowserDialog())
            {
                fd.Description = "Folder Name: ocgcore";
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    LuaFunction.Read(funtxt);//先读取旧函数列表
                    LuaFunction.Find(fd.SelectedPath);//查找新函数，并保存
                    MessageBox.Show("OK");
                }
            }
        }

        #endregion

        #region 系列名textbox
        //系列名输入时
        void setCode_InputText(int index, ComboBox cb, TextBox tb)
        {
            if (index >= 0 && index < this.setcodeIsedit.Length)
            {
                this.setcodeIsedit[index] = true;
                int.TryParse(tb.Text, NumberStyles.HexNumber, null, out int temp);
                //tb.Text = temp.ToString("x");
                if (temp == 0 && (tb.Text != "0" || tb.Text.Length == 0))
                {
                    temp = -1;
                }

                foreach (SetCodeItem sci in cb.Items)
                {
                    if (sci.Key == temp && temp > 0 && temp < 0xffff)
                    {
                        cb.SelectedItem = sci;
                        return;
                    }
                }
                this.setcodeIsedit[index] = false;
            }
        }
        private void tb_setcode1_TextChanged(object sender, EventArgs e)
        {
            this.setCode_InputText(1, this.cb_setname1, this.tb_setcode1);
        }

        private void tb_setcode2_TextChanged(object sender, EventArgs e)
        {
            this.setCode_InputText(2, this.cb_setname2, this.tb_setcode2);
        }

        private void tb_setcode3_TextChanged(object sender, EventArgs e)
        {
            this.setCode_InputText(3, this.cb_setname3, this.tb_setcode3);
        }

        private void tb_setcode4_TextChanged(object sender, EventArgs e)
        {
            this.setCode_InputText(4, this.cb_setname4, this.tb_setcode4);
        }
        #endregion

        #region 系列名comobox
        //系列选择框 选择时
        void setCode_Selected(int index, ComboBox cb, TextBox tb)
        {
            try
            {
                SetCodeItem sci = cb.SelectedItem as SetCodeItem;
                if (sci != null)
                {
                    tb.Text = sci.Key.ToString("x");
                }
            }
            catch { }

        }
        private void cb_setname1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.setCode_Selected(1, this.cb_setname1, this.tb_setcode1);
        }

        private void cb_setname2_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.setCode_Selected(2, this.cb_setname2, this.tb_setcode2);
        }

        private void cb_setname3_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.setCode_Selected(3, this.cb_setname3, this.tb_setcode3);
        }

        private void cb_setname4_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.setCode_Selected(4, this.cb_setname4, this.tb_setcode4);
        }
        #endregion

        #region 读取MSE存档
        private void menuitem_readmse_Click(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            if (this.isRun())
            {
                return;
            }
            //select open mse-set
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.selectMseset);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.MseType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    bool isUpdate = MyMsg.Question(LMSG.IfReplaceExistingImage);
                    this.tasker.SetTask(MyTask.ReadMSE, null,
                                   dlg.FileName, isUpdate.ToString());
                    this.Run(LanguageHelper.GetMsg(LMSG.ReadMSE));
                }
            }
        }
        #endregion

        #region 压缩数据库
        private void menuitem_compdb_Click(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            DataBase.Compression(this.nowCdbFile);
            MyMsg.Show(LMSG.CompDBOK);
        }
        #endregion

        #region 设置
        //删除卡片的时候，是否要删除图片和脚本
        private void menuitem_deletecardsfile_Click(object sender, EventArgs e)
        {
            this.menuitem_operacardsfile.Checked = !this.menuitem_operacardsfile.Checked;
            XMLReader.Save(DEXConfig.TAG_DELETE_WITH, this.menuitem_operacardsfile.Checked.ToString().ToLower());
        }
        //用CodeEditor打开lua
        private void menuitem_openfileinthis_Click(object sender, EventArgs e)
        {
            this.menuitem_openfileinthis.Checked = !this.menuitem_openfileinthis.Checked;
            XMLReader.Save(DEXConfig.TAG_OPEN_IN_THIS, this.menuitem_openfileinthis.Checked.ToString().ToLower());
        }
        //自动检查更新
        private void menuitem_autocheckupdate_Click(object sender, EventArgs e)
        {
            this.menuitem_autocheckupdate.Checked = !this.menuitem_autocheckupdate.Checked;
            XMLReader.Save(DEXConfig.TAG_AUTO_CHECK_UPDATE, this.menuitem_autocheckupdate.Checked.ToString().ToLower());
        }
        //add require automatically
        private void menuitem_addrequire_Click(object sender, EventArgs e)
        {
            this.Addrequire = Microsoft.VisualBasic.Interaction.InputBox(LanguageHelper.GetMsg(LMSG.TemplateFileHint), LanguageHelper.GetMsg(LMSG.About), this.Addrequire);
            this.menuitem_addrequire.Checked = Addrequire != "";
            XMLReader.Save(DEXConfig.TAG_ADD_REQUIRE_STRING, this.Addrequire);
            XMLReader.Save(DEXConfig.TAG_ADD_REQUIRE, this.menuitem_addrequire.Checked ? "true" : "false");
        }
        #endregion

        #region 语言菜单
        void GetLanguageItem()
        {
            if (!Directory.Exists(this.datapath))
            {
                return;
            }

            this.menuitem_language.DropDownItems.Clear();
            string[] files = Directory.GetFiles(this.datapath);
            foreach (string file in files)
            {
                string name = MyPath.GetFullFileName(DEXConfig.TAG_LANGUAGE, file);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                TextInfo txinfo = new CultureInfo(CultureInfo.InstalledUICulture.Name).TextInfo;
                ToolStripMenuItem tsmi = new ToolStripMenuItem(txinfo.ToTitleCase(name))
                {
                    ToolTipText = file
                };
                tsmi.Click += this.SetLanguage_Click;
                if (DEXConfig.ReadString(DEXConfig.TAG_LANGUAGE).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    tsmi.Checked = true;
                }

                this.menuitem_language.DropDownItems.Add(tsmi);
            }
        }
        void SetLanguage_Click(object sender, EventArgs e)
        {
            if (this.isRun())
            {
                return;
            }

            if (sender is ToolStripMenuItem tsmi)
            {
                XMLReader.Save(DEXConfig.TAG_LANGUAGE, tsmi.Text);
                this.GetLanguageItem();
                MyMsg.Show(LMSG.PlzRestart);
            }
        }
        #endregion

        //把mse存档导出为图片
        void Menuitem_exportMSEimageClick(object sender, EventArgs e)
        {
            if (this.isRun())
            {
                return;
            }

            string msepath = MyPath.GetRealPath(DEXConfig.ReadString(DEXConfig.TAG_MSE_PATH));
            if (!File.Exists(msepath))
            {
                MyMsg.Error(LMSG.exportMseImagesErr);
                this.menuitem_exportMSEimage.Checked = false;
                return;
            }
            else
            {
                if (MseMaker.MseIsRunning())
                {
                    MseMaker.MseStop();
                    this.menuitem_exportMSEimage.Checked = false;
                    return;
                }
                else
                {

                }
            }
            //select open mse-set
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.selectMseset);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.MseType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string mseset = dlg.FileName;
                    string exportpath = MyPath.GetRealPath(DEXConfig.ReadString(DEXConfig.TAG_MSE_EXPORT));
                    MseMaker.ExportSet(msepath, mseset, exportpath, delegate
                    {
                        this.menuitem_exportMSEimage.Checked = false;
                    });
                    this.menuitem_exportMSEimage.Checked = true;
                }
                else
                {
                    this.menuitem_exportMSEimage.Checked = false;
                }
            }
        }
        void Menuitem_testPendulumTextClick(object sender, EventArgs e)
        {
            Card c = this.GetCard();
            if (c != null)
            {
                this.tasker.TestPendulumText(c.desc);
            }
        }
        void Menuitem_export_select_sqlClick(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    DataBase.ExportSql(dlg.FileName, this.GetCardList(true));
                    MyMsg.Show("OK");
                }
            }
        }
        void Menuitem_export_all_sqlClick(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    DataBase.ExportSql(dlg.FileName, this.GetCardList(false));
                    MyMsg.Show("OK");
                }
            }
        }
        void Menuitem_autoreturnClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Card[] cards = DataBase.Read(this.nowCdbFile, true, "");
                    int count = cards.Length;
                    if (cards == null || cards.Length == 0)
                    {
                        return;
                    }

                    if (DataBase.Create(dlg.FileName))
                    {
                        //
                        int len = DEXConfig.ReadInteger(DEXConfig.TAG_AUTO_LEN, 30);
                        for (int i = 0; i < count; i++)
                        {
                            if (cards[i].desc != null)
                            {
                                cards[i].desc = StrUtil.AutoEnter(cards[i].desc, len, ' ');
                            }
                        }
                        DataBase.CopyDB(dlg.FileName, false, cards);
                        MyMsg.Show(LMSG.CopyCardsToDBIsOK);
                    }
                }
            }
        }

        void Menuitem_replaceClick(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Card[] cards = DataBase.Read(this.nowCdbFile, true, "");
                    int count = cards.Length;
                    if (cards == null || cards.Length == 0)
                    {
                        return;
                    }

                    if (DataBase.Create(dlg.FileName))
                    {
                        //
                        _ = DEXConfig.ReadInteger(DEXConfig.TAG_AUTO_LEN, 30);
                        for (int i = 0; i < count; i++)
                        {
                            if (cards[i].desc != null)
                            {
                                cards[i].desc = this.tasker.MseHelper.ReplaceText(cards[i].desc, cards[i].name);
                            }
                        }
                        DataBase.CopyDB(dlg.FileName, false, cards);
                        MyMsg.Show(LMSG.CopyCardsToDBIsOK);
                    }
                }
            }
        }

        private void text2LinkMarks(string text)
        {
            try
            {
                long mark = Convert.ToInt64(text, 2);
                this.setLinkMarks(mark, true);
            }
            catch
            {
                //
            }
        }

        void Tb_linkTextChanged(object sender, EventArgs e)
        {
            this.text2LinkMarks(this.tb_link.Text);
        }

        private void DataEditForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                this.tb_cardname.Focus();
                this.tb_cardname.SelectAll();
            }
        }

        private void tb_cardtext_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.R)
            {
                this.Btn_resetClick(null, null);
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.F)
            {
                this.tb_cardname.Focus();
            }
        }

        private void menuitem_language_Click(object sender, EventArgs e)
        {

        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            string[] drops = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> files = new List<string>();
            if (drops == null)
            {
                string file = (string)e.Data.GetData(DataFormats.Text);
                drops = new string[1] { file };
            }
            foreach (string file in drops)
            {
                if (Directory.Exists(file))
                {
                    files.AddRange(Directory.EnumerateFiles(file, "*.cdb", SearchOption.AllDirectories));
                    files.AddRange(Directory.EnumerateFiles(file, "*.lua", SearchOption.AllDirectories));
                }
                else if (File.Exists(file))
                {
                    files.Add(file);
                }
            }
            if (files.Count > 5)
            {
                if (!MyMsg.Question(LMSG.IfOpenLotsOfFile))
                {
                    return;
                }
            }
            foreach (string file in files)
            {
                (this.DockPanel.Parent as MainForm).Open(file);
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void tb_setcode1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchSetCode(tb_setcode1, cb_setname1);
            }
        }

        private void tb_setcode2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchSetCode(tb_setcode2, cb_setname2);
            }
        }

        private void tb_setcode3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchSetCode(tb_setcode3, cb_setname3);
            }
        }

        private void tb_setcode4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchSetCode(tb_setcode4, cb_setname4);
            }
        }

        private void tb_setcode1_Leave(object sender, EventArgs e)
        {
            CheckSetCode(tb_setcode1,cb_setname1);
        }

        private void tb_setcode2_Leave(object sender, EventArgs e)
        {
            CheckSetCode(tb_setcode2, cb_setname2);
        }

        private void CheckSetCode(TextBox tb, ComboBox cb)
        {
            if (tb.Text.ToLower().StartsWith("0x") || tb.Text.Length > 5)
            {
                SearchSetCode(tb, cb);
                return;
            }
            string numbers = "1234567890abcdef";
            for(int i = 0; i < tb.Text.Length; i++)
            {
                if (!numbers.Contains(tb.Text[i].ToString()))
                {
                    SearchSetCode(tb, cb);
                    return;
                }
            }
        }

        private void SearchSetCode(TextBox tb, ComboBox cb)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i].ToString().ToLower() == tb.Text.ToLower())
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i].ToString().ToLower().Contains(tb.Text.ToLower()))
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
            tb.Text = "0";
        }

        private void tb_setcode3_Leave(object sender, EventArgs e)
        {
            CheckSetCode(tb_setcode3, cb_setname3);
        }

        private void tb_setcode4_Leave(object sender, EventArgs e)
        {
            CheckSetCode(tb_setcode4, cb_setname4);
        }

        private void menuMergeDatabase_Click(object sender, EventArgs e)
        {
            if (!this.CheckOpen())
            {
                return;
            }
            List<Card> cards = new List<Card>();
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                    dlg.Multiselect = true;
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    bool replace = MessageBox.Show(LanguageHelper.GetMsg(LMSG.MergeHint), LanguageHelper.GetMsg(LMSG.titleInfo),MessageBoxButtons.YesNo) == DialogResult.Yes;

                    if (File.Exists(this.nowCdbFile))
                    {
                        using (SQLiteConnection mainConnection = new SQLiteConnection(@"Data Source=" + nowCdbFile))
                        {
                            mainConnection.Open();
                            using (SQLiteTransaction trans = mainConnection.BeginTransaction())
                            {
                                using (SQLiteCommand cmd = new SQLiteCommand(mainConnection))
                                {
                                    foreach (string file in dlg.FileNames)
                                    {
                                        //读取失败就跳过
                                        try
                                        {
                                            var dbCards = DataBase.Read(file, true, "");
                                            //单张读取，失败就跳过
                                            foreach (var dbCard in dbCards)
                                            {
                                                try
                                                {
                                                    cards.Add(dbCard);
                                                }
                                                catch { }
                                            }
                                        }
                                        catch { }
                                    }
                                    foreach(Card card in cards)
                                    {
                                        cmd.CommandText = DataBase.GetInsertSQL(card, !replace);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                trans.Commit();
                            }
                            mainConnection.Close();
                        }
                    }
                    MessageBox.Show(string.Format(LanguageHelper.GetMsg(LMSG.MergeComplete), cards.Count), LanguageHelper.GetMsg(LMSG.About));
                    Btn_resetClick(null, null);
                    Btn_searchClick(null, null);
                }
            }
        }

        private void setCode_TextChanged(object sender, EventArgs e)
        {
        }

        private void setName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }
            ComboBox cb = (ComboBox)sender;
            int index = int.Parse(cb.Name.Substring(cb.Name.Length - 1));
            filtered[index - 1] = true;
            Dictionary<long, string> tmpSetname = new Dictionary<long, string>();
            foreach (var kv in dataConfig.dicSetnames)
            {
                if (kv.Value.Contains(cb.Text))
                {
                    tmpSetname.Add(kv.Key, kv.Value);
                }
            }
            InitSetCode(cb, tmpSetname);
        }
        bool[] filtered = new bool[4];
        private void lv_cardlist_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
        }
        private void ChangeSecondCountry(object sender, EventArgs e)
        {
            string text=cb_cardMainCountry.GetItemText(cb_cardMainCountry.SelectedIndex);
            Dictionary<string, string[]> indexer = new Dictionary<string, string[]>();
            indexer["0"] = new string[] { "无集团" };
            indexer["1"] = new string[] { "无集团" };
            indexer["2"] = new string[] { "无集团", "光辉骑士团", "占卜魔法团", "天使之羽", "暗影骑士团", "黄金骑士团", "创世" };
            indexer["3"] = new string[] { "无集团", "阳炎", "射干玉", "太刀风", "丛云", "鸣神" };
            indexer["4"] = new string[] { "无集团", "搏击新星", "次元警察", "链环傀儡" };
            indexer["5"] = new string[] { "无集团" };
            indexer["6"] = new string[] { "无集团", "钢钉兄弟会", "黑暗不法者", "黯月", "齿轮编年史" };
            indexer["7"] = new string[] { "无集团" };
            indexer["8"] = new string[] { "无集团" };
            indexer["9"] = new string[] { "无集团", "百群", "大自然", "永生蜜酒", "雄伟深蓝", "苍海军势" };
            indexer["10"] = new string[] { "无集团" };
            indexer["11"] = new string[] { "无集团" };
            indexer["12"] = new string[] { "无集团", "百慕大三角" };
            indexer["13"] = new string[] { "无集团" };
            indexer["14"] = new string[] { "无集团" };
            indexer["15"] = new string[] { "无集团" };
            indexer["16"] = new string[] { "无集团" };
            indexer["17"] = new string[] { "无集团" };
            cb_CardSecondCountry.Items.Clear();
            cb_CardSecondCountry.Items.AddRange(indexer[text]);
            cb_CardSecondCountry.SelectedIndex = 0;
        }
        void Tb_linkKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '0' && e.KeyChar != '1' && e.KeyChar != 1 && e.KeyChar != 22 && e.KeyChar != 3 && e.KeyChar != 8)
            {
                //				MessageBox.Show("key="+(int)e.KeyChar);
                e.Handled = true;
            }
            else
            {
                this.text2LinkMarks(this.tb_link.Text);
            }
        }
        void DataEditFormSizeChanged(object sender, EventArgs e)
        {
            this.InitListRows();
            this.AddListView(this.page);
            this.tmpCodes.Clear();//清空临时的结果
        }

    }
}
