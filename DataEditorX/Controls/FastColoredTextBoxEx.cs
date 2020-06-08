/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 2014-10-24
 * 时间: 7:19
 * 
 */
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS
{
	public class FastColoredTextBoxEx : FastColoredTextBox
    {
        private Label lbTooltip;
        Point lastMouseCoord;
 
		public FastColoredTextBoxEx() : base()
		{
            this.SyntaxHighlighter = new MySyntaxHighlighter();
            this.InitializeComponent();
            this.ToolTipDelay = 1;
            this.TextChangedDelayed += this.FctbTextChangedDelayed;
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
			if (Math.Abs(p.X - this.lastMouseCoord.X) > this.CharWidth *2 ||
			    Math.Abs(p.Y - this.lastMouseCoord.Y) > this.CharHeight *2)
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
                lbTooltip.Visible = true;
                lbTooltip.Text = $"{ea.ToolTipTitle}\r\n\r\n{ea.ToolTipText}";
                lbTooltip.Location = new Point(this.Size.Width - 500, this.lastMouseCoord.Y + this.CharHeight);
                //this.ToolTip.ToolTipTitle = ea.ToolTipTitle;
                //this.ToolTip.ToolTipIcon = ea.ToolTipIcon;
                //ToolTip.SetToolTip(this, ea.ToolTipText);
                //this.ToolTip.Show(ea.ToolTipText, this, new Point(this.lastMouseCoord.X, this.lastMouseCoord.Y + this.CharHeight));
            }
        }


        //高亮当前词
        void FctbTextChangedDelayed(object sender, TextChangedEventArgs e)
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
            // FastColoredTextBoxEx
            // 
            this.AutoScrollMinSize = new System.Drawing.Size(27, 14);
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.lbTooltip);
            this.Name = "FastColoredTextBoxEx";
            this.Size = new System.Drawing.Size(584, 327);
            this.Load += new System.EventHandler(this.FastColoredTextBoxEx_Load);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void FastColoredTextBoxEx_Load(object sender, EventArgs e)
        {

        }

        private void lbTooltip_MouseMove(object sender, MouseEventArgs e)
        {
            lbTooltip.Visible = false;
        }
    }
}
