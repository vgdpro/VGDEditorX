﻿/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 5月12 星期一
 * 时间: 12:00
 * 
 */
using DataEditorX.Config;
using DataEditorX.Language;
using System;
using System.IO;
using System.Windows.Forms;


namespace DataEditorX
{
    internal sealed class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string arg = (args.Length > 0) ? args[0] : "";
            if (arg == DEXConfig.TAG_SAVE_LAGN || arg == DEXConfig.TAG_SAVE_LAGN2)
            {
                //保存语言
                SaveLanguage();
                MessageBox.Show("Save Language OK.");
                Environment.Exit(1);
            }
            if (DEXConfig.OpenOnExistForm(arg))//在已经存在的窗口打开文件
            {
                Environment.Exit(1);
            }
            else//新建窗口
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                MainForm mainForm = new MainForm();
                //设置将要打开的文件
                mainForm.SetOpenFile(arg);
                //数据目录
                mainForm.SetDataPath(MyPath.Combine(Application.StartupPath, DEXConfig.TAG_DATA));

                Application.Run(mainForm);
            }
        }
        static void SaveLanguage()
        {
            string datapath = MyPath.Combine(Application.StartupPath, DEXConfig.TAG_DATA);
            string conflang = DEXConfig.GetLanguageFile(datapath);
            LanguageHelper.LoadFormLabels(conflang);
            LanguageHelper langhelper = new LanguageHelper();
            MainForm form1 = new MainForm();
            LanguageHelper.SetFormLabel(form1);
            langhelper.GetFormLabel(form1);
            DataEditForm form2 = new DataEditForm();
            LanguageHelper.SetFormLabel(form2);
            langhelper.GetFormLabel(form2);
            CodeEditForm_Avalon form3 = new CodeEditForm_Avalon();
            LanguageHelper.SetFormLabel(form3);
            langhelper.GetFormLabel(form3);
            // LANG.GetFormLabel(this);
            //获取窗体文字
            langhelper.SaveLanguage(conflang + ".bak");
        }

    }
}
