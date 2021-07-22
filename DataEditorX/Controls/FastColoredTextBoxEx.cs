/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 2014-10-24
 * 时间: 7:19
 * 
 */
using System;
using System.Drawing;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    public class FastColoredTextBoxEx : FastColoredTextBox
    {
        public Label lbTooltip;
        private Label lbSizeController;
        Point lastMouseCoord;
        public FastColoredTextBoxEx() : base()
        {
            this.SyntaxHighlighter = new MySyntaxHighlighter(this);
            this.TextChanged += this.FctbTextChanged;
            this.ToolTipDelay = 1;
            this.DelayedEventsInterval = 1;
            this.DelayedTextChangedInterval = 1;
            this.Selection.ColumnSelectionMode = true;
            this.InitializeComponent();
        }
        public new event EventHandler<ToolTipNeededEventArgs> ToolTipNeeded;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.lastMouseCoord = e.Location;
        }
        //函数悬停提示
        protected override void OnToolTip()
        {
            if (this.ToolTip == null)
            {
                return;
            }

            if (ToolTipNeeded == null)
            {
                return;
            }

            //get place under mouse
            Place place = this.PointToPlace(this.lastMouseCoord);

            //check distance
            Point p = this.PlaceToPoint(place);
            if (Math.Abs(p.X - this.lastMouseCoord.X) > this.CharWidth * 2 ||
                Math.Abs(p.Y - this.lastMouseCoord.Y) > this.CharHeight * 2)
            {
                return;
            }
            //get word under mouse
            var r = new Range(this, place, place);
            string hoveredWord = r.GetFragment("[a-zA-Z0-9_]").Text;
            //event handler
            var ea = new ToolTipNeededEventArgs(place, hoveredWord);
            ToolTipNeeded(this, ea);

            if (ea.ToolTipText != null)
            {
                this.ShowTooltipWithLabel(ea.ToolTipTitle, ea.ToolTipText);
            }
        }
        public void ShowTooltipWithLabel(AutocompleteItem item)
        {
            this.ShowTooltipWithLabel(item.ToolTipTitle, item.ToolTipText);
        }
        public void ShowTooltipWithLabel(string title, string text, int height)
        {
            this.lbTooltip.Visible = true;
            this.lbTooltip.Text = $"{title}\r\n\r\n{text}";
            this.lbTooltip.Location = new Point(this.Size.Width - 500, height);
        }

        public void ShowTooltipWithLabel(string title, string text)
        {
            this.ShowTooltipWithLabel(title, text, this.lastMouseCoord.Y + this.CharHeight);
        }

        //高亮当前词
        void FctbTextChanged(object sender, TextChangedEventArgs e)
        {
            //delete all markers
            this.Range.ClearFoldingMarkers();

            var currentIndent = 0;
            var lastNonEmptyLine = 0;

            for (int i = 0; i < this.LinesCount; i++)
            {
                var line = this[i];
                var spacesCount = line.StartSpacesCount;
                if (spacesCount == line.Count) //empty line
                {
                    continue;
                }

                if (currentIndent < spacesCount)
                {
                    //append start folding marker
                    this[lastNonEmptyLine].FoldingStartMarker = "m" + currentIndent;
                }
                else if (currentIndent > spacesCount)
                {
                    //append end folding marker
                    this[lastNonEmptyLine].FoldingEndMarker = "m" + spacesCount;
                }

                currentIndent = spacesCount;
                lastNonEmptyLine = i;
            }
        }

        private void InitializeComponent()
        {
            this.lbTooltip = new System.Windows.Forms.Label();
            this.lbSizeController = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // lbTooltip
            // 
            this.lbTooltip.AutoSize = true;
            this.lbTooltip.BackColor = System.Drawing.SystemColors.Desktop;
            this.lbTooltip.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbTooltip.ForeColor = System.Drawing.SystemColors.Control;
            this.lbTooltip.Location = new System.Drawing.Point(221, 117);
            this.lbTooltip.MaximumSize = new System.Drawing.Size(480, 0);
            this.lbTooltip.Name = "lbTooltip";
            this.lbTooltip.Size = new System.Drawing.Size(0, 28);
            this.lbTooltip.TabIndex = 1;
            this.lbTooltip.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lbTooltip_MouseMove);
            // 
            // lbSizeController
            // 
            this.lbSizeController.AutoSize = true;
            this.lbSizeController.BackColor = System.Drawing.Color.Transparent;
            this.lbSizeController.ForeColor = System.Drawing.Color.Transparent;
            this.lbSizeController.Location = new System.Drawing.Point(179, 293);
            this.lbSizeController.Name = "lbSizeController";
            this.lbSizeController.Size = new System.Drawing.Size(136, 16);
            this.lbSizeController.TabIndex = 2;
            this.lbSizeController.Text = "lbSizeController";
            // 
            // FastColoredTextBoxEx
            // 
            this.AutoScrollMinSize = new System.Drawing.Size(27, 14);
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.lbSizeController);
            this.Controls.Add(this.lbTooltip);
            this.Name = "FastColoredTextBoxEx";
            this.Size = new System.Drawing.Size(584, 327);
            this.Load += new System.EventHandler(this.FastColoredTextBoxEx_Load);
            this.Scroll += new System.Windows.Forms.ScrollEventHandler(this.FastColoredTextBoxEx_Scroll);
            this.SizeChanged += new System.EventHandler(this.FastColoredTextBoxEx_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void FastColoredTextBoxEx_Load(object sender, EventArgs e)
        {

        }

        private void lbTooltip_MouseMove(object sender, MouseEventArgs e)
        {
            this.lbTooltip.Visible = false;
        }
        private void ResizeWindow()
        {
            lbSizeController.Location = new Point(0, this.Height);
            lbSizeController.Text = "\r\n\r\n";
        }
        private void FastColoredTextBoxEx_SizeChanged(object sender, EventArgs e)
        {
            lbTooltip.Visible = false;
            this.ResizeWindow();
        }

        private void FastColoredTextBoxEx_Scroll(object sender, ScrollEventArgs e)
        {
            this.ResizeWindow();
        }
    }
}
