using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Label
{
    class GUI: Form
    {
        public class Properties
        {
            public static int DefaultThreadCount = Math.Max(Environment.ProcessorCount - 1, 1);

            [Description("指定外部名字列表文件"),
             Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string NamesFilePath { get; set; }

            [Description("指定内部名字列表文件"),
             Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string LabelsFilePath { get; set; }

            [Description("指定结果输出文件"),
             Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string OutputFilePath { get; set; }

            [Description("Number of threads to use. Default is max(logical processor count - 1, 1)")]
            public int Concurrency { get; set; } = DefaultThreadCount;
        }

        public Properties Inputs
        {
            get { return (Properties)Grid.SelectedObject; }
        }

        private PropertyGrid Grid = new PropertyGrid
        {
            SelectedObject = new Properties(),
            Dock = DockStyle.Fill,
            PropertySort = PropertySort.NoSort,
            ToolbarVisible = false
        };

        private Button OkButton = new Button
        {
            Text = "OK",
            Anchor = AnchorStyles.Right
        };
        private Button CancelBtn = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel
        };
        private TableLayoutPanel ButtonRow = new TableLayoutPanel
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            RowCount = 1,
            ColumnCount = 2,
            Height = 30
        };

        private TableLayoutPanel MainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };

        public GUI()
        {
            ButtonRow.Controls.Add(OkButton);
            ButtonRow.Controls.Add(CancelBtn);
            ButtonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ButtonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            MainLayout.Controls.Add(Grid);
            MainLayout.Controls.Add(ButtonRow);
            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(MainLayout);

            Text = "Label v1.5";
            Width = 400;
            Height = 225;
            MinimumSize = new System.Drawing.Size(Width, Height);
            AcceptButton = OkButton;
            CancelButton = CancelBtn;
            OkButton.Click += OnOk;
        }

        private void OnOk(object sender, EventArgs e)
        {
            var errMsg = Validate();
            if (errMsg == null)
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show(
                    this,
                    errMsg,
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string Validate()
        {
            return
                string.IsNullOrWhiteSpace(Inputs.NamesFilePath) ? "'NamesFilePath' 未指定" :
                string.IsNullOrWhiteSpace(Inputs.OutputFilePath) ? "'OutputFilePath' 未指定" :
                !File.Exists(Inputs.NamesFilePath) ? $"指定文件 '{Inputs.NamesFilePath}' 不存在" :
                !string.IsNullOrWhiteSpace(Inputs.LabelsFilePath) && !File.Exists(Inputs.LabelsFilePath) ? $"指定文件 '{Inputs.LabelsFilePath}' 不存在" :
                (Inputs.Concurrency < 1 || Inputs.Concurrency > Environment.ProcessorCount) ? $"'Concurrency' should be within [1, {Environment.ProcessorCount}]" :
                null;
        }
    }
}
