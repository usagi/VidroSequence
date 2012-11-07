using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace VidroSequence {

	public partial class Form1: Form {

		public Form1() {
			InitializeComponent();
			if(Properties.Settings.Default.List.Length > 0) {
				listBox1.Items.AddRange(Properties.Settings.Default.List.Split('&'));
			}
			textBox2_TextChanged(null, null);
			textBox3_TextChanged(null, null);

		}

		private void listBox1_DragDrop(object sender, DragEventArgs e) {
			if(e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);
				if(data[0].Substring(data[0].Length - 5) == ".vdrm") {
					tabControl1.SelectedIndex = 1;
					textBox4.Text = data[0];
				} else {
					tabControl1.SelectedIndex = 0;
					listBox1.Items.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
				}
			}
		}

		private void listBox1_DragEnter(object sender, DragEventArgs e) {
			e.Effect = DragDropEffects.All;
		}

		private void button1_Click(object sender, EventArgs e) {

			Process pVidro = new Process();
			pVidro.StartInfo.FileName = textBox1.Text;

			string arg_base;
			{
				var b = new StringBuilder("\"\uE000\" /run /out=\"\uE001\uE002\"");

				if(checkBox2.Checked) b.Append(" /out_pr=\"\uE001_pr\uE002\"");
				if(checkBox3.Checked) b.Append(" /out_npr=\"\uE001_npr\uE002\"");
				if(checkBox4.Checked) b.Append(" /out_contour=\"\uE001_contour\uE002\"");
				if(checkBox5.Checked) b.Append(" /out_tr=\"\uE001_tr\uE002\"");
				if(checkBox5.Checked) b.Append(" /out_normal=\"\uE001_normal\uE002\"");

				b.Append(" /width=" + numericUpDown1.Value.ToString());
				b.Append(" /height=" + numericUpDown2.Value.ToString());
				b.Append(" /cp=" + numericUpDown3.Value.ToString());
				b.Append(" /vp=" + numericUpDown4.Value.ToString());
				b.Append(" /oversampling=" + numericUpDown5.Value.ToString());
				b.Append(" /gi=" + numericUpDown7.Value.ToString());
				b.Append(" /arealight=" + numericUpDown8.Value.ToString());
				b.Append(" /pointlight=" + numericUpDown9.Value.ToString());
				b.Append(" /parallellight=" + numericUpDown10.Value.ToString());

				if(File.Exists(textBox2.Text))
					b.Append(" /bg=\"" + textBox2.Text + "\"");

				if(textBox3.Text.Length > 0)
					b.Append(" " + textBox3.Text);

				arg_base = Regex.Replace(b.ToString(), "\uE002", comboBox1.Text);
			}

			foreach(string fileIn in EnumTarget) {
				
				var fileIn_without_ext = Path.GetFileNameWithoutExtension(fileIn);

				if(checkBox1.Checked) {
					var fn = fileIn_without_ext + ".log";

					if(File.Exists(fn))
						continue;

					using(File.Create(fn)) { }
				}

				pVidro.StartInfo.Arguments = Regex.Replace(
					Regex.Replace(arg_base, "\uE001", fileIn_without_ext)
					, "\uE000", fileIn
				);

				pVidro.Start();
				pVidro.WaitForExit();
			}
		}

		private void button2_Click(object sender, EventArgs e) {
			string path = textBox1.Text;
			openFileDialog1.InitialDirectory = path.Substring(0, path.LastIndexOf('\\'));
			if(!Directory.Exists(openFileDialog1.InitialDirectory)) {
				openFileDialog1.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
			}
			openFileDialog1.FileName = path.Substring(path.LastIndexOf('\\') + 1);
			openFileDialog1.Filter = "vidro.exe|vidro.exe";
			if(DialogResult.OK == openFileDialog1.ShowDialog()) {
				textBox1.Text = openFileDialog1.FileName;
			}
		}

		private void listBox1_KeyDown(object sender, KeyEventArgs e) {
			if(e.KeyCode == Keys.Delete) {
				while(listBox1.SelectedItems.Count > 0) {
					listBox1.Items.Remove(listBox1.SelectedItem);
				}
			}

		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
			Properties.Settings.Default.VidroPath = textBox1.Text;
			Properties.Settings.Default.Vwidth = numericUpDown1.Value;
			Properties.Settings.Default.Vheight = numericUpDown2.Value;
			Properties.Settings.Default.Vcp = numericUpDown3.Value;
			Properties.Settings.Default.Vvp = numericUpDown4.Value;
			Properties.Settings.Default.Vgi = numericUpDown7.Value;
			Properties.Settings.Default.Varealight = numericUpDown8.Value;
			Properties.Settings.Default.Vpointlight = numericUpDown9.Value;
			Properties.Settings.Default.Vparallellight = numericUpDown10.Value;
			Properties.Settings.Default.Voversampling = numericUpDown5.Value;
			Properties.Settings.Default.Vbg = textBox2.Text;
			Properties.Settings.Default.Vout = comboBox1.Text;
			Properties.Settings.Default.optLogExclusive = checkBox1.Checked;
			Properties.Settings.Default.Additional = textBox3.Text;
			string[] buf = new string[listBox1.Items.Count];
			listBox1.Items.CopyTo(buf, 0);
			Properties.Settings.Default.List = string.Join("&", buf);
			Properties.Settings.Default.Save();
		}

		private void checkBox1_MouseHover(object sender, EventArgs e) {
			toolTip1.SetToolTip(checkBox1, "チェックすると既にログファイルの存在しているシーンファイルは処理から除外する。\n共有フォルダ等を使って複数のマシンから簡単に似非クラスター処理する為のフラグ。");
		}

		private void button3_Click(object sender, EventArgs e) {
			openFileDialog1.Filter = "image|*.png;*.jpg;*.gif;*.bmp|All|*.*";
			string path = string.Empty;
			if(label12.Enabled)
				path = textBox2.Text;
			else if(listBox1.Items.Count > 0)
				path = (string)(listBox1.Items[0]);
			
			if(path != string.Empty) {
				try {
					openFileDialog1.InitialDirectory = path.Substring(0, path.LastIndexOf('\\'));
					openFileDialog1.FileName = path.Substring(path.LastIndexOf('\\') + 1);
				} catch(ArgumentOutOfRangeException) {
					openFileDialog1.InitialDirectory = string.Empty;
					openFileDialog1.FileName = textBox2.Text;
				}
			}
			if(openFileDialog1.ShowDialog() == DialogResult.OK) {
				textBox2.Text = openFileDialog1.FileName;
			}
		}

		private void textBox2_TextChanged(object sender, EventArgs e) {
			label12.Enabled = (textBox2.Text.Length > 0);
		}

		private void textBox3_TextChanged(object sender, EventArgs e) {
			label13.Enabled = (textBox3.Text.Length > 0);
		}

		private void button4_Click(object sender, EventArgs e) {
			string path = textBox4.Text;
			if(path.Length > 0)
				openFileDialog1.InitialDirectory = path.Substring(0, path.LastIndexOf('\\'));
			if(!Directory.Exists(openFileDialog1.InitialDirectory)) {
				openFileDialog1.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
			}
			openFileDialog1.FileName = path.Substring(path.LastIndexOf('\\') + 1);
			openFileDialog1.Filter = "VDRM|*.vdrm|All|*.*";
			if(DialogResult.OK == openFileDialog1.ShowDialog()) {
				textBox4.Text = openFileDialog1.FileName;
			}
		}

		private void Form1_Load(object sender, EventArgs e) {
			string[] args = System.Environment.GetCommandLineArgs();
			if(args.Length > 1 && File.Exists(args[1])) {
				tabControl1.SelectTab(1);
				textBox4.Text = args[1];
				if(args.Length > 2 && args[2] == "/run")
					button1_Click(null, null);
			}
		}

	}
}
