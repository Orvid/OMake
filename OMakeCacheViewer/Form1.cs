using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OMake;

namespace OMakeCacheViewer
{
	public partial class Form1 : Form
	{
		private Cache curCache;

		public Form1()
		{
			InitializeComponent();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				curCache = new Cache(openFileDialog1.FileName);
				splitContainer1.Enabled = true;
				saveToolStripMenuItem.Enabled = true;
				saveAsToolStripMenuItem.Enabled = true;
				RefreshListView();
				saveFileDialog1.FileName = openFileDialog1.FileName;
			}
		}

		public void RefreshListView()
		{
			listView1.Items.Clear();
			foreach (KeyValuePair<string, Cache.CacheObject> co in curCache.objects)
			{
				listView1.Items.Add(new ListViewItem(new string[] { co.Value.Name, co.Value.Type.ToString(), GetStringForCacheObjectValue(co.Value) }));
			}
		}

		private string GetStringForCacheObjectValue(Cache.CacheObject obj)
		{
			switch (obj.Type)
			{
				case Cache.CacheObjectType.String:
				case Cache.CacheObjectType.Byte:
				case Cache.CacheObjectType.SByte:
				case Cache.CacheObjectType.UShort:
				case Cache.CacheObjectType.Short:
				case Cache.CacheObjectType.UInt:
				case Cache.CacheObjectType.Int:
				case Cache.CacheObjectType.ULong:
				case Cache.CacheObjectType.Long:
				case Cache.CacheObjectType.Float:
				case Cache.CacheObjectType.Double:
				case Cache.CacheObjectType.Decimal:
					return obj.Value.ToString();
				case Cache.CacheObjectType.Bool:
					return ((bool)obj.Value) ? "true" : "false";
				case Cache.CacheObjectType.Data:
					StringBuilder sb = new StringBuilder();
					sb.Append("{ ");
					byte[] dat = (byte[])obj.Value;
					bool first = true;
					for (uint i = 0; i < dat.Length; i++)
					{
						if (!first)
							sb.Append(", " + ConvertToHex(dat[i]));
						else
							sb.Append(ConvertToHex(dat[i]));
						first = false;
					}
					sb.Append(" }");
					return sb.ToString();
				default:
					throw new Exception("Unknown StackObjectType!");
			}
		}

		#region ConvertToHex
		private static string ConvertToHex(UInt32 num)
		{
			string xHex = string.Empty;

			if (num == 0)
			{
				xHex = "0";
			}
			else
			{
				while (num != 0)
				{
					//Note; char is converted to string because Cosmos crashes when adding char and string. Frode, 7.june.
					//TODO: Is this still true? I think Cosmos can handle char + string just fine now.
					xHex = SingleDigitToHex((byte)(num & 0xf)) + xHex;
					num = num >> 4;
				}
			}

			return "0x" + (xHex.PadLeft(2, '0'));
		}

		private static string SingleDigitToHex(byte d)
		{
			switch (d)
			{
				case 0:
					return "0";
				case 1:
					return "1";
				case 2:
					return "2";
				case 3:
					return "3";
				case 4:
					return "4";
				case 5:
					return "5";
				case 6:
					return "6";
				case 7:
					return "7";
				case 8:
					return "8";
				case 9:
					return "9";
				case 10:
					return "A";
				case 11:
					return "B";
				case 12:
					return "C";
				case 13:
					return "D";
				case 14:
					return "E";
				case 15:
					return "F";
			}
			return " ";

		}
		#endregion

		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			curCache.Save();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				curCache.FileName = saveFileDialog1.FileName;
				curCache.Save();
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			string OldName = SelectedItem.SubItems[0].Text;

			SelectedItem.SubItems[0].Text = textBox1.Text;
			SelectedItem.SubItems[1].Text = EntryTypeComboBox.Text;
			SelectedItem.SubItems[2].Text = textBox2.Text;

			ModifyCacheValue(OldName, SelectedItem);
		}

		private void ModifyCacheValue(string oldName, ListViewItem itm)
		{
			Cache.CacheObject obj = curCache.objects[oldName];
			curCache.objects.Remove(oldName);
			obj.Name = itm.SubItems[0].Text;
			obj.Type = (Cache.CacheObjectType)Enum.Parse(typeof(Cache.CacheObjectType), itm.SubItems[1].Text);
			switch (obj.Type)
			{
				case Cache.CacheObjectType.String:
					obj.Value = itm.SubItems[2].Text;
					break;
				case Cache.CacheObjectType.Byte:
					obj.Value = Byte.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.SByte:
					obj.Value = SByte.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.UShort:
					obj.Value = UInt16.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.Short:
					obj.Value = Int16.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.UInt:
					obj.Value = UInt32.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.Int:
					obj.Value = Int32.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.ULong:
					obj.Value = UInt64.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.Long:
					obj.Value = Int64.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.Float:
					obj.Value = Single.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.Double:
					obj.Value = Double.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.Decimal:
					obj.Value = Decimal.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.Bool:
					obj.Value = Boolean.Parse(itm.SubItems[2].Text);
					break;
				case Cache.CacheObjectType.Data:
					List<byte> bl = new List<byte>();
					string dt = itm.SubItems[2].Text;
					dt.Replace("{", "");
					dt.Replace("}", "");
					List<string> vals = new List<string>(dt.Split(','));
					foreach (string s in vals)
					{
						bl.Add(byte.Parse(s, System.Globalization.NumberStyles.Integer | System.Globalization.NumberStyles.HexNumber));
					}
					obj.Value = bl.ToArray();
					break;
				default:
					throw new Exception("Unknown StackObjectType!");
			}
			curCache.objects.Add(obj.Name, obj);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			listView1_SelectedIndexChanged(null, null);
		}

		private void Form1_Load(object sender, EventArgs e)
		{

			EntryTypeComboBox.Items.Clear();
			foreach (System.Reflection.FieldInfo f in typeof(Cache.CacheObjectType).GetFields())
			{
				if (f.Name != "value__")
				{
					EntryTypeComboBox.Items.Add(f.Name);
				}
			}
		}

		private void EntryTypeComboBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		bool ListView_HadValSelected = false;
		ListViewItem SelectedItem;
		private void listView1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView1.SelectedItems.Count > 0)
			{
				tableLayoutPanel1.Enabled = true;
				textBox1.Text = listView1.SelectedItems[0].SubItems[0].Text;
				EntryTypeComboBox.Text = listView1.SelectedItems[0].SubItems[1].Text;
				textBox2.Text = listView1.SelectedItems[0].SubItems[2].Text;
				if (ListView_HadValSelected)
					button1.PerformClick();
				SelectedItem = listView1.SelectedItems[0];
				ListView_HadValSelected = true;
			}
			else if (ListView_HadValSelected)
			{
				button1.PerformClick();
				tableLayoutPanel1.Enabled = false;
				ListView_HadValSelected = false;
				textBox1.Text = "";
				EntryTypeComboBox.Text = "";
				textBox2.Text = "";
				SelectedItem = null;
			}
		}
	}
}