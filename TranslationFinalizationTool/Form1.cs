using System;
using System.Text;
using System.Transactions;
using TranslationFinalizationTool.Class;
using static System.Windows.Forms.AxHost;

namespace TranslationFinalizationTool
{
    public partial class Form1 : Form
    {
        string[] files;
        private TranslationStructureClass _writeTo = new();
        private TranslationStructureClass _writeFrom = new();
        public Form1()
        {
            InitializeComponent();
        }

        private void FileButton_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new();
            ofd.Filter = @"XLIFF (*.xlf)|*.xlf|All files (*.*)|*.*";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
            }
        }

        private void DirectoryButton_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                files = Directory.GetFiles(fbd.SelectedPath);

            }
            textBox2.Text = fbd.SelectedPath;
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            ReadEvaluationFile();
            FindAllSourcesAndTargets();
            FindNeedsTranslation();
            CreateNewFile(_writeTo,textBox1.Text);
        }
        private void ReadEvaluationFile()
        {
            int minimumLength = 13;
            int iteration = 0;
            int iterationStart = 0;
            _writeTo = new TranslationStructureClass();
            if (File.ReadAllLines(textBox1.Text).Length > minimumLength)
            {
                foreach (string line in File.ReadAllLines(textBox1.Text, Encoding.Default))
                {
                    if (line.Contains("<trans-unit"))
                    {
                        _writeTo.StartLine.Add(line);
                    }
                    //If starting file has <target then save value
                    else if (line.Contains("<target"))
                    {
                        _writeTo.Target.Add(line);
                    }
                    else if (line.Contains("<source>"))
                    {
                        _writeTo.Source.Add(line);
                    }
                    else if (line.Contains("<note") && iteration == 0)
                    {
                        _writeTo.Note1.Add(line);
                        iteration = 1;
                    }
                    else if (line.Contains("<note") && iteration == 1)
                    {

                        _writeTo.Note2.Add(line);
                        iteration = 0;

                        if (_writeTo.Note2.Count > _writeTo.Target.Count)
                        {
                            _writeTo.Target.Add("          <target state=\"needs-translation\"");
                        }
                    }
                    else if (line.Contains("</trans-unit"))
                    {
                        _writeTo.EndLine.Add(line);
                    }
                    else if (iterationStart < 5)
                    {
                        _writeTo.FileStart.Add(line);
                        iterationStart++;
                    }
                    else if (line.Contains("</group>") || line.Contains("</body>") || line.Contains("</file>") || line.Contains("</xliff>"))
                    {
                        _writeTo.FileEnd.Add(line);
                    }
                }
            }
        }

        private void FindAllSourcesAndTargets()
        {
            _writeFrom = new TranslationStructureClass();
            foreach (var file in files)
            {
                int minimumLength = 13;
                if (File.ReadAllLines(file).Length > minimumLength)
                {
                    foreach (string line in File.ReadAllLines(file, Encoding.Default))
                    {
                        if (line.Contains("<target"))
                        {
                            _writeFrom.Target.Add(line);
                        }
                        else if (line.Contains("<source>"))
                        {
                            _writeFrom.Source.Add(line);
                        }
                    }
                }
            }
            MessageBox.Show(@"= Translation files uploaded");
        }

        private void FindNeedsTranslation()
        {
            int Number = 0;
            //int needsTrans = 0;
            foreach(var Entry in _writeTo.Source)
            {
                if(_writeTo.Target[Number].Contains("<target state=\"needs-translation\"/>"))
                {
                    var Temp = FindBestTarget(Entry);
                    if (!Temp.Equals(""))
                    {
                        _writeTo.Target[Number] = Temp;
                    }
                    //needsTrans++;
                }
                Number++;
            }
            //MessageBox.Show(needsTrans.ToString());
        }

        private string FindBestTarget(string Source)
        {
            List<String> AllPossibleTargets = new();
            int Index = 0;
            foreach (var SourceEntry in _writeFrom.Source)
            {
                if (Source.Equals(SourceEntry))
                {
                    if (!_writeFrom.Target[Index].Contains("needs-translation"))
                    {
                        AllPossibleTargets.Add(_writeFrom.Target[Index]);
                    }
                }
                Index++;
            }
            if (!AllPossibleTargets.Count.Equals(0))
            {
                var BestTarget = AllPossibleTargets.GroupBy(x => x).OrderByDescending(g => g.Count()).First();
                string BestString = BestTarget.Key.Insert(17, " state=\"translated\"");
                return BestString;
            }
            return "";
        }

        private static void CreateNewFile(TranslationStructureClass writeTo, string readPath)
        {
            MessageBox.Show(@"Create translated file");
            string fileName = Path.GetFileName(readPath);
            SaveFileDialog saveFileDialog1 = new()
            {
                FileName = fileName,
                Filter = @"txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StreamWriter writer = new(saveFileDialog1.OpenFile(), Encoding.Default);
                int iteration = 0;
                foreach (string item in writeTo.FileStart)
                {
                    writer.WriteLine(item);
                }
                foreach (string unused in writeTo.StartLine)
                {
                    writer.WriteLine(writeTo.StartLine[iteration]);
                    writer.WriteLine(writeTo.Source[iteration]);
                    writer.WriteLine(writeTo.Target[iteration]);
                    writer.WriteLine(writeTo.Note1[iteration]);
                    writer.WriteLine(writeTo.Note2[iteration]);
                    writer.WriteLine(writeTo.EndLine[iteration]);
                    iteration++;
                }
                foreach (string item in writeTo.FileEnd)
                {
                    writer.WriteLine(item);
                }

                writer.Dispose();
                writer.Close();
            }

        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}