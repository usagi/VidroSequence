using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace VidroSequence {
	static class Program {
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}

	public partial class Form1 {

		private IEnumerable<string> EnumTarget {

			get {

				switch(tabControl1.SelectedIndex) {

				case 0: // directly

					foreach(string buf in listBox1.Items)
						yield return buf;

					break;

				case 1: // VDRM

					if(!File.Exists(textBox4.Text)) {
						MessageBox.Show("中断:VDRMファイルが見つかりません。");
						yield break;
					}

					Queue<string> pf = new Queue<string>(),	// prefix
								  sf = new Queue<string>(),	// suffix
								  ss = new Queue<string>();	// 置換対象

					int ni = 0, // 連番の最初
						nf = 0, // 連番の最後
						nd = 0; // 連番の桁(0なら桁調整しない)

					string vdrt = ""; // VDRのテンプレート

					using(StreamReader sr = new StreamReader(textBox4.Text)) {

						int f = 0x00; // パース状態のフラグ

						do {

							string line = sr.ReadLine();
							//VDRM パーサもどき
							if(line.Length == 0)
								continue;
							else if(line[0] != '#') {
								vdrt += line + "\n";
								continue;
							} else if(f >= 20) {
								foreach(string token in line.Split('#', ' ', '\t')) {
									if(token.Length == 0)
										continue;
									else if(token == "@VDRM_OBJECTS_END") {
										f = 0;
										break;
									}
									switch(f) {
									case 20: ss.Enqueue(token); f++; break;
									case 21: pf.Enqueue(token); f++; break;
									case 22: sf.Enqueue(token); f = 20; break;
									}
								}
							} else if(line.IndexOf("@VDRM") > 0) {
								foreach(string token in line.Split('#', ' ', '\t')) {
									if(token.Length == 0)
										continue;
									switch(f) {
									case 0:
										if(token == "@VDRM_RANGE")
											f = 10;
										else if(token == "@VDRM_OBJECTS_BEGIN")
											f = 20;
										break;
									case 10:
										ni = int.Parse(token);
										nd = token.ToString().Length;
										f++;
										break;
									case 11:
										nf = int.Parse(token);
										if(nd != token.ToString().Length)
											nd = 0;
										f = 0;
										break;
									}
								}
							}
						} while(!sr.EndOfStream);
					}


					string vdr_pf = textBox4.Text.Substring(0, textBox4.Text.Length - 5);
					for(int n = ni; n <= nf; n++) {
						string num = (nd > 0) ? string.Format("{0:D" + nd + "}", n) : n.ToString();
						string buf = vdr_pf + "_" + num + ".vdr";
						StreamWriter sw = new StreamWriter(buf, false);
						Queue<string>.Enumerator ess = ss.GetEnumerator();
						Queue<string>.Enumerator epf = pf.GetEnumerator();
						Queue<string>.Enumerator esf = sf.GetEnumerator();
						string tmp = vdrt;
						while(ess.MoveNext() && epf.MoveNext() && esf.MoveNext())
							tmp = tmp.Replace('<' + ess.Current + '>', epf.Current + num + esf.Current);
						sw.Write(tmp);
						sw.Close();
						yield return buf;
					}
					break;
				}
			}
		}
	}
}
