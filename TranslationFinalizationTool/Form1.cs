using System;
using System.Text;
using System.Transactions;
using TranslationFinalizationTool.Class;
using static System.Windows.Forms.AxHost;
using System.Threading;
using System.Diagnostics;

namespace TranslationFinalizationTool
{
    public partial class Translations : Form
    {
        string[] files;
        private TranslationStructureClass _writeTo = new();
        private TranslationStructureClass _writeFrom = new();
        private int _progressBarMaximum;
        private Stopwatch _stopWatch;

        public Translations()
        {
            InitializeComponent();
        }

        private void SetProgressBar()
        {
            progressBar1.Maximum = _progressBarMaximum;
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Visible = true;
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
            label4.Text = @"00s 00m";
            SetProgressBar();
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            timer1.Interval = 1000;
            timer1.Enabled = true;
            timer1.Tick += Timer1_Tick;
            timer1.Start();
            Thread thread = new(FindNeedsTranslation);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            MethodInvoker labelUpdate = () => label4.Text = _stopWatch.Elapsed.Minutes.ToString() + @"m " + _stopWatch.Elapsed.Seconds.ToString() + @"s";
            label4.Invoke(labelUpdate);
            Application.DoEvents();
        }

        private void ReadEvaluationFile()
        {
            int minimumLength = 13;
            int iteration = 0;
            int iterationStart = 0;
            _progressBarMaximum = 0;
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
                        _progressBarMaximum++;
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
            MethodInvoker labelShow4 = () => label4.Visible = true;
            MethodInvoker labelShow5 = () => label5.Visible = true;
            label4.Invoke(labelShow4);
            label5.Invoke(labelShow5);
            int Number = 0;
            foreach(var Entry in _writeTo.Source)
            {
                if(_writeTo.Target[Number].Contains("<target state=\"needs-translation\"/>"))
                {
                    if (!Entry.Contains("<source></source>"))
                    {
                        var Temp = FindBestTarget(Entry);
                        if (!Temp.Equals(""))
                        {
                            _writeTo.Target[Number] = Temp;
                        }
                    }
                }
                Number++;
                MethodInvoker progressBarUp = () => progressBar1.Value++;
                progressBar1.Invoke(progressBarUp);
            }
            MethodInvoker labelHide4 = () => label4.Visible = false;
            MethodInvoker labelHide5 = () => label5.Visible = false;
            MethodInvoker progressBarVisible = () => progressBar1.Visible = false;
            progressBar1.Invoke(progressBarVisible);
            label4.Invoke(labelHide4);
            label5.Invoke(labelHide5);
            _stopWatch.Stop();
            _stopWatch.Reset();
            timer1.Stop();
            CreateNewFile(_writeTo, textBox1.Text);
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
                if (!BestTarget.Key.Contains("state=\"translated\""))
                {
                    string BestString = BestTarget.Key.Insert(17, " state=\"translated\"");
                    
                    if (BestString.Contains("          <target state=\"translated\">"))
                    {
                        return BestString;
                    }
                }
                /*if (!BestTarget.Key.Contains("          <target state=\"translated\">"))
                {
                    return BestTarget.Key.Insert(17, " state=\"translated\"");
                }*/
                return BestTarget.Key;
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

    }
}