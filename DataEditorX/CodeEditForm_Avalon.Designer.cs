/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 2014-10-22
 * 时间: 19:16
 * 
 */
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;

namespace DataEditorX
{
    partial class CodeEditForm_Avalon
    {
        /// <summary>
        /// Designer variable used to keep track of non-visual components.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Disposes resources used by the form.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// This method is required for Windows Forms designer support.
        /// Do not change the method contents inside the source code editor. The Forms designer might
        /// not be able to load this method if it was changed manually.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.menuitem_file = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_open = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_save = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_saveas = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuitem_quit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_setting = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_showinput = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_find = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_replace = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_tooltipFont = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_tools = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_testlua = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_effectcreator = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_help = new System.Windows.Forms.ToolStripMenuItem();
            this.menuitem_about = new System.Windows.Forms.ToolStripMenuItem();
            this.tb_input = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.documentMap1 = new FastColoredTextBoxNS.DocumentMap();
            this.host = new System.Windows.Forms.Integration.ElementHost();
            this.editor = new ICSharpCode.AvalonEdit.TextEditor();
            this.lbTooltip = new System.Windows.Forms.Label();
            this.mainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuitem_file,
            this.menuitem_setting,
            this.menuitem_tools,
            this.menuitem_help});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(816, 24);
            this.mainMenu.TabIndex = 0;
            this.mainMenu.Text = "mainMenu";
            // 
            // menuitem_file
            // 
            this.menuitem_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuitem_open,
            this.menuitem_save,
            this.menuitem_saveas,
            this.toolStripSeparator1,
            this.menuitem_quit});
            this.menuitem_file.Name = "menuitem_file";
            this.menuitem_file.Size = new System.Drawing.Size(51, 20);
            this.menuitem_file.Text = "File(&F)";
            // 
            // menuitem_open
            // 
            this.menuitem_open.Name = "menuitem_open";
            this.menuitem_open.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.menuitem_open.Size = new System.Drawing.Size(145, 22);
            this.menuitem_open.Text = "Open";
            this.menuitem_open.Click += new System.EventHandler(this.Menuitem_openClick);
            // 
            // menuitem_save
            // 
            this.menuitem_save.Name = "menuitem_save";
            this.menuitem_save.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.menuitem_save.Size = new System.Drawing.Size(145, 22);
            this.menuitem_save.Text = "Save";
            this.menuitem_save.Click += new System.EventHandler(this.SaveToolStripMenuItemClick);
            // 
            // menuitem_saveas
            // 
            this.menuitem_saveas.Name = "menuitem_saveas";
            this.menuitem_saveas.Size = new System.Drawing.Size(145, 22);
            this.menuitem_saveas.Text = "Save As";
            this.menuitem_saveas.Click += new System.EventHandler(this.SaveAsToolStripMenuItemClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(142, 6);
            // 
            // menuitem_quit
            // 
            this.menuitem_quit.Name = "menuitem_quit";
            this.menuitem_quit.Size = new System.Drawing.Size(145, 22);
            this.menuitem_quit.Text = "Quit";
            this.menuitem_quit.Click += new System.EventHandler(this.QuitToolStripMenuItemClick);
            // 
            // menuitem_setting
            // 
            this.menuitem_setting.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuitem_showinput,
            this.menuitem_find,
            this.menuitem_replace,
            this.menuitem_tooltipFont});
            this.menuitem_setting.Name = "menuitem_setting";
            this.menuitem_setting.Size = new System.Drawing.Size(75, 20);
            this.menuitem_setting.Text = "Settings(&S)";
            // 
            // menuitem_showinput
            // 
            this.menuitem_showinput.Checked = true;
            this.menuitem_showinput.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuitem_showinput.Name = "menuitem_showinput";
            this.menuitem_showinput.Size = new System.Drawing.Size(184, 22);
            this.menuitem_showinput.Text = "Show/Hide InputBox";
            this.menuitem_showinput.Click += new System.EventHandler(this.Menuitem_showinputClick);
            // 
            // menuitem_find
            // 
            this.menuitem_find.Name = "menuitem_find";
            this.menuitem_find.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.menuitem_find.Size = new System.Drawing.Size(184, 22);
            this.menuitem_find.Text = "Find";
            this.menuitem_find.Click += new System.EventHandler(this.Menuitem_findClick);
            // 
            // menuitem_replace
            // 
            this.menuitem_replace.Name = "menuitem_replace";
            this.menuitem_replace.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
            this.menuitem_replace.Size = new System.Drawing.Size(184, 22);
            this.menuitem_replace.Text = "Replace";
            this.menuitem_replace.Click += new System.EventHandler(this.Menuitem_replaceClick);
            // 
            // menuitem_tooltipFont
            // 
            this.menuitem_tooltipFont.Name = "menuitem_tooltipFont";
            this.menuitem_tooltipFont.Size = new System.Drawing.Size(184, 22);
            this.menuitem_tooltipFont.Text = "Set Toolltip Font";
            this.menuitem_tooltipFont.Click += new System.EventHandler(this.menuitem_tooltipFont_Click);
            // 
            // menuitem_tools
            // 
            this.menuitem_tools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuitem_testlua,
            this.menuitem_effectcreator});
            this.menuitem_tools.Name = "menuitem_tools";
            this.menuitem_tools.Size = new System.Drawing.Size(60, 20);
            this.menuitem_tools.Text = "Tools(&T)";
            // 
            // menuitem_testlua
            // 
            this.menuitem_testlua.Name = "menuitem_testlua";
            this.menuitem_testlua.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.menuitem_testlua.Size = new System.Drawing.Size(164, 22);
            this.menuitem_testlua.Text = "Syntax Check";
            this.menuitem_testlua.Click += new System.EventHandler(this.menuitem_testlua_Click);
            // 
            // menuitem_effectcreator
            // 
            this.menuitem_effectcreator.Name = "menuitem_effectcreator";
            this.menuitem_effectcreator.ShortcutKeys = System.Windows.Forms.Keys.F3;
            this.menuitem_effectcreator.Size = new System.Drawing.Size(164, 22);
            this.menuitem_effectcreator.Text = "Effect Creator";
            this.menuitem_effectcreator.Visible = false;
            this.menuitem_effectcreator.Click += new System.EventHandler(this.effectCreatorToolStripMenuItem_Click);
            // 
            // menuitem_help
            // 
            this.menuitem_help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuitem_about});
            this.menuitem_help.Name = "menuitem_help";
            this.menuitem_help.Size = new System.Drawing.Size(61, 20);
            this.menuitem_help.Text = "Help(&H)";
            // 
            // menuitem_about
            // 
            this.menuitem_about.Name = "menuitem_about";
            this.menuitem_about.Size = new System.Drawing.Size(107, 22);
            this.menuitem_about.Text = "About";
            this.menuitem_about.Click += new System.EventHandler(this.AboutToolStripMenuItemClick);
            // 
            // tb_input
            // 
            this.tb_input.BackColor = System.Drawing.SystemColors.Control;
            this.tb_input.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tb_input.Location = new System.Drawing.Point(0, 486);
            this.tb_input.Margin = new System.Windows.Forms.Padding(0);
            this.tb_input.Name = "tb_input";
            this.tb_input.Size = new System.Drawing.Size(625, 21);
            this.tb_input.TabIndex = 1;
            this.tb_input.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Tb_inputKeyDown);
            // 
            // documentMap1
            // 
            this.documentMap1.BackColor = System.Drawing.Color.DimGray;
            this.documentMap1.Dock = System.Windows.Forms.DockStyle.Right;
            this.documentMap1.ForeColor = System.Drawing.Color.Maroon;
            this.documentMap1.Location = new System.Drawing.Point(625, 24);
            this.documentMap1.Name = "documentMap1";
            this.documentMap1.Size = new System.Drawing.Size(191, 483);
            this.documentMap1.TabIndex = 5;
            this.documentMap1.Target = null;
            this.documentMap1.Text = "documentMap1";
            this.documentMap1.Visible = false;
            // 
            // host
            // 
            this.host.Dock = System.Windows.Forms.DockStyle.Fill;
            this.host.Location = new System.Drawing.Point(0, 24);
            this.host.Name = "host";
            this.host.Size = new System.Drawing.Size(625, 462);
            this.host.TabIndex = 0;
            this.host.Child = this.editor;
            // 
            // lbTooltip
            // 
            this.lbTooltip.AutoSize = true;
            this.lbTooltip.BackColor = System.Drawing.SystemColors.Desktop;
            this.lbTooltip.Font = new System.Drawing.Font("宋体", 10F);
            this.lbTooltip.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lbTooltip.Location = new System.Drawing.Point(556, 230);
            this.lbTooltip.MaximumSize = new System.Drawing.Size(400, 0);
            this.lbTooltip.Name = "lbTooltip";
            this.lbTooltip.Size = new System.Drawing.Size(0, 14);
            this.lbTooltip.TabIndex = 6;
            this.lbTooltip.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lbTooltip_MouseMove);
            // 
            // CodeEditForm_Avalon
            // 
            this.AllowDrop = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(816, 507);
            this.Controls.Add(this.lbTooltip);
            this.Controls.Add(this.host);
            this.Controls.Add(this.tb_input);
            this.Controls.Add(this.documentMap1);
            this.Controls.Add(this.mainMenu);
            this.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MainMenuStrip = this.mainMenu;
            this.Name = "CodeEditForm_Avalon";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.TabText = "CodeEditor";
            this.Text = "CodeEditor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CodeEditFormFormClosing);
            this.Load += new System.EventHandler(this.CodeEditFormLoad);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDtop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnDragEnter);
            this.Enter += new System.EventHandler(this.CodeEditFormEnter);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CodeEditForm_Avalon_KeyDown);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        const string sep = "+-*/()[]{}., :\t\n";

        CompletionWindow completionWindowUse = null;
        int lastOffset = 0;
        private void editor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            // Open code completion after the user has pressed dot:
            completionWindowUse = new CompletionWindow(editor.TextArea);
            completionWindowUse.Closed += delegate
            {
                completionWindowUse = null;
            };
            completionWindowUse.CompletionList.ListBox.SelectionChanged += ListBox_SelectionChanged;
            completionWindowUse.StartOffset = lastOffset;
            if (completionWindowUse.EndOffset < completionWindowUse.StartOffset)
            {
                completionWindowUse.EndOffset = completionWindowUse.StartOffset;
            }
            IList<ICompletionData> data = completionWindowUse.CompletionList.CompletionData;
            string find = editor.Document.GetText(lastOffset, completionWindowUse.EndOffset - completionWindowUse.StartOffset);
            if (string.IsNullOrEmpty(find))
            {
                return;
            }
            foreach (var d in tooltipDic)
            {
                if (d.Key.ToLower().StartsWith(find.ToLower()))
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
                    lbTooltip.Location = new System.Drawing.Point(Math.Min((int)ePos.X + 800, host.Width - 500), Math.Min((int)ePos.Y, this.Height - lbTooltip.Height - 20));
                }
                completionWindowUse.Closed += delegate {
                    completionWindowUse = null;
                };
            }
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CompletionListBox lb = (CompletionListBox)sender;
            if (lb.SelectedItem == null)
            {
                return;
            }
            string find = (lb.SelectedItem as YGOProAutoCompletion).Text;
            var ePos = editor.TextArea.Caret.CalculateCaretRectangle();
            if (tooltipDic.ContainsKey(find))
            {
                lbTooltip.Text = find + "\n" + tooltipDic[find];
                lbTooltip.Location = new System.Drawing.Point(Math.Min((int)ePos.X + 800, host.Width - 500), Math.Min((int)ePos.Y, this.Height - lbTooltip.Height - 20));
            }
        }

        private void editor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsLetterOrDigit(e.Text[0]))
            {
                lastOffset = Math.Max(0, editor.CaretOffset + 1);
            }
            else
            {
                lastOffset = editor.CaretOffset - 1;
                string nowChar = editor.Document.GetCharAt(lastOffset).ToString();
                while (!sep.Contains(nowChar) && lastOffset > 0)
                {
                    lastOffset--;
                    nowChar = editor.Document.GetCharAt(lastOffset).ToString();
                }
                lastOffset++;
            }
            if (e.Text.Length > 0 && completionWindowUse != null)
            {
                completionWindowUse.StartOffset = lastOffset;
                // Whenever a non-letter is typed while the completion window is open,
                // insert the currently selected element.
                completionWindowUse.CompletionList.RequestInsertion(e);
            }
        }

        internal class YGOProAutoCompletion : ICompletionData
        {
            public YGOProAutoCompletion(string text, string description)
            {
                this.Text = text;
                _description = description;
            }

            public System.Windows.Media.ImageSource Image
            {
                get { return null; }
            }

            public string Text { get; private set; }
            private string _description;

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content
            {
                get { return this.Text; }
            }

            public object Description
            {
                get { return _description; }
            }

            public double Priority
            {
                get { return 0; }
            }

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, this.Text);
            }
        }
        private System.Windows.Forms.ToolStripMenuItem menuitem_replace;
        private System.Windows.Forms.ToolStripMenuItem menuitem_find;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem menuitem_showinput;
        private System.Windows.Forms.TextBox tb_input;
        private FastColoredTextBoxNS.DocumentMap documentMap1;
        private System.Windows.Forms.ToolStripMenuItem menuitem_about;
        private System.Windows.Forms.ToolStripMenuItem menuitem_help;
        private System.Windows.Forms.ToolStripMenuItem menuitem_setting;
        private System.Windows.Forms.ToolStripMenuItem menuitem_quit;
        private System.Windows.Forms.ToolStripMenuItem menuitem_saveas;
        private System.Windows.Forms.ToolStripMenuItem menuitem_save;
        private System.Windows.Forms.ToolStripMenuItem menuitem_open;
        private System.Windows.Forms.ToolStripMenuItem menuitem_file;
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ToolStripMenuItem menuitem_tools;
        private System.Windows.Forms.ToolStripMenuItem menuitem_testlua;
        private System.Windows.Forms.ToolStripMenuItem menuitem_effectcreator;
        private ElementHost host;
        ICSharpCode.AvalonEdit.TextEditor editor;
        private Label lbTooltip;
        private ToolStripMenuItem menuitem_tooltipFont;
    }
}
