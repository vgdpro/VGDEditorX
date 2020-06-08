/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 2014-10-23
 * 时间: 23:14
 * 
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace FastColoredTextBoxNS
{
	/// <summary>
	/// ygocore的lua高亮，夜间
	/// </summary>
	public class MySyntaxHighlighter : SyntaxHighlighter
	{
        readonly TextStyle mNumberStyle = new TextStyle(Brushes.Orange, null, FontStyle.Regular);
        readonly TextStyle mStrStyle = new TextStyle(Brushes.Gold, null, FontStyle.Regular);
        readonly TextStyle conStyle = new TextStyle(Brushes.YellowGreen, null, FontStyle.Regular);
        readonly TextStyle mKeywordStyle = new TextStyle(Brushes.DeepSkyBlue, null, FontStyle.Regular);
        readonly TextStyle mGrayStyle = new TextStyle(Brushes.Gray, null, FontStyle.Regular);
        readonly TextStyle mFunStyle = new TextStyle(Brushes.LightGray, null, FontStyle.Bold);
		
		
		/// <summary>
		/// Highlights Lua code
		/// </summary>
		/// <param name="range"></param>
		public override void LuaSyntaxHighlight(Range range)
		{
			range.tb.CommentPrefix = "--";
			range.tb.LeftBracket = '(';
			range.tb.RightBracket = ')';
			range.tb.LeftBracket2 = '{';
			range.tb.RightBracket2 = '}';
			range.tb.BracketsHighlightStrategy = BracketsHighlightStrategy.Strategy2;

			range.tb.AutoIndentCharsPatterns
				= @"^\s*[\w\.]+(\s\w+)?\s*(?<range>=)\s*(?<range>.+)";

			//clear style of changed range
			range.ClearStyle(this.mStrStyle, this.mGrayStyle, this.conStyle, this.mNumberStyle, this.mKeywordStyle, this.mFunStyle);
			//
			if (base.LuaStringRegex == null)
            {
                base.InitLuaRegex();
            }
            //string highlighting
            range.SetStyle(this.mStrStyle, base.LuaStringRegex);
			//comment highlighting
			range.SetStyle(this.mGrayStyle, base.LuaCommentRegex1);
			range.SetStyle(this.mGrayStyle, base.LuaCommentRegex2);
			range.SetStyle(this.mGrayStyle, base.LuaCommentRegex3);
			//number highlighting
			range.SetStyle(this.mNumberStyle, base.LuaNumberRegex);

			//keyword highlighting
			range.SetStyle(this.mKeywordStyle, base.LuaKeywordRegex);
			//functions highlighting
			range.SetStyle(this.mFunStyle, base.LuaFunctionsRegex);
			range.SetStyle(this.mNumberStyle, @"\bc\d+\b");
			
			range.SetStyle(this.conStyle, @"[\s|\(|+|,]{0,1}(?<range>[A-Z_]+?)[\)|+|\s|,|;]");
			//range.SetStyle(mFunStyle, @"[:|\.|\s](?<range>[a-zA-Z0-9_]*?)[\(|\)|\s]");
			
			//clear folding markers
			range.ClearFoldingMarkers();
			//set folding markers
			range.SetFoldingMarkers("{", "}"); //allow to collapse brackets block
			range.SetFoldingMarkers(@"--\[\[", @"\]\]"); //allow to collapse comment block
		}
	}
}
