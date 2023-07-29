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
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;
using WeifenLuo.WinFormsUI.Docking;

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
            editor.TextChanged += Editor_TextChanged;
            editor.MouseMove += Editor_MouseMove;
            editor.PreviewMouseWheel += Editor_MouseWheel;
            editor.FontFamily = new System.Windows.Media.FontFamily(DEXConfig.ReadString(DEXConfig.TAG_FONT_NAME));
            editor.FontSize = DEXConfig.ReadFloat(DEXConfig.TAG_FONT_SIZE, (float)editor.FontSize);
            editor.TextArea.FontFamily = editor.FontFamily;
            editor.TextArea.FontSize = editor.FontSize;
            editor.WordWrap = DEXConfig.ReadBoolean(DEXConfig.TAG_WORDWRAP);
            editor.Background = System.Windows.Media.Brushes.Black;
            editor.Foreground = System.Windows.Media.Brushes.GhostWhite;
            editor.AllowDrop = true;
            editor.PreviewDragEnter += Editor_DragEnter;
            this.RefreshHighlighting();
        }

        private void Editor_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var data = e.Data.GetData(DataFormats.FileDrop);
                    string[] files = (string[])data;
                    (this.DockPanel.Parent as MainForm).Open(files[0]);
                    SendKeys.Send("{ESC}");
                }
            }
            catch { }
            try
            {
                if (e.Data.GetDataPresent(DataFormats.Text))
                {
                    string file = (string)e.Data.GetData(DataFormats.Text);
                    (this.DockPanel.Parent as MainForm).Open(file);
                    SendKeys.Send("{ESC}");
                }
            }
            catch { }
        }

        private void Editor_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                double add = editor.TextArea.FontSize + (e.Delta > 0 ? 1 : -1);
                if (add > 100)
                {
                    add = 100;
                }
                if (add < 8)
                {
                    add = 8;
                }
                editor.TextArea.FontSize = add;
                editor.FontSize = add;
                DEXConfig.Save(DEXConfig.TAG_FONT_SIZE, ((int)add).ToString());
            }
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
                    var x = (int)(host.Width - lbTooltip.Width - System.Windows.SystemParameters.ScrollWidth);
                    var y = (int)ePos.Y;
                    if (y + lbTooltip.Height > host.Height)
                    {
                        y -= (y + lbTooltip.Height - host.Height);
                    }
                    lbTooltip.Location = new Point(x,y);
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
        private void RefreshHighlighting()
        {
            FileInfo fi = new FileInfo(Application.ExecutablePath);
            using (XmlReader reader = new XmlTextReader(fi.DirectoryName + "\\data\\avalon.xshd"))
            {
                var gLua = HighlightingLoader.LoadXshd(reader);
                if (nowFile != null && this.Text.Length > 4)
                {
                    string cName = this.Text.Substring(0, this.Text.Length - 4);
                    var cRule = new XshdKeywords();
                    cRule.Words.Add(cName);
                    XshdColor color = new XshdColor
                    {
                        Name = "cFunction",
                        Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x92, 0x1A, 0xFF)),
                        FontWeight = System.Windows.FontWeights.Bold
                    };
                    cRule.ColorReference = new XshdReference<XshdColor>(color);
                    (gLua.Elements[this.FindMainRuleSetIndex(gLua)] as XshdRuleSet).Elements.Insert(0, cRule);

                    var cRule2 = new XshdRule
                    {
                        Regex = @"\b(" + cName + @"\.[A-Za-z0-9_]+)"
                    };
                    XshdColor color2 = new XshdColor
                    {
                        Name = "cFunction2",
                        Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x92, 0x1A, 0xFF)),
                        FontWeight = System.Windows.FontWeights.Bold
                    };
                    cRule2.ColorReference = new XshdReference<XshdColor>(color2);
                    (gLua.Elements[this.FindMainRuleSetIndex(gLua)] as XshdRuleSet).Elements.Insert(0, cRule2);
                }
                editor.SyntaxHighlighting = HighlightingLoader.Load(gLua, HighlightingManager.Instance);
            }
        }

        internal void InitTooltip(CodeConfig codecfg)
        {
            this.tooltipDic = codecfg.TooltipDic;
            this.items = codecfg.Items;
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
            return SaveFile(string.IsNullOrEmpty(nowFile));
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
                editor.Text = File.ReadAllText(file, new UTF8Encoding(false));
                Regex regex = new Regex(@"c([0-9]+)\.lua");
                var match = regex.Match(fi.Name.ToLower());
                if (match.Success)
                {
                    string code = match.Groups[1].Value;
                    functionCompletions.Add(new FunctionParamsAutoCompletion($"e1=Effect.CreateEffect(c)\n\te1:SetType(EFFECT_TYPE_)\n\te1:SetCode()\n\te1:SetCategory(CATEGORY_)\n\te1:SetRange(LOCATION_)\n\te1:SetDescription(aux.Stringid({code},0))\n\te1:SetCondition(c{code}.con)\n\te1:SetProperty(EFFECT_FLAG_)\n\te1:SetCost(c{code}.cost)\n\te1:SetTarget(c{code}.tg)\n\te1:SetOperation(c{code}.op)\n\tc:RegisterEffect(e1)", "效果模板", 2));
                }
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
        bool SaveFile(bool saveas)
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
            return SaveFile(true);
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

        void CodeEditFormLoad(object sender, EventArgs e)
        {
            Font f = new Font("微软雅黑", 14, FontStyle.Bold);
            string fontJson = DEXConfig.ReadString(DEXConfig.TOOLTIP_FONT);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            try
            {
                f = jss.Deserialize<FontHelper>(fontJson).ToFont();
            }
            catch 
            {
                DEXConfig.Save(DEXConfig.TOOLTIP_FONT, jss.Serialize(f));
            }
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
            var rtxt = new RichTextBox
            {
                Text = text
            };

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
                findStart += findStr.Length;
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
                completionWindowUse = new CompletionWindow(editor.TextArea)
                {
                    StartOffset = editor.CaretOffset,
                    EndOffset = editor.CaretOffset
                };
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
                    completionWindowUse.Closed += delegate
                    {
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
            JavaScriptSerializer jss = new JavaScriptSerializer();
            fd.Font = lbTooltip.Font;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                Common.XMLReader.Save(DEXConfig.TOOLTIP_FONT, jss.Serialize(fd.Font));
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

        private void setCodeEditorFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontDialog fd = new FontDialog();
            fd.MaxSize = 100;
            fd.MinSize = 8;
            fd.Font = new Font(DEXConfig.ReadString(DEXConfig.TAG_FONT_NAME), (float)editor.FontSize);
            if (fd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    editor.TextArea.FontFamily = new System.Windows.Media.FontFamily(fd.Font.FontFamily.Name);
                    editor.TextArea.FontSize = fd.Font.Size;
                    DEXConfig.Save(DEXConfig.TAG_FONT_NAME, fd.Font.FontFamily.Name);
                    DEXConfig.Save(DEXConfig.TAG_FONT_SIZE, fd.Font.Size.ToString());
                }
                catch { }
            }
        }

        private void menuitem_fixCardCode_Click(object sender, EventArgs e)
        {
            string text = editor.Text;
            Regex regex = new Regex(@"(c[0-9]{4,9})");
            var matches = regex.Matches(text);
            string cName = "";
            if (nowFile != null && regex.IsMatch(nowFile))
            {
                cName = regex.Match(nowFile).Groups[1].Value;
            }
            else
            {
                MyMsg.Show(LMSG.InvalidFileName);
                return;
            }
            HashSet<string> hs = new HashSet<string>();
            foreach (Match match in matches)
            {
                hs.Add(match.Groups[1].Value);
            }
            foreach (string str in hs)
            {
                text = text.Replace(str, cName);
                text = text.Replace(str.Substring(1), cName.Substring(1));
            }
            editor.Text = text;
        }
    }
}
