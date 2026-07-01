/*
MIT License

Copyright (c) 2026 Sarayut Chaisuriya

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
 
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Note on dataset:
The included MalwareBazaar sample CSV has been modified:
- Limited to first 500 rows
- Header format adjusted for teaching purposes
See README.md for full details.
*/
using System;
using System.IO;
using System.Windows.Forms;

namespace FileProcessing
{
    public partial class frmTextView : Form
    {
        /// <summary>
        /// Initializes a new instance of the frmTextView class.
        /// </summary>
        public frmTextView()
        {
            InitializeComponent();

            // ✅ ผูก Event KeyPress ให้กับ TextBox ทั้ง 4 ตัว เพื่อดักไม่ให้พิมพ์ตัวอักษร
            tbStartRow.KeyPress += new KeyPressEventHandler(NumericTextBox_KeyPress);
            tbEndRow.KeyPress += new KeyPressEventHandler(NumericTextBox_KeyPress);
            tbStartRowCSV.KeyPress += new KeyPressEventHandler(NumericTextBox_KeyPress);
            tbEndRowCSV.KeyPress += new KeyPressEventHandler(NumericTextBox_KeyPress);
        }

        // ✅ ฟังก์ชันสำหรับดักจับการกดคีย์บอร์ด (อนุญาตแค่ตัวเลข และปุ่มควบคุมเช่น Backspace)
        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ถ้าคีย์ที่กด ไม่ใช่ปุ่มควบคุม (Control) และ ไม่ใช่ตัวเลข (Digit)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // ยกเลิกการพิมพ์ตัวอักษรนั้นทิ้งไป
            }
        }

        /// <summary>
        /// Handles the Click event of the Read button by loading the contents of the specified file into the display area.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void btRead_Click(object sender, EventArgs e)
        {
            rtbShow.Clear(); // เคลียร์ข้อความเก่า

            // --- เตรียมตัวแปรรับค่าจาก UI ---
            int m = 1;
            int n = int.MaxValue;
            string filter = tbFormat.Text.Trim().ToLower();

            // เพิ่ม .Trim() เพื่อป้องกันบั๊กกรณีเผลอพิมพ์เว้นวรรค
            if (!string.IsNullOrEmpty(tbStartRow.Text)) int.TryParse(tbStartRow.Text.Trim(), out m);
            if (!string.IsNullOrEmpty(tbEndRow.Text)) int.TryParse(tbEndRow.Text.Trim(), out n);

            if (m > n)
            {
                MessageBox.Show("Start Row ต้องน้อยกว่าหรือเท่ากับ End Row", "Invalid Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (StreamReader srReader = new StreamReader(tbFileName.Text))
                {
                    string strLine;
                    int currentRow = 0;
                    int addedLines = 0;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    while ((strLine = srReader.ReadLine()) != null)
                    {
                        // ✅ เพิ่มเงื่อนไข: ถ้าบรรทัดไหนขึ้นต้นด้วย # ให้ข้ามไปเลย ไม่ต้องเอามานับ
                        if (strLine.StartsWith("#")) continue;

                        currentRow++;

                        if (currentRow < m) continue;
                        if (currentRow > n) break;

                        if (!string.IsNullOrEmpty(filter))
                        {
                            if (!strLine.ToLower().Contains(filter)) continue;
                        }

                        sb.AppendLine(strLine);
                        addedLines++;
                    }

                    rtbShow.Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Handles the Click event of the btReadCSV button, reading CSV data from the specified file and populating the
        /// DataGridView with its contents.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void btReadCSV_Click(object sender, EventArgs e)
        {
            // เคลียร์ตารางเก่า
            dgvData.Columns.Clear();
            dgvData.Rows.Clear();

            // --- เตรียมตัวแปรรับค่าจาก UI ฝั่ง CSV ---
            int m = 1;
            int n = int.MaxValue;
            string filter = tbFormatCSV.Text.Trim().ToLower();

            if (!string.IsNullOrEmpty(tbStartRowCSV.Text)) int.TryParse(tbStartRowCSV.Text.Trim(), out m);
            if (!string.IsNullOrEmpty(tbEndRowCSV.Text)) int.TryParse(tbEndRowCSV.Text.Trim(), out n);

            if (m > n)
            {
                MessageBox.Show("Start Row ต้องน้อยกว่าหรือเท่ากับ End Row", "Invalid Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (StreamReader srReader = new StreamReader(tbFileName.Text))
                {
                    string strLine;
                    bool bHeaderRead = false;
                    string[] strHeaders_arr = null;
                    int currentRow = 0;

                    while ((strLine = srReader.ReadLine()) != null)
                    {
                        // ✅ 1. ดักจับบรรทัด Comment (#) และเก็บหัวตาราง (#HEADER) แล้วข้ามบรรทัดนั้นไป
                        if (strLine.StartsWith("#"))
                        {
                            if (strLine.Length > 8 && strLine.Substring(0, 8).Equals("#HEADER"))
                            {
                                strHeaders_arr = strLine.Substring(8).Split(',');
                            }
                            continue;
                        }

                        string[] strValues_arr = strLine.Split(',');

                        // ✅ 2. สร้างหัวตาราง (Header) ให้เสร็จสมบูรณ์ก่อนเริ่มนับบรรทัดข้อมูล
                        if (!bHeaderRead)
                        {
                            foreach (string strHeader in strValues_arr)
                            {
                                if (strHeaders_arr == null)
                                    dgvData.Columns.Add(strHeader.Trim(), strHeader.Trim());
                                else
                                    dgvData.Columns.Add(strHeader.Trim(), strHeaders_arr[dgvData.Columns.Count].Trim());
                            }
                            bHeaderRead = true; // จำไว้ว่าสร้างหัวตารางเสร็จแล้ว
                            continue; // ข้ามไปอ่านข้อมูลบรรทัดถัดไปทันที
                        }

                        // ✅ 3. พอพ้นช่วง # และช่วง Header มาแล้ว ค่อยเริ่มนับข้อมูลบรรทัดที่ m ถึง n
                        currentRow++;

                        if (currentRow < m) continue;
                        if (currentRow > n) break;

                        if (!string.IsNullOrEmpty(filter))
                        {
                            if (!strLine.ToLower().Contains(filter)) continue;
                        }

                        dgvData.Rows.Add(strValues_arr);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Handles the Click event of the Browse button, allowing the user to select a file and displaying its path in the
        /// file name text box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void btBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    tbFileName.Text = ofd.FileName;
                }
            }
        }

        private void tbStartRow_TextChanged(object sender, EventArgs e)
        {

        }
    }   // End of frmTextView class
}