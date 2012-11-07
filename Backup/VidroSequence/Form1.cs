using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace VidroSequence
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            if (Properties.Settings.Default.List.Length > 0)
            {
                listBox1.Items.AddRange(Properties.Settings.Default.List.Split('&'));
            }
            textBox2_TextChanged(null, null);
            textBox3_TextChanged(null, null);

        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (data[0].Substring(data[0].Length - 5) == ".vdrm")
				{
					tabControl1.SelectedIndex = 1;
					textBox4.Text = data[0];
				}
				else
				{
					tabControl1.SelectedIndex = 0;
					listBox1.Items.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
				}
			}
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Queue<string> cq = new Queue<string>();	// シーン
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    foreach (string buf in listBox1.Items)
                        cq.Enqueue(buf);
                    break;
                case 1:
                    // VDRM
					if (!File.Exists(textBox4.Text))
                    {
                        MessageBox.Show("中断:VDRMファイルが見つかりません。");
                        return;
                    }
					Queue<string> pf = new Queue<string>(),	// prefix
								  sf = new Queue<string>(),	// suffix
								  ss = new Queue<string>();	// 置換対象
					int ni = 0, // 連番の最初
						nf = 0, // 連番の最後
						nd = 0; // 連番の桁(0なら桁調整しない)
					string vdrt = ""; // VDRのテンプレート
					StreamReader sr = new StreamReader(textBox4.Text);
					int f = 0x00;

					do
					{
						string line = sr.ReadLine();
						//VDRM パーサもどき
						if (line.Length == 0)
							continue;
						else if (line[0] != '#')
						{
							vdrt += line + "\n";
							continue;
						}
						else if (f >= 20)
						{
							foreach (string token in line.Split('#', ' ', '\t'))
							{
								if (token.Length == 0)
									continue;
								else if (token == "@VDRM_OBJECTS_END")
								{
									f = 0;
									break;
								}
								switch (f)
								{
									case 20: ss.Enqueue(token); f++; break;
									case 21: pf.Enqueue(token); f++; break;
									case 22: sf.Enqueue(token); f = 20; break;
								}
							}
						}
						else if (line.IndexOf("@VDRM") > 0)
						{
							foreach (string token in line.Split('#', ' ', '\t'))
							{
								if (token.Length == 0)
									continue;
								switch (f)
								{
									case 0:
										if (token == "@VDRM_RANGE")
											f = 10;
										else if (token == "@VDRM_OBJECTS_BEGIN")
											f = 20;
										break;
									case 10:
										ni = int.Parse(token);
										nd = token.ToString().Length;
										f++;
										break;
									case 11:
										nf = int.Parse(token);
										if (nd != token.ToString().Length)
											nd = 0;
										f = 0;
										break;
								}
							}
						}
					} while (!sr.EndOfStream);

					sr.Close();

					string vdr_pf = textBox4.Text.Substring(0,textBox4.Text.Length - 5);
					for (int n = ni; n <= nf; n++)
					{
						string num = (nd > 0) ? string.Format("{0:D" + nd + "}", n) : n.ToString();
						string buf = vdr_pf + "_" + num + ".vdr";
						StreamWriter sw = new StreamWriter(buf, false);
						Queue<string>.Enumerator ess = ss.GetEnumerator();
						Queue<string>.Enumerator epf = pf.GetEnumerator();
						Queue<string>.Enumerator esf = sf.GetEnumerator();
						string tmp = vdrt;
						while (ess.MoveNext() && epf.MoveNext() && esf.MoveNext())
							tmp = tmp.Replace('<' + ess.Current + '>', epf.Current + num + esf.Current);
						sw.Write(tmp);
						sw.Close();
						cq.Enqueue(buf);
					}
                    break;
            }

            Process pVidro = new Process();
            pVidro.StartInfo.FileName = textBox1.Text;
            foreach (string fileIn in cq)
            {
                if (checkBox1.Checked)
                {
                    string fn = fileIn.Substring(0, fileIn.Length - 4) + ".log";

                    if (File.Exists(fn))
                    {
                        continue;
                    }
                    else
                    {
                        FileStream fs = File.Create(fn);
                        fs.Close();
                        fs.Dispose();
                    }
                }

                pVidro.StartInfo.Arguments = "\"" + fileIn + "\"";
                pVidro.StartInfo.Arguments += " /run";
				if (radioButton1.Checked)
					pVidro.StartInfo.Arguments += " /out_pr=\"";
				else if (radioButton2.Checked)
					pVidro.StartInfo.Arguments += " /out_npr=\"";
				else if (radioButton3.Checked)
					pVidro.StartInfo.Arguments += " /out_normal=\"";
				else if (radioButton4.Checked)
					pVidro.StartInfo.Arguments += " /out_tr=\"";
				else
					pVidro.StartInfo.Arguments += " /out=\"";
				pVidro.StartInfo.Arguments += fileIn.Substring(0, fileIn.Length - 4) + comboBox1.Text + "\"";
                pVidro.StartInfo.Arguments += " /width=" + numericUpDown1.Value.ToString();
                pVidro.StartInfo.Arguments += " /height=" + numericUpDown2.Value.ToString();
                pVidro.StartInfo.Arguments += " /cp=" + numericUpDown3.Value.ToString();
                pVidro.StartInfo.Arguments += " /vp=" + numericUpDown4.Value.ToString();
                pVidro.StartInfo.Arguments += " /oversampling=" + numericUpDown5.Value.ToString();
                pVidro.StartInfo.Arguments += " /reflection=" + numericUpDown6.Value.ToString();
                pVidro.StartInfo.Arguments += " /indirectlight=" + numericUpDown7.Value.ToString();
                pVidro.StartInfo.Arguments += " /arealight=" + numericUpDown8.Value.ToString();
                pVidro.StartInfo.Arguments += " /skylight=" + numericUpDown9.Value.ToString();
                pVidro.StartInfo.Arguments += " /volume=" + numericUpDown10.Value.ToString();
                if(File.Exists(textBox2.Text)){
                    pVidro.StartInfo.Arguments += " /bg=\"" + textBox2.Text + "\"";
                }
                if (textBox3.Text.Length > 0)
                {
                    pVidro.StartInfo.Arguments += " " + textBox3.Text;
                }
                pVidro.Start();
                pVidro.WaitForExit();
            }
            /*
            if (checkBox2.Checked)
            {
                Dictionary<string, queue<string>> buf = new Dictionary<string, queue<string>>();
                foreach (string fileIn in listBox1.Items)
                {
                    StreamReader sr = new StreamReader(fileIn.Substring(0, fileIn.Length - 4) + ".log");
                    string line = sr.ReadLine();
                    string tokens = line.Split(0x09, StringSplitOptions.RemoveEmptyEntries);
                    buf.Add(;
                    sr.Close();
                }
                FileStream fs = File.CreateText(((string)(listBox1.Items[0])).Substring(0, fileIn.Length - 4) + ".csv");
                fs.Close();
            }
            */
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string path = textBox1.Text;
            openFileDialog1.InitialDirectory = path.Substring(0, path.LastIndexOf('\\'));
            if (!Directory.Exists(openFileDialog1.InitialDirectory))
            {
                openFileDialog1.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            }
            openFileDialog1.FileName = path.Substring(path.LastIndexOf('\\') + 1);
            openFileDialog1.Filter = "vidro.exe|vidro.exe";
            if (DialogResult.OK == openFileDialog1.ShowDialog())
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                while (listBox1.SelectedItems.Count > 0)
                {
                    listBox1.Items.Remove(listBox1.SelectedItem);
                }
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.VidroPath = textBox1.Text;
            Properties.Settings.Default.Vwidth = numericUpDown1.Value;
            Properties.Settings.Default.Vheight = numericUpDown2.Value;
            Properties.Settings.Default.Vcp = numericUpDown3.Value;
            Properties.Settings.Default.Vvp = numericUpDown4.Value;
            Properties.Settings.Default.Vindirectlight = numericUpDown7.Value;
            Properties.Settings.Default.Varealight = numericUpDown8.Value;
            Properties.Settings.Default.Vskylight = numericUpDown9.Value;
            Properties.Settings.Default.Vvolume = numericUpDown10.Value;
            Properties.Settings.Default.Voversampling = numericUpDown5.Value;
            Properties.Settings.Default.Vreflection = numericUpDown6.Value;
            Properties.Settings.Default.Vbg = textBox2.Text;
            Properties.Settings.Default.Vout = comboBox1.Text;
            Properties.Settings.Default.optLogExclusive = checkBox1.Checked;
            Properties.Settings.Default.Additional = textBox3.Text;
            string[] buf = new string[listBox1.Items.Count];
            listBox1.Items.CopyTo(buf, 0);
            Properties.Settings.Default.List = string.Join("&", buf);
            Properties.Settings.Default.Save();
        }

        private void checkBox1_MouseHover(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(checkBox1, "チェックすると既にログファイルの存在しているシーンファイルは処理から除外する。\n共有フォルダ等を使って複数のマシンから簡単に似非クラスター処理する為のフラグ。");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "image|*.png;*.jpg;*.gif;*.bmp|All|*.*";
            string path = string.Empty;
            if (label12.Enabled)
            {
                path = textBox2.Text;
            }
            else if (listBox1.Items.Count > 0)
            {
                path = (string)(listBox1.Items[0]);
            }
            if (path != string.Empty)
            {
                try
                {
                    openFileDialog1.InitialDirectory = path.Substring(0, path.LastIndexOf('\\'));
                    openFileDialog1.FileName = path.Substring(path.LastIndexOf('\\') + 1);
                }
                catch (ArgumentOutOfRangeException)
                {
                    openFileDialog1.InitialDirectory = string.Empty;
                    openFileDialog1.FileName = textBox2.Text;
                }
            }
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = openFileDialog1.FileName;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            label12.Enabled = (textBox2.Text.Length > 0);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            label13.Enabled = (textBox3.Text.Length > 0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string path = textBox4.Text;
            if (path.Length > 0)
                openFileDialog1.InitialDirectory = path.Substring(0, path.LastIndexOf('\\'));
            if (!Directory.Exists(openFileDialog1.InitialDirectory))
            {
                openFileDialog1.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            }
            openFileDialog1.FileName = path.Substring(path.LastIndexOf('\\') + 1);
            openFileDialog1.Filter = "VDRM|*.vdrm|All|*.*";
            if (DialogResult.OK == openFileDialog1.ShowDialog())
            {
                textBox4.Text = openFileDialog1.FileName;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1]))
            {
                tabControl1.SelectTab(1);
                textBox4.Text = args[1];
				if (args.Length > 2 && args[2] == "/run")
					button1_Click(null, null);
            }
        }
    }
}
