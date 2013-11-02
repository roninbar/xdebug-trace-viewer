using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace xdebug_trace_viewer
{
    public partial class Form1 : Form
    {
        #region Construction

        public Form1(string fileName)
            : this()
        {
            parseTraceFile(fileName);
        }

        public Form1()
        {
            InitializeComponent();
        }

        #endregion

        #region WinForms Event Handlers

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openTraceFile();
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            openTraceFile();
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                e.Effect = 1 == fileNames.Length ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            Debug.Assert(1 == fileNames.Length);
            parseTraceFile(fileNames[0]);
        }

        #endregion

        #region Private Methods

        private void openTraceFile()
        {
            var dlg = new OpenFileDialog();
            if (DialogResult.OK == dlg.ShowDialog())
            {
                parseTraceFile(dlg.FileName);
            }
        }

        private void parseTraceFile(string filenName)
        {
            using (var fs = File.OpenText(filenName))
            {
                string versionLine = fs.ReadLine();
                var versionLineRegex = new Regex(@"Version: ([0-9][0-9\.]*)");
                var versionMatch = versionLineRegex.Match(versionLine);
                var version = new Version(versionMatch.Groups[1].Value);
                if (version.Major != 2)
                {
                    throw new Exception(string.Format("Version {0} is not supported.", version));
                }
                string fileFormatLine = fs.ReadLine();
                var fileFormatRegex = new Regex(@"File format: ([0-9]+)", RegexOptions.IgnoreCase);
                var fileFormatMatch = fileFormatRegex.Match(fileFormatLine);
                var fileFormatString = fileFormatMatch.Groups[1].Value;
                int fileFormat;
                if (!int.TryParse(fileFormatString, out fileFormat) || 2 != fileFormat)
                {
                    throw new Exception(string.Format("File format must be 2. {0} found.", fileFormatString));
                }
                string traceStartLine = fs.ReadLine();

                TreeNode caller = new TreeNode(filenName);
                caller.Tag = 0.0;
                treeView1.Nodes.Clear();
                treeView1.Nodes.Add(caller);
                var callStack = new List<TreeNode>();
                string recordLine;
                while (parseTraceRecord(recordLine = fs.ReadLine()))
                {
                    var fields = recordLine.Split('\t');
                    var recordType = int.Parse(fields[2]);
                    if (0 == recordType)
                    {
                        // Entry
                        int depth;
                        Debug.Assert(int.TryParse(fields[0], out depth));
                        var thisCall = new TreeNode(string.Format("{0} {1}", fields[5], fields[7]));
                        thisCall.ToolTipText = recordLine;
                        int userDefined;
                        thisCall.ForeColor = int.TryParse(fields[6], out userDefined) && 1 == userDefined ? Color.Black : Color.Red;
                        double entryTime;
                        double.TryParse(fields[3], out entryTime);
                        thisCall.Tag = entryTime;
                        caller.Nodes.Add(thisCall);
                        callStack.Push(caller);
                        Debug.Assert(depth == callStack.Count);
                        Debug.Assert(depth == thisCall.Level);
                        caller = thisCall;
                    }
                    else
                    {
                        // Exit
                        int depth;
                        Debug.Assert(int.TryParse(fields[0], out depth) && depth == callStack.Count);
                        caller = callStack.Pop();
                        double exitTime;
                        if (double.TryParse(fields[3], out exitTime))
                        {
                            caller.ToolTipText = Convert.ToString(exitTime - (double)caller.Tag);
                        }
                    }
                }
                string summaryLine = recordLine;
                string traceEndLine = fs.ReadLine();
            }
        }

        private static bool parseTraceRecord(string line)
        {
            var fields = line.Split('\t');
            int recordType, argc;
            // col[2] indicates the type of record, Entry or Exit. Entry records should have 11 fields + 1 field for each argument. Exit records should have 5 fields.
            var isValid =
                2 < fields.Length &&
                int.TryParse(fields[2], out recordType) &&
                (0 == recordType
                ? (10 < fields.Length &&
                int.TryParse(fields[10], out argc) &&
                11 + argc == fields.Length)
                : 5 == fields.Length);
            return isValid;
        }

        #endregion
    }
}
