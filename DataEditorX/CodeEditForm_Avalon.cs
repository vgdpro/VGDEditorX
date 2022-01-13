/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 2014-10-22
 * 时间: 19:16
 * 
 */
using DataEditorX.Config;
using DataEditorX.Controls;
using DataEditorX.Core;
using DataEditorX.Language;
using FastColoredTextBoxNS;
using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace DataEditorX
{
    /// <summary>
    /// Description of CodeEditForm.
    /// </summary>
    public partial class CodeEditForm_Avalon : DockContent, IEditForm
    {
        #region Style
        SortedDictionary<long, string> cardlist;
        #endregion

        #region init 函数提示菜单
        //自动完成
        string nowFile;
        string title = "CodeEditor";
        string oldtext;
        public SortedList<string, string> tooltipDic;
        AutocompleteItem[] items;
        bool tabisspaces = false;
        public CodeEditForm_Avalon()
        {
            InitForm();
        }

        void InitForm()
        {
            cardlist = new SortedDictionary<long, string>();
            tooltipDic = new SortedList<string, string>();
            InitializeComponent();
            editor.ShowLineNumbers = true;
            editor.TextArea.TextEntered += editor_TextArea_TextEntered;
            editor.TextArea.TextEntering += editor_TextArea_TextEntering;
            editor.PreviewKeyDown += TextArea_KeyDown;
            //editor.TextArea.KeyDown += TextArea_KeyDown;
            //editor.KeyDown += TextArea_KeyDown;
            editor.TextChanged += Editor_TextChanged;
            editor.MouseMove += Editor_MouseMove;

            editor.FontFamily = new System.Windows.Media.FontFamily(MyConfig.TAG_FONT_NAME);
            editor.FontSize = MyConfig.ReadFloat(MyConfig.TAG_FONT_SIZE, (float)editor.FontSize);
            editor.WordWrap = MyConfig.ReadBoolean(MyConfig.TAG_WORDWRAP);
            editor.Background = System.Windows.Media.Brushes.Black;
            editor.Foreground = System.Windows.Media.Brushes.GhostWhite;
            this.RefreshHighlighting();
        }

        private void Editor_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var ePos = e.GetPosition(editor);
            var pos = editor.GetPositionFromPoint(ePos);
            if (pos != null)
            {
                int offset = editor.Document.GetOffset(pos.Value.Location);
                int nowOffset = offset;
                string c;
                try
                {
                    c = editor.Document.GetCharAt(nowOffset).ToString();
                }
                catch
                {
                    return;
                }
                if (sep.Contains(c))
                {
                    return;
                }
                while (!sep.Contains(c) && nowOffset > 0)
                {
                    nowOffset--;
                    c = editor.Document.GetCharAt(nowOffset).ToString();
                    if (sep.Contains(c))
                    {
                        break;
                    }
                }
                c = editor.Document.GetCharAt(offset).ToString();
                int maxlen = editor.Document.TextLength - 1;
                while (!sep.Contains(c) && offset < maxlen)
                {
                    offset++;
                    c = editor.Document.GetCharAt(offset).ToString();
                    if (sep.Contains(c))
                    {
                        offset--;
                        break;
                    }
                }
                string find = editor.Document.GetText(nowOffset + 1, offset - nowOffset);
                if (string.IsNullOrWhiteSpace(find))
                {
                    return;
                }
                if (tooltipDic.ContainsKey(find))
                {
                    lbTooltip.Text = find + "\n" + tooltipDic[find];
                    lbTooltip.Location = new Point(Math.Min((int)ePos.X + 800, host.Width - 500), Math.Min((int)ePos.Y, this.Height - lbTooltip.Height - 20));
                }
            }
        }

        int lastCaret = 0;
        private void Editor_TextChanged(object sender, EventArgs e)
        {
            if (completionWindowUse != null && completionWindowUse.CompletionList.SelectedItem != null && editor.CaretOffset != lastCaret)
            {
                completionWindowUse.Close();
            }
            lastCaret = editor.CaretOffset;
        }

        private void TextArea_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key.Equals(System.Windows.Input.Key.Back))
            {
                if (completionWindowUse != null)
                {
                    completionWindowUse.Close();
                    completionWindowUse = null;
                    return;
                }
            }
            if (!e.Key.Equals(System.Windows.Input.Key.Tab))
            {
                return;
            }
            if (completionWindowUse != null && completionWindowUse.CompletionList.SelectedItem == null && completionWindowUse.CompletionList.CompletionData.Count > 0)
            {
                completionWindowUse.CompletionList.SelectItem(completionWindowUse.CompletionList.CompletionData[0].Text);
            }
        }


        private int FindMainRuleSetIndex(XshdSyntaxDefinition definition)
        {
            if (definition == null)
            {
                return -1;
            }
            for (int i = 0; i < definition.Elements.Count; i++)
            {
                if (definition.Elements[i] is XshdRuleSet && definition.Elements[i] != null && (definition.Elements[i] as XshdRuleSet).Name == null)
                {
                    return i;
                }
            }
            return -1;
        }
        private int FindXshdColorByName(XshdSyntaxDefinition definition, string name)
        {
            if (definition == null)
            {
                return -1;
            }
            for (int i = 0; i < definition.Elements.Count; i++)
            {
                if (definition.Elements[i] is XshdRuleSet && definition.Elements[i] != null && (definition.Elements[i] as XshdRuleSet).Name == null)
                {
                    return i;
                }
            }
            return -1;
        }
        private void RefreshHighlighting()
        {
            using (XmlReader reader = new XmlTextReader("data\\avalon.xshd"))
            {
                var gLua = HighlightingLoader.LoadXshd(reader);
                if (nowFile != null && this.Text.Length > 4)
                {
                    string cName = this.Text.Substring(0, this.Text.Length - 4);
                    var cRule = new XshdKeywords();
                    cRule.Words.Add(cName);
                    XshdColor color = new XshdColor();
                    color.Name = "cFunction";
                    color.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x92, 0x1A, 0xFF));
                    color.FontWeight = System.Windows.FontWeights.Bold;
                    cRule.ColorReference = new XshdReference<XshdColor>(color);
                    (gLua.Elements[this.FindMainRuleSetIndex(gLua)] as XshdRuleSet).Elements.Insert(0, cRule);

                    var cRule2 = new XshdRule();
                    cRule2.Regex = @"\b(" + cName + @"\.[A-Za-z0-9_]+)";
                    XshdColor color2 = new XshdColor();
                    color2.Name = "cFunction2";
                    color2.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x92, 0x1A, 0xFF));
                    color2.FontWeight = System.Windows.FontWeights.Bold;
                    cRule2.ColorReference = new XshdReference<XshdColor>(color2);
                    (gLua.Elements[this.FindMainRuleSetIndex(gLua)] as XshdRuleSet).Elements.Insert(0, cRule2);
                }
                editor.SyntaxHighlighting = HighlightingLoader.Load(gLua, HighlightingManager.Instance);
            }
        }

        internal void InitTooltip(CodeConfig codecfg)
        {
            this.tooltipDic = codecfg.TooltipDic;
            foreach(var item in this.tooltipDic)
            {
                AutocompleteItem it = new AutocompleteItem();
                this.items = codecfg.Items;
            }
        }
        #endregion

        #region IEditForm接口
        public void SetActived()
        {
            Activate();
        }
        public bool CanOpen(string file)
        {
            return YGOUtil.IsScript(file);
        }
        public string GetOpenFile()
        {
            return nowFile;
        }
        public bool Create(string file)
        {
            return Open(file);
        }
        public bool Save()
        {
            return savefile(string.IsNullOrEmpty(nowFile));
        }
        public bool Open(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                if (!File.Exists(file))
                {
                    FileStream fs = new FileStream(file, FileMode.Create);
                    fs.Close();
                }
                nowFile = file;
                FileInfo fi = new FileInfo(file);
                if (fi.Name.ToUpper().EndsWith(".LUA"))
                {
                    //(this.fctb.SyntaxHighlighter as MySyntaxHighlighter).cCode
                    //    = fi.Name.Substring(0, fi.Name.Length - 4);
                }
                string cdb = MyPath.Combine(
                    Path.GetDirectoryName(file), "../cards.cdb");
                //SetCardDB(cdb);//后台加载卡片数据
                editor.Text = File.ReadAllText(file, new UTF8Encoding(false));
                oldtext = editor.Text;
                this.SetTitle();
                this.RefreshHighlighting();
                return true;
            }
            return false;
        }

        #endregion

        #region 设置标题
        void SetTitle()
        {
            string str;
            if (string.IsNullOrEmpty(nowFile))
            {
                str = title;
            }
            else
            {
                str = new FileInfo(nowFile).Name;
            }

            if (MdiParent != null)//如果父容器不为空
            {
                if (string.IsNullOrEmpty(nowFile))
                {
                    Text = title;
                    TabText = title;
                }
                else
                {
                    Text = Path.GetFileName(nowFile);
                }
                MdiParent.Text = str;
            }
            else
            {
                Text = str;
                TabText = str;
            }
        }

        void CodeEditFormEnter(object sender, EventArgs e)
        {
            SetTitle();
        }
        #endregion

        #region 悬停的函数说明
        //查找函数说明
        string FindTooltip(string word)
        {
            string desc = "";
            foreach (string v in tooltipDic.Keys)
            {
                int t = v.IndexOf(".");
                string k = v;
                if (t > 0)
                {
                    k = v.Substring(t + 1);
                }

                if (word == k)
                {
                    desc = tooltipDic[v];
                }
            }
            return desc;
        }

        //悬停的函数说明
        void FctbToolTipNeeded(object sender, ToolTipNeededEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.HoveredWord))
            {
                long tl = 0;
                string name = e.HoveredWord;
                string desc = "";
                if (!name.StartsWith("0x") && name.Length <= 9)
                {
                    name = name.Replace("c", "");
                    long.TryParse(name, out tl);
                }

                if (tl > 0)
                {
                    //获取卡片信息
                    if (cardlist.ContainsKey(tl))
                    {
                        desc = cardlist[tl];
                    }
                }
                else
                {
                    desc = FindTooltip(e.HoveredWord);
                }

                if (!string.IsNullOrEmpty(desc))
                {
                    e.ToolTipTitle = e.HoveredWord;
                    e.ToolTipText = desc;
                }
            }
        }
        #endregion

        #region 保存文件
        bool savefile(bool saveas)
        {
            string alltext = this.editor.Text;
            if (!tabisspaces)
            {
                alltext = alltext.Replace("    ", "\t");
            }

            if (saveas)
            {
                using (SaveFileDialog sfdlg = new SaveFileDialog())
                {
                    try
                    {
                        sfdlg.Filter = LanguageHelper.GetMsg(LMSG.ScriptFilter);
                    }
                    catch { }
                    if (sfdlg.ShowDialog() == DialogResult.OK)
                    {
                        nowFile = sfdlg.FileName;
                        SetTitle();
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            this.oldtext = this.editor.Text;
            File.WriteAllText(this.nowFile, alltext, new UTF8Encoding(false));
            return true;
        }

        public bool SaveAs()
        {
            return savefile(true);
        }

        void SaveToolStripMenuItemClick(object sender, EventArgs e)
        {
            Save();
        }
        void SaveAsToolStripMenuItemClick(object sender, EventArgs e)
        {
            SaveAs();
        }
        #endregion

        #region 菜单
        //显示/隐藏输入框
        void Menuitem_showinputClick(object sender, EventArgs e)
        {
            if (menuitem_showinput.Checked)
            {
                menuitem_showinput.Checked = false;
                tb_input.Visible = false;
            }
            else
            {
                menuitem_showinput.Checked = true;
                tb_input.Visible = true;
            }
        }
        //如果是作为mdi，则隐藏菜单
        void HideMenu()
        {
            if (MdiParent == null)
            {
                return;
            }

            mainMenu.Visible = false;
            menuitem_file.Visible = false;
            menuitem_file.Enabled = false;
        }

        void CodeEditFormLoad(object sender, EventArgs e)
        {
            ICSharpCode.AvalonEdit.TextEditor editor = new ICSharpCode.AvalonEdit.TextEditor();
            ICSharpCode.AvalonEdit.Rendering.TextView eView = new ICSharpCode.AvalonEdit.Rendering.TextView();
            Font f = new Font("微软雅黑", 14, FontStyle.Bold);
            string fontJson = MyConfig.ReadString(MyConfig.TOOLTIP_FONT);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            try
            {
                f = jss.Deserialize<FontHelper>(fontJson).ToFont();
            }
            catch { }
            lbTooltip.Font = f;
        }
        SearchPanel sp = null;
        void Menuitem_findClick(object sender, EventArgs e)
        {
            if (sp == null)
            {
                sp = SearchPanel.Install(editor.TextArea);
            }
            sp.Open();
        }

        FindReplaceForm frForm = new FindReplaceForm();
        void Menuitem_replaceClick(object sender, EventArgs e)
        {
            frForm.Show();
            LanguageHelper.SetFormLabel(frForm);
            frForm.btnFind.Click += BtnFind_Click;
            frForm.btnReplace.Click += BtnReplace_Click;
            frForm.btnReplaceAll.Click += BtnReplaceAll_Click;
            frForm.FormClosed += Form_FormClosed;
            frForm.txtFind.TextChanged += TxtFind_TextChanged;
        }

        private void BtnReplaceAll_Click(object sender, EventArgs e)
        {
            string newText = editor.Text.Replace(frForm.txtFind.Text, frForm.txtReplace.Text);
            editor.Document.Replace(0, editor.Document.TextLength, newText);
        }

        private void BtnReplace_Click(object sender, EventArgs e)
        {
            if (findStart < 0 || frForm.txtFind.Text == "")
            {
                return;
            }
            try
            {
                var replace = editor.Document.GetText(findStart - frForm.txtFind.Text.Length, frForm.txtFind.Text.Length);
                if (replace == frForm.txtFind.Text)
                {
                    editor.Document.Replace(findStart - frForm.txtFind.Text.Length, frForm.txtFind.Text.Length, frForm.txtReplace.Text);
                }
            }
            catch { }
        }

        private void TxtFind_TextChanged(object sender, EventArgs e)
        {
            findStart = -1;
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            findStart = -1;
            frForm = new FindReplaceForm();
        }

        int findStart = -1;
        private void BtnFind_Click(object sender, EventArgs e)
        {
            string text = editor.Text;
            string findStr = frForm.txtFind.Text;
            var rtxt = new RichTextBox();
            rtxt.Text = text;

            if (findStart < 0)
            {
                findStart = 0;
            }
            findStart = rtxt.Find(findStr, findStart, RichTextBoxFinds.MatchCase);
            if (findStart >= 0)
            {
                var line = editor.Document.GetLineByOffset(findStart).LineNumber;
                editor.ScrollTo(line, 0);
                editor.Select(findStart, findStr.Length);
                findStart = findStart + findStr.Length;
            }
        }
        void QuitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }

        void AboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            MyMsg.Show(
                LanguageHelper.GetMsg(LMSG.About) + "\t" + Application.ProductName + "\n"
                + LanguageHelper.GetMsg(LMSG.Version) + "\t1.1.0.0\n"
                + LanguageHelper.GetMsg(LMSG.Author) + "\tNanahira & JoyJ");
        }

        void Menuitem_openClick(object sender, EventArgs e)
        {
            using (OpenFileDialog sfdlg = new OpenFileDialog())
            {
                try
                {
                    sfdlg.Filter = LanguageHelper.GetMsg(LMSG.ScriptFilter);
                }
                catch { }
                if (sfdlg.ShowDialog() == DialogResult.OK)
                {
                    nowFile = sfdlg.FileName;
                    this.Open(nowFile);
                }
            }
        }

        #endregion

        #region 搜索函数
        //搜索函数
        void Tb_inputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string key = tb_input.Text;
                List<AutocompleteItem> list = new List<AutocompleteItem>();
                foreach (AutocompleteItem item in items)
                {
                    if (item.ToolTipText.Contains(key))
                    {
                        list.Add(item);
                    }
                }
                completionWindowUse = new CompletionWindow(editor.TextArea);
                completionWindowUse.StartOffset = editor.CaretOffset;
                completionWindowUse.EndOffset = editor.CaretOffset;
                IList<ICompletionData> data = completionWindowUse.CompletionList.CompletionData;
                if (string.IsNullOrEmpty(key))
                {
                    return;
                }
                foreach (var d in tooltipDic)
                {
                    if (d.Key.ToLower().StartsWith(key.ToLower()))
                    {
                        data.Add(new YGOProAutoCompletion(d.Key, d.Value));
                    }
                }
                if (data.Count > 0)
                {
                    completionWindowUse.Show();
                    string find2 = data[0].Text;
                    var ePos = editor.TextArea.Caret.CalculateCaretRectangle();
                    if (tooltipDic.ContainsKey(find2))
                    {
                        lbTooltip.Text = find2 + "\n" + tooltipDic[find2];
                        lbTooltip.Location = new Point(Math.Min((int)ePos.X + 800, host.Width - 500), Math.Min((int)ePos.Y, this.Height - lbTooltip.Height - 20));
                    }
                    completionWindowUse.Closed += delegate {
                        completionWindowUse = null;
                    };
                }
            }
        }
        #endregion

        #region 提示保存
        void CodeEditFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.oldtext))
            {
                if (this.editor.Text != this.oldtext)
                {
                    if (MyMsg.Question(LMSG.IfSaveScript))
                    {
                        this.Save();
                    }
                }
            }
            else if (this.editor.Text.Length > 0)
            {
                if (MyMsg.Question(LMSG.IfSaveScript))
                {
                    this.Save();
                }
            }
        }
        #endregion

        private void menuitem_testlua_Click(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(nowFile);
            string fn = fi.Name;
            if (!fn.ToUpper().EndsWith(".LUA"))
            {
                return;
            }
            string cCode = fn.Substring(0, fn.Length - 4);
            bool error = false;
            try
            {
                Directory.SetCurrentDirectory(fi.DirectoryName);
                Lua lua = new Lua();
                var env = lua.CreateEnvironment();
                string pre = "Duel={} Effect={} Card={} aux={} Auxiliary={} " + cCode + "={} Duel.LoadScript=function(str) end ";
                env.DoChunk(pre + this.editor.Text, "test.lua");
            }
            catch (LuaException ex)
            {
                MessageBox.Show($"LINE{ex.Line} - {ex.Message}");
                error = true;
            }
            if (!error)
            {
                MyMsg.Show(LMSG.syntaxCheckPassed);
            }
        }

        private void effectCreatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EffectCreatorForm form = new EffectCreatorForm();
            form.Show();
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void OnDragDtop(object sender, DragEventArgs e)
        {
            string[] drops = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (drops == null)
            {
                return;
            }
            List<string> files = new List<string>();
            foreach (string file in drops)
            {
                if (Directory.Exists(file))
                {
                    files.AddRange(Directory.EnumerateFiles(file, "*.cdb", SearchOption.AllDirectories));
                    files.AddRange(Directory.EnumerateFiles(file, "*.lua", SearchOption.AllDirectories));
                }
                files.Add(file);
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
                (DockPanel.Parent as MainForm).Open(file);
            }
        }
        private void menuitem_tooltipFont_Click(object sender, EventArgs e)
        {
            FontDialog fd = new FontDialog();
            string fontJson = MyConfig.ReadString(MyConfig.TOOLTIP_FONT);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            fd.Font = lbTooltip.Font;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                Common.XMLReader.Save(MyConfig.TOOLTIP_FONT, jss.Serialize(fd.Font));
                lbTooltip.Font = fd.Font;
            }
        }
        public class FontFamilyHelper
        {
            public string Name;
        }
        public class FontHelper
        {
            public FontFamilyHelper FontFamily;
            public bool Bold;
            public bool Italic;
            public string Name;
            public bool Strikeout;
            public bool Underline;
            public float Size;
            public int Unit;
            public int Height;

            public Font ToFont()
            {
                var style = (Bold ? FontStyle.Bold : FontStyle.Regular) | (Italic ? FontStyle.Italic : FontStyle.Regular) | (Strikeout ? FontStyle.Strikeout : FontStyle.Regular) | (Underline ? FontStyle.Underline : FontStyle.Regular);
                return new Font(FontFamily.Name, Size, style);
            }

        }
        private void lbTooltip_MouseMove(object sender, MouseEventArgs e)
        {
            lbTooltip.Text = "";
        }

        private void CodeEditForm_Avalon_KeyDown(object sender, KeyEventArgs e)
        {
            ;
        }
    }
}
