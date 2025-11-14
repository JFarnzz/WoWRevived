using System.Text;
using WoWViewer.Parsers;

namespace WoWViewer
{
    public partial class OJDParser : Form
    {
        private List<OjdEntry> entries = new List<OjdEntry>();

        public OJDParser()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Parse SFX.ojd file with improved error handling and async support.
        /// </summary>
        public async Task ParseSfxOjdAsync(string filename)
        {
            try
            {
                listBox1.Items.Clear();
                label1.Text = "Parsing...";

                await Task.Run(() =>
                {
                    var entries = SfxOjdParser.Parse(filename);
                    string logPath = Path.ChangeExtension(filename, "-dump.csv");

                    using var writer = new StreamWriter(logPath, false, Encoding.UTF8);
                    writer.WriteLine("Index,Offset,HeaderID,Length,Type,Text");

                    foreach (var entry in entries)
                    {
                        if (entry.Type == SfxEntryType.StringEntry)
                        {
                            Invoke(() => listBox1.Items.Add(entry.Text));
                        }
                        writer.WriteLine($"{entry.Index},{entry.Offset:X},{entry.HeaderId},{entry.Length},{entry.Type},\"{entry.Text.Replace("\"", "\"\"")}\"");
                    }

                    Invoke(() => label1.Text = $"Total Strings: {entries.Count}");
                });
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"File not found: {ex.FileName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label1.Text = "Error: File not found";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing SFX file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label1.Text = "Error during parsing";
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility.
        /// </summary>
        public void ParseSfxOjd(string filename)
        {
            ParseSfxOjdAsync(filename).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Parse OBJ.ojd file with improved error handling.
        /// </summary>
        public async Task ParseObjOjdAsync()
        {
            try
            {
                listBox1.Items.Clear();
                label1.Text = "Parsing...";

                await Task.Run(() =>
                {
                    entries = ObjOjdParser.Parse("OBJ.ojd");
                    string logPath = "ojd_log.txt";

                    // Write log file efficiently
                    using var writer = new StreamWriter(logPath, false, Encoding.UTF8);
                    foreach (var entry in entries)
                    {
                        Invoke(() => listBox1.Items.Add(entry.Name));
                        writer.WriteLine(entry.ToString());
                    }

                    Invoke(() => label1.Text = $"Total Entries: {entries.Count}");
                });
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"File not found: {ex.FileName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label1.Text = "Error: File not found";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing OBJ file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label1.Text = "Error during parsing";
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility.
        /// </summary>
        public void ParseObjOjd()
        {
            ParseObjOjdAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Parse TEXT.ojd file using the new TextOjdParser.
        /// </summary>
        public async Task ParseTextOjdAsync()
        {
            try
            {
                listBox1.Items.Clear();
                label1.Text = "Parsing TEXT.ojd...";

                await Task.Run(() =>
                {
                    var textEntries = TextOjdParser.Parse("TEXT.ojd");
                    string logPath = "text-ojd-log.txt";

                    using var writer = new StreamWriter(logPath, false, Encoding.UTF8);
                    foreach (var entry in textEntries)
                    {
                        Invoke(() => listBox1.Items.Add(entry.Text));
                        writer.WriteLine(entry.ToString());
                    }

                    Invoke(() => label1.Text = $"Total Strings: {textEntries.Count}");
                });
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"File not found: {ex.FileName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label1.Text = "Error: File not found";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing TEXT file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label1.Text = "Error during parsing";
            }
        }

        // Event Handlers
        private async void button1_Click(object sender, EventArgs e)
        {
            await ParseObjOjdAsync(); // 2072 entries
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await ParseSfxOjdAsync("SFX.ojd"); // 755 entries
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await ParseTextOjdAsync(); // 1396 Entries (0 - 1395)
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0 || listBox1.SelectedIndex >= entries.Count)
                return;

            var entry = entries[listBox1.SelectedIndex];
            textBox1.Text = entry.Id.ToString();          // ID
            textBox2.Text = entry.Type.ToString();     // Type
            textBox3.Text = entry.Length.ToString();// Length/Flags
            textBox4.Text = entry.Name;       // Path/Name
        }
    }
}
