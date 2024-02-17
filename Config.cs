using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace DSProcessManager
{
    public partial class Config : Form
    {
        public Config()
        {
            InitializeComponent();
        }

        private void Config_Load(object sender, EventArgs e)
        {
            lock (AppSettings.Default)
            {
                textBox1.Text = AppSettings.Default.setFlEXE;
                textBox2.Text = AppSettings.Default.setFLExeArgs;
                textBox3.Text = AppSettings.Default.setFLHookPassword;
                checkBoxUnicode.Checked = AppSettings.Default.setFLHookUnicode;
                comboBox1.SelectedIndex = AppSettings.Default.setRestartTime - 1;
                checkBox1.Checked = AppSettings.Default.setDailyRestart;
                textBoxWarning10min.Text = AppSettings.Default.setDailyRestartWarning10min;
                textBoxWarning5min.Text = AppSettings.Default.setDailyRestartWarning5min;
                textBoxWarning1min.Text = AppSettings.Default.setDailyRestartWarning1min;
                numericUpDown2.Value = AppSettings.Default.setRestartMaxMemory;
                numericUpDown3.Value = AppSettings.Default.setRestartMaxLoad;
                numericUpDown4.Value = AppSettings.Default.setStartupMaxReplyTime;
                numericUpDown6.Value = AppSettings.Default.setFLHookPort;
                textBox4.Text = AppSettings.Default.setDailyExtCmd1;
                comboBox2.SelectedIndex = AppSettings.Default.setDailyExtCmd1Time - 1;
                textBox5.Text = AppSettings.Default.setDailyExtCmd2;
                comboBox3.SelectedIndex = AppSettings.Default.setDailyExtCmd2Time - 1;
                textBox6.Text = AppSettings.Default.setMainLogPath;
                textBox7.Text = AppSettings.Default.setBeforeRestartExtCmd;
                textBox8.Text = AppSettings.Default.setGameLogPath;
                numericUpDown1.Value = AppSettings.Default.setGraphUpdateRate;
                numericUpDown5.Value = AppSettings.Default.setLogHighNetTrafficLimit;
                checkBox2.Checked = AppSettings.Default.setBlockHighNetTraffic;
                numericUpDown7.Value = AppSettings.Default.setNetLowPort;
                numericUpDown8.Value = AppSettings.Default.setNetHighPort;
                checkBox3.Checked = AppSettings.Default.setEnableNetMonitor;
                numericUpDown9.Value = AppSettings.Default.setDebug;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            lock (AppSettings.Default)
            {
                AppSettings.Default.setFlEXE = textBox1.Text;
                AppSettings.Default.setFLExeArgs = textBox2.Text;
                AppSettings.Default.setFLHookPassword = textBox3.Text;
                AppSettings.Default.setFLHookPort = numericUpDown6.Value;
                AppSettings.Default.setFLHookUnicode = checkBoxUnicode.Checked;
                AppSettings.Default.setRestartTime = comboBox1.SelectedIndex + 1;
                AppSettings.Default.setDailyRestart = checkBox1.Checked;
                AppSettings.Default.setDailyRestartWarning10min = textBoxWarning10min.Text;
                AppSettings.Default.setDailyRestartWarning5min = textBoxWarning5min.Text;
                AppSettings.Default.setDailyRestartWarning1min = textBoxWarning1min.Text;
                AppSettings.Default.setRestartMaxMemory = numericUpDown2.Value;
                AppSettings.Default.setRestartMaxLoad = numericUpDown3.Value;
                AppSettings.Default.setStartupMaxReplyTime = numericUpDown4.Value;
                AppSettings.Default.setDailyExtCmd1 = textBox4.Text;
                AppSettings.Default.setDailyExtCmd1Time = comboBox2.SelectedIndex + 1;
                AppSettings.Default.setDailyExtCmd2 = textBox5.Text;
                AppSettings.Default.setDailyExtCmd2Time = comboBox3.SelectedIndex + 1;
                AppSettings.Default.setMainLogPath = textBox6.Text;
                AppSettings.Default.setBeforeRestartExtCmd = textBox7.Text;
                AppSettings.Default.setGameLogPath = textBox8.Text;
                AppSettings.Default.setGraphUpdateRate = numericUpDown1.Value;
                AppSettings.Default.setLogHighNetTrafficLimit = numericUpDown5.Value;
                AppSettings.Default.setBlockHighNetTraffic = checkBox2.Checked;
                AppSettings.Default.setNetLowPort = numericUpDown7.Value;
                AppSettings.Default.setNetHighPort = numericUpDown8.Value;
                AppSettings.Default.setEnableNetMonitor = checkBox3.Checked;
                AppSettings.Default.setDebug = numericUpDown9.Value;
                AppSettings.Default.Save();
            }
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonSetPath_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = textBox1.Text;
            openFileDialog1.InitialDirectory = Path.GetDirectoryName(textBox1.Text);
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }
    }
}
