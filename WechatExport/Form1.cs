﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using iphonebackupbrowser;
using System.IO;
using mbdbdump;
using System.Runtime.Serialization.Plists;
using System.Collections;
using System.Drawing;

namespace WechatExport
{
    public partial class Form1 : Form
    {
        private List<iPhoneBackup> backups = new List<iPhoneBackup>();
        private List<mbdb.MBFileRecord> files92;
        private iPhoneBackup currentBackup = null;
        private iPhoneApp weixinapp = null;

        public Form1()
        {
            InitializeComponent();
        }
        private void LoadManifests()
        {
            backups.Clear();
            comboBox1.Items.Clear();

            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            s = Path.Combine(s, "Apple Computer", "MobileSync", "Backup");

            DirectoryInfo d = new DirectoryInfo(s);

            foreach (DirectoryInfo sd in d.EnumerateDirectories())
            {
                LoadManifest(sd.FullName);
            }

            foreach (iPhoneBackup b in backups)
            {
                b.index = comboBox1.Items.Add(b);
            }

            comboBox1.Items.Add("<选择其他备份文件夹...>");
        }
        private iPhoneBackup LoadManifest(string path)
        {
            iPhoneBackup backup = null;

            string filename = Path.Combine(path, "Info.plist");

            try
            {
                xdict dd = xdict.open(filename);

                if (dd != null)
                {
                    backup = new iPhoneBackup();

                    backup.path = path;

                    foreach (xdictpair p in dd)
                    {
                        if (p.item.GetType() == typeof(string))
                        {
                            switch (p.key)
                            {
                                case "Device Name": backup.DeviceName = (string)p.item; break;
                                case "Display Name": backup.DisplayName = (string)p.item; break;
                                case "Last Backup Date":
                                    DateTime.TryParse((string)p.item, out backup.LastBackupDate);
                                    break;
                            }
                        }
                    }

                    backups.Add(backup);
                    backups.Sort(iPhoneBackup.SortByDate);
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return backup;
        }
        private void loadCurrentBackup()
        {
            if (currentBackup == null)
                return;
            
            files92 = null;

            try
            {
                iPhoneBackup backup = currentBackup;

                // backup iTunes 9.2+
                if (File.Exists(Path.Combine(backup.path, "Manifest.mbdb")))
                {
                    files92 = mbdbdump.mbdb.ReadMBDB(backup.path);

                    BinaryPlistReader az = new BinaryPlistReader();
                    IDictionary er = az.ReadObject(Path.Combine(backup.path, "Manifest.plist"));

                    parseAll92(er);
                }

            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private class appFiles
        {
            public List<int> indexes = new List<int>();
            public long FilesLength = 0;

            public void add(int index, long length)
            {
                indexes.Add(index);
                FilesLength += length;
            }
        }

        private void parseAll92(IDictionary mbdb)
        {
            var sd = mbdb["Applications"] as Dictionary<object, object>;
            if (sd == null)
                return;

            var filesByDomain = new Dictionary<string, appFiles>();

            for (int i = 0; i < files92.Count; ++i)
            {
                if ((files92[i].Mode & 0xF000) == 0x8000)
                {
                    string d = files92[i].Domain;
                    if (!filesByDomain.ContainsKey(d))
                        filesByDomain.Add(d, new appFiles());

                    filesByDomain[d].add(i, files92[i].FileLength);
                }
            }


            foreach (var p in sd)
            {
                iPhoneApp app = new iPhoneApp();

                app.Key = p.Key as string;

                var zz = p.Value as IDictionary;

                //app.DisplayName = zz["CFBundleDisplayName"] as string;
                //app.Name = zz["CFBundleName"] as string;
                app.Identifier = zz["CFBundleIdentifier"] as string;
                app.Container = zz["Path"] as string;

                // il y a des applis mal paramétrées...
                //if (app.Name == null) app.Name = app.Key;
                //if (app.DisplayName == null) app.DisplayName = app.Name;

                if (filesByDomain.ContainsKey("AppDomain-" + app.Key))
                {
                    app.Files = new List<String>();

                    foreach (int i in filesByDomain["AppDomain-" + app.Key].indexes)
                    {
                        app.Files.Add(i.ToString());
                    }
                    app.FilesLength = filesByDomain["AppDomain-" + app.Key].FilesLength;

                    filesByDomain.Remove("AppDomain-" + app.Key);
                }

                addApp(app);
            }
        }

        private void addApp(iPhoneApp app)
        {
            if (app.Key == "com.tencent.xin")
            {
                weixinapp = app;
                label2.Text = "正确";
                label2.ForeColor = Color.Green;
                button2.Enabled = true;
            }
        }

        private void beforeLoadManifest()
        {
            comboBox1.SelectedIndex = -1;
            weixinapp = null;
            currentBackup = null;
            label2.Text = "未选择";
            label2.ForeColor = Color.Black;
            button2.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            beforeLoadManifest();
            LoadManifests();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ClientSize = new Size(groupBox2.Left * 2 + groupBox2.Width, groupBox2.Top + groupBox2.Height + groupBox1.Top);
            textBox1.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            button1_Click(null, null);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (comboBox1.SelectedIndex == -1)
                return;
            if (comboBox1.SelectedItem.GetType() == typeof(iPhoneBackup))
            {
                if (currentBackup == null || currentBackup.index != comboBox1.SelectedIndex)
                {
                    currentBackup = (iPhoneBackup)comboBox1.SelectedItem;
                    loadCurrentBackup();
                    if (weixinapp == null)
                    {
                        label2.Text = "未找到";
                        label2.ForeColor = Color.Red;
                    }
                }
                return;
            }
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "iPhone Backup|Info.plist|All files (*.*)|*.*";
            fd.FilterIndex = 1;
            fd.RestoreDirectory = true;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                beforeLoadManifest();
                iPhoneBackup b = LoadManifest(Path.GetDirectoryName(fd.FileName));

                if (b != null)
                {
                    b.custom = true;

                    comboBox1.Items.Insert(comboBox1.Items.Count - 1, b);

                    b.index = comboBox1.Items.Count - 2;

                    comboBox1.SelectedIndex = b.index;
                }
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                textBox1.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            radioButton2.Checked = true;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            if (textBox1.Text == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                radioButton1.Checked = true;
        }
    }
}