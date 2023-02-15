using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace Text2Image
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string keyword;
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Icon = Properties.Resources.sync;
            keyword = "";
            checkForKeyword();
            if (keyword != "")
            {
                turnOnInterface();
            }
        }
        private void checkForKeyword()
        {
            if (keyword == "")
            {
                turnOffInterface();
                defineKeyword();
            }
            else turnOnInterface();
        }

        //using Vigenère polyalphabetic substitution cypher
        static string Encrypt(string input, string keyword)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                char k = keyword[i % keyword.Length];
                int codePoint = c + k;
                output.Append(char.ConvertFromUtf32(codePoint));
            }
            return output.ToString();
        }
        static string Decrypt(string input, string keyword)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                char k = keyword[i % keyword.Length];
                int codePoint = c - k;

                if (codePoint >= 0x0000 && codePoint <= 0x10FFFF && (codePoint < 0xD800 || codePoint > 0xDFFF))
                {
                    output.Append(char.ConvertFromUtf32(codePoint));
                }
                else
                {
                    // Handle invalid code point
                    output.Append(' ');
                }
            }
            return output.ToString().Trim();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.Visible = true;
            turnOffInterface();
            var encryptedText = Encrypt(richTextBox1.Text, keyword);
            var binary = ConvertStringToBinary(encryptedText);
            var colorList = ConvertBinaryToColors(binary);
            var bitmap = CreateColorBitmap(colorList);
            saveFileDialog1.FileName = "EncodedImage.png";
            saveFileDialog1.DefaultExt = "png";
            saveFileDialog1.Filter = "png files (*.png)|*.png";
            saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                bitmap.Save(saveFileDialog1.FileName, ImageFormat.Png);
                pictureBox1.Image = bitmap;
                MessageBox.Show(saveFileDialog1.FileName.ToString() + " has been saved.");                
            }
            turnOnInterface();
        }
        private void button2_Click(object sender, EventArgs e)
        {            
            turnOffInterface();
            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.DefaultExt = "png";
            openFileDialog1.Title = "Select Encoded Image";
            openFileDialog1.Filter = "png files (*.png)|*.png";
            label1.Visible = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var fileName = openFileDialog1.FileName;
                Bitmap bitmap = new Bitmap(fileName);
                pictureBox1.Image = bitmap;
                var colorList = ReadColorBitmap(bitmap);
                var encryptedText = ConvertColorsToUTC(colorList);
                var text = Decrypt(encryptedText, keyword);
                richTextBox1.Text = text;
            }
            turnOnInterface();
            pictureBox1.Visible = true;
            label1.Visible = false;
        }
        public static string ConvertStringToBinary(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            StringBuilder result = new StringBuilder(bytes.Length * 8);
            foreach (byte b in bytes)
            {
                result.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
            return result.ToString();
        }
        public static List<string> ConvertBinaryToColors(string binaryString)
        {
            int numDigits = binaryString.Length;
            
            if (numDigits % 24 != 0)
            {
                int paddingSize = 24 - (numDigits % 24);
                binaryString += new string('0', paddingSize);
            }

            List<string> colors = new List<string>();
            for (int i = 0; i < binaryString.Length; i += 24)
            {
                string chunk = binaryString.Substring(i, 24);
                int colorValue = Convert.ToInt32(chunk, 2);
                string hexValue = colorValue.ToString("X6");
                colors.Add(hexValue);
            }

            return colors;
        }
        public static Bitmap CreateColorBitmap(List<string> colors)
        {
            int numColors = colors.Count;
            int numRows = (int)Math.Ceiling(Math.Sqrt(numColors));
            int numCols = (int)Math.Ceiling((double)numColors / numRows);

            int pixelSize = 10;
            int bitmapWidth = numCols * pixelSize;
            int bitmapHeight = numRows * pixelSize;
            Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                for (int i = 0; i < numColors; i++)
                {
                    int row = i / numCols;
                    int col = i % numCols;

                    int x = col * pixelSize;
                    int y = row * pixelSize;

                    string hexColor = colors[i];
                    Color color = ColorTranslator.FromHtml("#" + hexColor);
                    Brush brush = new SolidBrush(color);

                    g.FillRectangle(brush, x, y, pixelSize, pixelSize);
                }
            }
            return bitmap;
        }
        public static List<string> ReadColorBitmap(Bitmap bitmap, Action<int> progressCallback = null)
        {
            int pixelSize = 10;
            int numCols = bitmap.Width / pixelSize;
            int numRows = bitmap.Height / pixelSize;

            List<string> colors = new List<string>();

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            IntPtr scan0 = bitmapData.Scan0;

            unsafe
            {
                byte* ptr = (byte*)scan0.ToPointer();

                for (int row = 0; row < numRows; row++)
                {
                    for (int col = 0; col < numCols; col++)
                    {
                        int x = col * pixelSize;
                        int y = row * pixelSize;
                        int offset = (y * bitmapData.Stride) + (x * bytesPerPixel);
                        byte blue = *(ptr + offset);
                        byte green = *(ptr + offset + 1);
                        byte red = *(ptr + offset + 2);
                        string hexColor = string.Format("{0:X2}{1:X2}{2:X2}", red, green, blue);
                        colors.Add(hexColor);

                        if (progressCallback != null)
                        {
                            int progress = colors.Count * 100 / (numRows * numCols);
                            progressCallback(progress);
                        }
                    }
                }
            }

            bitmap.UnlockBits(bitmapData);

            return colors;
        }
        public static string ConvertColorsToUTC(List<string> colors)
        {
            StringBuilder binaryString = new StringBuilder();
            foreach (string color in colors)
            {
                int colorValue = Convert.ToInt32(color, 16);
                string binary = Convert.ToString(colorValue, 2).PadLeft(24, '0');
                binaryString.Append(binary);
            }
            byte[] bytes = new byte[binaryString.Length / 8];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(binaryString.ToString().Substring(i * 8, 8), 2);
            }
            return Encoding.UTF8.GetString(bytes);
        }
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void defineKeyword()
        {
            KeywordDialogBox keywordDialog = new KeywordDialogBox();
            if (keywordDialog.ShowDialog() == DialogResult.OK)
            {
                string keyword = keywordDialog.Keyword;
                this.keyword = keyword;
            }
        }
        private void defineKeywordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            defineKeyword();
            checkForKeyword();
        }
        private void turnOffInterface()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            richTextBox1.Enabled = false;
        }
        private void turnOnInterface()
        {
            button1.Enabled = true;
            button2.Enabled = true;
            richTextBox1.Enabled = true;
        }
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
            pictureBox1.Visible = false;
        }
    }

    public class KeywordDialogBox : Form
    {
        private TextBox keywordTextBox;
        private Button okButton;
        private Button cancelButton;

        public KeywordDialogBox()
        {            
            this.Text = "Define Keyword";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Icon = Properties.Resources.sync;

            keywordTextBox = new TextBox();
            keywordTextBox.Location = new Point(10, 20);
            keywordTextBox.Width = 240;
            this.Controls.Add(keywordTextBox);

            okButton = new Button();
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(10, 45);
            this.Controls.Add(okButton);

            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(175, 45);
            this.Controls.Add(cancelButton);
            
            this.ClientSize = new Size(260, 80);
        }

        public string Keyword
        {
            get { return keywordTextBox.Text; }
        }
    }
}
