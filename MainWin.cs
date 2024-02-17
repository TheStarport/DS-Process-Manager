using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DSProcessManager
{
	
	public class MainWin : System.Windows.Forms.Form, FLHookListener, LogRecorderInterface
    {
        private IContainer components;

        /// <summary>
        /// The time at which flserver was detected running or restarted
        /// </summary>
        DateTime flServerStartTime = DateTime.Now;

        /// <summary>
        /// The number of restarts
        /// </summary>
        int flServerRestartCount = 0;

        /// <summary>
        /// The time at which the server may restart after being stopped.
        /// </summary>
        DateTime flServerCoolDownTime = DateTime.MinValue;

        /// <summary>
        /// Server statistics for the specified time
        /// </summary>
        struct DataPoint
        {
            public DataPoint(long timeInTicks, float magnitude)
            { this.timeInTicks = timeInTicks; this.magnitude = magnitude; }

            public long timeInTicks;
            
            public float magnitude;
        };

        /// <summary>
        /// Server statistic types
        /// </summary>
        enum StatsType
        {
            LOAD,
            PLAYERS,
            MEMORY,
            NPC_SPAWN
        };

        /// <summary>
        /// A list of server stats to show in a graph.
        /// </summary>
        Dictionary<StatsType, LinkedList<DataPoint>> stats = new Dictionary<StatsType, LinkedList<DataPoint>>();

        /// <summary>
        /// The possible server states.
        /// </summary>
        enum FLSERVER_STATES
        {
            DETERMINING_STATUS,
            NOT_RUNNING,
            STARTING,
            RUNNING,
            STOPPING,
        };

        /// <summary>
        /// The current server state.
        /// </summary>
        FLSERVER_STATES flServerState = FLSERVER_STATES.DETERMINING_STATUS;

        enum DAILY_RESTART_STATES
        {
            IDLE,
            WARNING_10MINS,
            WARNING_5MINS,
            WARNING_1MIN,
            RESTARTING
        };

        /// <summary>
        /// Current dialy restart state.
        /// </summary>
        DAILY_RESTART_STATES dailyRestartState = DAILY_RESTART_STATES.WARNING_10MINS;

        /// <summary>
        /// Object used to synchronise data access between main and gui threads.
        /// </summary>
        Object locker = new Object();

        /// <summary>
        /// The background FLHook comms thread.
        /// </summary>
        System.Threading.Thread flhookCommsCmdrThread = null;

        /// <summary>
        /// The current FLHook load
        /// </summary>
        int flhookLoad = 0;

        /// <summary>
        /// True if NPC spawning is enabled
        /// </summary>
        bool flhookNpcSpawnEnabled = false;

        /// <summary>
        /// True if the connection to flhook is valid.
        /// </summary>
        bool flhookConnected = false;

        /// <summary>
        /// Time at which server max exceeded allowed limit.
        /// </summary>
        DateTime lastNormalLoad = DateTime.MinValue;

        /// <summary>
        /// Time at which flhook did not reply.
        /// </summary>
        DateTime lastFLHookReply = DateTime.MinValue;
        
        /// <summary>
        /// The network monitor
        /// </summary>
        Sniffer sniffer = new Sniffer();

        private TabControl tabControl1;
        private TabPage tabPage1;
        private Label label6;
        private RichTextBox richTextBoxLog;
        private Label labelStatusLoad;
        private PictureBox pictureBoxPlayers;
        private Label labelStatusPlayers;
        private Label labelStatusNPC;
        private Label labelStatusMemory;
        private PictureBox pictureBoxMemory;
        private PictureBox pictureBoxNPC;
        private Button buttonSettings;
        private TextBox textBoxRestartCount;
        private Label label2;
        private TextBox textBoxStatus;
        private Label label1;
        private PictureBox pictureBoxStatus;
        private TabPage tabPage2;
        private NotifyIcon notifyIcon1;
        private Timer timerStatus;
        private Label label3;
        private Button buttonBan;
        private Button buttonKick;
        private DataGridView dataGridViewPlayers;
        private TabPage tabPage3;
        private RichTextBox richTextBoxEvents;
        private TabPage tabPage4;
        private DataGridView dataGridViewNetworkInfo;
        private TableData tableData;
        private BindingSource networkInfoBindingSource;
        private BindingSource playerInfoBindingSource;
        private DataGridViewTextBoxColumn colIDDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn colCharnameDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn colIPDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn colPingDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn colSystemDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn colIPDataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn colInNowDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn colIn10MinDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn colOutNowDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn colOut10MinDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;

        /// <summary>
        /// Currently online players
        /// </summary>
        Dictionary<int, FLHookSocket.PlayerInfo> flHookPlayerInfoList = new Dictionary<int, FLHookSocket.PlayerInfo>();

        FLHookEventSocket flHookLstr;

        public MainWin()
		{
			InitializeComponent();
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle19 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle20 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWin));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
            this.labelStatusLoad = new System.Windows.Forms.Label();
            this.pictureBoxPlayers = new System.Windows.Forms.PictureBox();
            this.labelStatusPlayers = new System.Windows.Forms.Label();
            this.labelStatusNPC = new System.Windows.Forms.Label();
            this.labelStatusMemory = new System.Windows.Forms.Label();
            this.pictureBoxMemory = new System.Windows.Forms.PictureBox();
            this.pictureBoxNPC = new System.Windows.Forms.PictureBox();
            this.buttonSettings = new System.Windows.Forms.Button();
            this.textBoxRestartCount = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBoxStatus = new System.Windows.Forms.PictureBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dataGridViewPlayers = new System.Windows.Forms.DataGridView();
            this.colIDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCharnameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIPDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPingDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSystemDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.playerInfoBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.tableData = new DSProcessManager.TableData();
            this.buttonBan = new System.Windows.Forms.Button();
            this.buttonKick = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.richTextBoxEvents = new System.Windows.Forms.RichTextBox();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.dataGridViewNetworkInfo = new System.Windows.Forms.DataGridView();
            this.colIPDataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInNowDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIn10MinDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOutNowDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOut10MinDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.networkInfoBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.timerStatus = new System.Windows.Forms.Timer(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPlayers)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMemory)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxNPC)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStatus)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewPlayers)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.playerInfoBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tableData)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewNetworkInfo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.networkInfoBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(428, 442);
            this.tabControl1.TabIndex = 16;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.richTextBoxLog);
            this.tabPage1.Controls.Add(this.labelStatusLoad);
            this.tabPage1.Controls.Add(this.pictureBoxPlayers);
            this.tabPage1.Controls.Add(this.labelStatusPlayers);
            this.tabPage1.Controls.Add(this.labelStatusNPC);
            this.tabPage1.Controls.Add(this.labelStatusMemory);
            this.tabPage1.Controls.Add(this.pictureBoxMemory);
            this.tabPage1.Controls.Add(this.pictureBoxNPC);
            this.tabPage1.Controls.Add(this.buttonSettings);
            this.tabPage1.Controls.Add(this.textBoxRestartCount);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.textBoxStatus);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.pictureBoxStatus);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(420, 416);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Server";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(5, 295);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(25, 13);
            this.label6.TabIndex = 28;
            this.label6.Text = "Log";
            // 
            // richTextBoxLog
            // 
            this.richTextBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxLog.Location = new System.Drawing.Point(5, 311);
            this.richTextBoxLog.Name = "richTextBoxLog";
            this.richTextBoxLog.Size = new System.Drawing.Size(403, 93);
            this.richTextBoxLog.TabIndex = 27;
            this.richTextBoxLog.Text = "";
            // 
            // labelStatusLoad
            // 
            this.labelStatusLoad.AutoSize = true;
            this.labelStatusLoad.Location = new System.Drawing.Point(5, 111);
            this.labelStatusLoad.Name = "labelStatusLoad";
            this.labelStatusLoad.Size = new System.Drawing.Size(31, 13);
            this.labelStatusLoad.TabIndex = 26;
            this.labelStatusLoad.Text = "Load";
            // 
            // pictureBoxPlayers
            // 
            this.pictureBoxPlayers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxPlayers.Location = new System.Drawing.Point(5, 57);
            this.pictureBoxPlayers.Name = "pictureBoxPlayers";
            this.pictureBoxPlayers.Size = new System.Drawing.Size(405, 50);
            this.pictureBoxPlayers.TabIndex = 29;
            this.pictureBoxPlayers.TabStop = false;
            // 
            // labelStatusPlayers
            // 
            this.labelStatusPlayers.AutoSize = true;
            this.labelStatusPlayers.Location = new System.Drawing.Point(5, 40);
            this.labelStatusPlayers.Name = "labelStatusPlayers";
            this.labelStatusPlayers.Size = new System.Drawing.Size(41, 13);
            this.labelStatusPlayers.TabIndex = 30;
            this.labelStatusPlayers.Text = "Players";
            // 
            // labelStatusNPC
            // 
            this.labelStatusNPC.AutoSize = true;
            this.labelStatusNPC.Location = new System.Drawing.Point(5, 182);
            this.labelStatusNPC.Name = "labelStatusNPC";
            this.labelStatusNPC.Size = new System.Drawing.Size(29, 13);
            this.labelStatusNPC.TabIndex = 25;
            this.labelStatusNPC.Text = "NPC";
            // 
            // labelStatusMemory
            // 
            this.labelStatusMemory.AutoSize = true;
            this.labelStatusMemory.Location = new System.Drawing.Point(5, 224);
            this.labelStatusMemory.Name = "labelStatusMemory";
            this.labelStatusMemory.Size = new System.Drawing.Size(44, 13);
            this.labelStatusMemory.TabIndex = 24;
            this.labelStatusMemory.Text = "Memory";
            // 
            // pictureBoxMemory
            // 
            this.pictureBoxMemory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxMemory.Location = new System.Drawing.Point(5, 241);
            this.pictureBoxMemory.Name = "pictureBoxMemory";
            this.pictureBoxMemory.Size = new System.Drawing.Size(405, 50);
            this.pictureBoxMemory.TabIndex = 23;
            this.pictureBoxMemory.TabStop = false;
            // 
            // pictureBoxNPC
            // 
            this.pictureBoxNPC.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxNPC.Location = new System.Drawing.Point(5, 199);
            this.pictureBoxNPC.Name = "pictureBoxNPC";
            this.pictureBoxNPC.Size = new System.Drawing.Size(405, 21);
            this.pictureBoxNPC.TabIndex = 22;
            this.pictureBoxNPC.TabStop = false;
            // 
            // buttonSettings
            // 
            this.buttonSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSettings.Location = new System.Drawing.Point(335, 12);
            this.buttonSettings.Name = "buttonSettings";
            this.buttonSettings.Size = new System.Drawing.Size(75, 23);
            this.buttonSettings.TabIndex = 21;
            this.buttonSettings.Text = "Settings";
            this.buttonSettings.UseVisualStyleBackColor = true;
            this.buttonSettings.Click += new System.EventHandler(this.buttonSettings_Click);
            // 
            // textBoxRestartCount
            // 
            this.textBoxRestartCount.Location = new System.Drawing.Point(277, 14);
            this.textBoxRestartCount.Name = "textBoxRestartCount";
            this.textBoxRestartCount.Size = new System.Drawing.Size(52, 20);
            this.textBoxRestartCount.TabIndex = 20;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(225, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 19;
            this.label2.Text = "Restarts";
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.Location = new System.Drawing.Point(50, 13);
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.Size = new System.Drawing.Size(169, 20);
            this.textBoxStatus.TabIndex = 18;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 17;
            this.label1.Text = "Status";
            // 
            // pictureBoxStatus
            // 
            this.pictureBoxStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxStatus.Location = new System.Drawing.Point(5, 128);
            this.pictureBoxStatus.Name = "pictureBoxStatus";
            this.pictureBoxStatus.Size = new System.Drawing.Size(405, 50);
            this.pictureBoxStatus.TabIndex = 16;
            this.pictureBoxStatus.TabStop = false;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dataGridViewPlayers);
            this.tabPage2.Controls.Add(this.buttonBan);
            this.tabPage2.Controls.Add(this.buttonKick);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(420, 416);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Players";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridViewPlayers
            // 
            this.dataGridViewPlayers.AllowUserToAddRows = false;
            this.dataGridViewPlayers.AllowUserToDeleteRows = false;
            this.dataGridViewPlayers.AllowUserToOrderColumns = true;
            this.dataGridViewPlayers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewPlayers.AutoGenerateColumns = false;
            this.dataGridViewPlayers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewPlayers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colIDDataGridViewTextBoxColumn,
            this.colCharnameDataGridViewTextBoxColumn,
            this.colIPDataGridViewTextBoxColumn,
            this.colPingDataGridViewTextBoxColumn,
            this.colSystemDataGridViewTextBoxColumn});
            this.dataGridViewPlayers.DataSource = this.playerInfoBindingSource;
            this.dataGridViewPlayers.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewPlayers.Name = "dataGridViewPlayers";
            this.dataGridViewPlayers.ReadOnly = true;
            this.dataGridViewPlayers.RowHeadersVisible = false;
            this.dataGridViewPlayers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewPlayers.Size = new System.Drawing.Size(408, 375);
            this.dataGridViewPlayers.TabIndex = 2;
            // 
            // colIDDataGridViewTextBoxColumn
            // 
            this.colIDDataGridViewTextBoxColumn.DataPropertyName = "ColID";
            this.colIDDataGridViewTextBoxColumn.HeaderText = "ID";
            this.colIDDataGridViewTextBoxColumn.Name = "colIDDataGridViewTextBoxColumn";
            this.colIDDataGridViewTextBoxColumn.ReadOnly = true;
            this.colIDDataGridViewTextBoxColumn.Width = 40;
            // 
            // colCharnameDataGridViewTextBoxColumn
            // 
            this.colCharnameDataGridViewTextBoxColumn.DataPropertyName = "ColCharname";
            this.colCharnameDataGridViewTextBoxColumn.HeaderText = "Charname";
            this.colCharnameDataGridViewTextBoxColumn.Name = "colCharnameDataGridViewTextBoxColumn";
            this.colCharnameDataGridViewTextBoxColumn.ReadOnly = true;
            this.colCharnameDataGridViewTextBoxColumn.Width = 120;
            // 
            // colIPDataGridViewTextBoxColumn
            // 
            this.colIPDataGridViewTextBoxColumn.DataPropertyName = "ColIP";
            this.colIPDataGridViewTextBoxColumn.HeaderText = "IP";
            this.colIPDataGridViewTextBoxColumn.Name = "colIPDataGridViewTextBoxColumn";
            this.colIPDataGridViewTextBoxColumn.ReadOnly = true;
            this.colIPDataGridViewTextBoxColumn.Width = 90;
            // 
            // colPingDataGridViewTextBoxColumn
            // 
            this.colPingDataGridViewTextBoxColumn.DataPropertyName = "ColPing";
            this.colPingDataGridViewTextBoxColumn.HeaderText = "Ping";
            this.colPingDataGridViewTextBoxColumn.Name = "colPingDataGridViewTextBoxColumn";
            this.colPingDataGridViewTextBoxColumn.ReadOnly = true;
            this.colPingDataGridViewTextBoxColumn.Width = 60;
            // 
            // colSystemDataGridViewTextBoxColumn
            // 
            this.colSystemDataGridViewTextBoxColumn.DataPropertyName = "ColSystem";
            this.colSystemDataGridViewTextBoxColumn.HeaderText = "System";
            this.colSystemDataGridViewTextBoxColumn.Name = "colSystemDataGridViewTextBoxColumn";
            this.colSystemDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // playerInfoBindingSource
            // 
            this.playerInfoBindingSource.DataMember = "PlayerInfo";
            this.playerInfoBindingSource.DataSource = this.tableData;
            // 
            // tableData
            // 
            this.tableData.DataSetName = "TableData";
            this.tableData.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // buttonBan
            // 
            this.buttonBan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBan.Location = new System.Drawing.Point(258, 387);
            this.buttonBan.Name = "buttonBan";
            this.buttonBan.Size = new System.Drawing.Size(75, 23);
            this.buttonBan.TabIndex = 1;
            this.buttonBan.Text = "Ban";
            this.buttonBan.UseVisualStyleBackColor = true;
            this.buttonBan.Click += new System.EventHandler(this.buttonBan_Click);
            // 
            // buttonKick
            // 
            this.buttonKick.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonKick.Location = new System.Drawing.Point(339, 387);
            this.buttonKick.Name = "buttonKick";
            this.buttonKick.Size = new System.Drawing.Size(75, 23);
            this.buttonKick.TabIndex = 0;
            this.buttonKick.Text = "Kick";
            this.buttonKick.UseVisualStyleBackColor = true;
            this.buttonKick.Click += new System.EventHandler(this.buttonKick_Click);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.richTextBoxEvents);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(420, 416);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Events";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // richTextBoxEvents
            // 
            this.richTextBoxEvents.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxEvents.Location = new System.Drawing.Point(6, 6);
            this.richTextBoxEvents.Name = "richTextBoxEvents";
            this.richTextBoxEvents.Size = new System.Drawing.Size(408, 404);
            this.richTextBoxEvents.TabIndex = 0;
            this.richTextBoxEvents.Text = "";
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.dataGridViewNetworkInfo);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(420, 416);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Network";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // dataGridViewNetworkInfo
            // 
            this.dataGridViewNetworkInfo.AllowUserToAddRows = false;
            this.dataGridViewNetworkInfo.AllowUserToDeleteRows = false;
            this.dataGridViewNetworkInfo.AllowUserToOrderColumns = true;
            this.dataGridViewNetworkInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewNetworkInfo.AutoGenerateColumns = false;
            this.dataGridViewNetworkInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewNetworkInfo.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colIPDataGridViewTextBoxColumn1,
            this.colInNowDataGridViewTextBoxColumn,
            this.colIn10MinDataGridViewTextBoxColumn,
            this.colOutNowDataGridViewTextBoxColumn,
            this.colOut10MinDataGridViewTextBoxColumn});
            this.dataGridViewNetworkInfo.DataSource = this.networkInfoBindingSource;
            this.dataGridViewNetworkInfo.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewNetworkInfo.Name = "dataGridViewNetworkInfo";
            this.dataGridViewNetworkInfo.ReadOnly = true;
            this.dataGridViewNetworkInfo.RowHeadersVisible = false;
            this.dataGridViewNetworkInfo.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewNetworkInfo.Size = new System.Drawing.Size(408, 407);
            this.dataGridViewNetworkInfo.TabIndex = 4;
            // 
            // colIPDataGridViewTextBoxColumn1
            // 
            this.colIPDataGridViewTextBoxColumn1.DataPropertyName = "ColIP";
            this.colIPDataGridViewTextBoxColumn1.HeaderText = "IP";
            this.colIPDataGridViewTextBoxColumn1.Name = "colIPDataGridViewTextBoxColumn1";
            this.colIPDataGridViewTextBoxColumn1.ReadOnly = true;
            this.colIPDataGridViewTextBoxColumn1.Width = 90;
            // 
            // colInNowDataGridViewTextBoxColumn
            // 
            this.colInNowDataGridViewTextBoxColumn.DataPropertyName = "ColInNow";
            dataGridViewCellStyle17.Format = "N2";
            this.colInNowDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle17;
            this.colInNowDataGridViewTextBoxColumn.HeaderText = "Inbound Now (kbit/s)";
            this.colInNowDataGridViewTextBoxColumn.Name = "colInNowDataGridViewTextBoxColumn";
            this.colInNowDataGridViewTextBoxColumn.ReadOnly = true;
            this.colInNowDataGridViewTextBoxColumn.Width = 80;
            // 
            // colIn10MinDataGridViewTextBoxColumn
            // 
            this.colIn10MinDataGridViewTextBoxColumn.DataPropertyName = "ColIn10Min";
            dataGridViewCellStyle18.Format = "N2";
            this.colIn10MinDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle18;
            this.colIn10MinDataGridViewTextBoxColumn.HeaderText = "Inbound 10 Min (KB)";
            this.colIn10MinDataGridViewTextBoxColumn.Name = "colIn10MinDataGridViewTextBoxColumn";
            this.colIn10MinDataGridViewTextBoxColumn.ReadOnly = true;
            this.colIn10MinDataGridViewTextBoxColumn.Width = 80;
            // 
            // colOutNowDataGridViewTextBoxColumn
            // 
            this.colOutNowDataGridViewTextBoxColumn.DataPropertyName = "ColOutNow";
            dataGridViewCellStyle19.Format = "N2";
            this.colOutNowDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle19;
            this.colOutNowDataGridViewTextBoxColumn.HeaderText = "Outbound Now (kbit/s)";
            this.colOutNowDataGridViewTextBoxColumn.Name = "colOutNowDataGridViewTextBoxColumn";
            this.colOutNowDataGridViewTextBoxColumn.ReadOnly = true;
            this.colOutNowDataGridViewTextBoxColumn.Width = 80;
            // 
            // colOut10MinDataGridViewTextBoxColumn
            // 
            this.colOut10MinDataGridViewTextBoxColumn.DataPropertyName = "ColOut10Min";
            dataGridViewCellStyle20.Format = "N2";
            this.colOut10MinDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle20;
            this.colOut10MinDataGridViewTextBoxColumn.HeaderText = "Outbound 10 Min (KB)";
            this.colOut10MinDataGridViewTextBoxColumn.Name = "colOut10MinDataGridViewTextBoxColumn";
            this.colOut10MinDataGridViewTextBoxColumn.ReadOnly = true;
            this.colOut10MinDataGridViewTextBoxColumn.Width = 80;
            // 
            // networkInfoBindingSource
            // 
            this.networkInfoBindingSource.DataMember = "NetworkInfo";
            this.networkInfoBindingSource.DataSource = this.tableData;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // timerStatus
            // 
            this.timerStatus.Enabled = true;
            this.timerStatus.Interval = 1000;
            this.timerStatus.Tick += new System.EventHandler(this.timerStatus_Tick);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 5.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(431, 2);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(9, 7);
            this.label3.TabIndex = 32;
            this.label3.Text = "V";
            this.label3.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "ColIP";
            this.dataGridViewTextBoxColumn1.HeaderText = "IP";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "ColIP";
            this.dataGridViewTextBoxColumn2.HeaderText = "IP";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 90;
            // 
            // MainWin
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(452, 466);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(460, 440);
            this.Name = "MainWin";
            this.Text = "DS Process Manager";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPlayers)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMemory)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxNPC)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStatus)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewPlayers)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.playerInfoBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tableData)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewNetworkInfo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.networkInfoBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            bool firstInstance;
            System.Threading.Mutex mutex = new System.Threading.Mutex(false, "Local\\FLProcessManagerSIOJOQIWHIBKAJBSJHBS", out firstInstance);
            if (firstInstance)
			    Application.Run(new MainWin());
		}

        /// <summary>
        /// Start the flhook socket on form load and run the timer tick.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void Form1_Load(object sender, System.EventArgs e)
		{
            label3.Text = "22";

            stats[StatsType.LOAD] = new LinkedList<DataPoint>();
            stats[StatsType.MEMORY] = new LinkedList<DataPoint>();
            stats[StatsType.NPC_SPAWN] = new LinkedList<DataPoint>();
            stats[StatsType.PLAYERS] = new LinkedList<DataPoint>();

            flhookCommsCmdrThread = new System.Threading.Thread(new System.Threading.ThreadStart(FLHookCommsCmdRun));
            flhookCommsCmdrThread.IsBackground = true;
            flhookCommsCmdrThread.Start();

            flHookLstr = new FLHookEventSocket(this, this);

            if (AppSettings.Default.setEnableNetMonitor)
                sniffer.StartSniffing();
            
            timerStatus_Tick(null, null);
        }

        /// <summary>
        /// Background comms command thread.
        /// </summary>
        private void FLHookCommsCmdRun()
        {
            DateTime timeForPlayerListUpdate = DateTime.Now;
            using (FLHookSocket flHookCmdr = new FLHookSocket())
            {
                while (true)
                {
                    // Request the current server state
                    int load = 0;
                    bool npcSpawnEnabled = false;
                    string upTime = "-";
                    bool connected = flHookCmdr.CmdServerInfo(out load, out npcSpawnEnabled, upTime);
                    lock (locker)
                    {
                        flhookLoad = load;
                        flhookNpcSpawnEnabled = npcSpawnEnabled;
                        flhookConnected = connected;
                    }

                    // Periodically request a player update
                    if (timeForPlayerListUpdate <= DateTime.Now && connected)
                    {
                        timeForPlayerListUpdate = DateTime.Now.AddSeconds(30);
                        Dictionary<int, FLHookSocket.PlayerInfo> playerInfoList = new Dictionary<int, FLHookSocket.PlayerInfo>();
                        flHookCmdr.CmdGetPlayers(playerInfoList);
                        lock (flHookPlayerInfoList)
                        {
                            flHookPlayerInfoList = playerInfoList;
                        }
                    }

                    System.Threading.Thread.Sleep(3000);
                }
            }
        }

        /// <summary>
        /// Try to parse an event from flhook socket
        /// </summary>
        public void ReceiveFLHookEvent(string type, string[] keys, string[] values, string eventLine)
        {
            if (keys.Length > 0)
            {
                if (type == "chat")
                {

                    string from = values[1];
                    int id = Convert.ToInt32(values[2]);
                    string chattype = values[3];
                    string to = "";
                    string text = eventLine.Substring(eventLine.IndexOf(" text=") + 6); ;

                    if (chattype == "system")
                    {
                        lock (flHookPlayerInfoList)
                        {
                            if (flHookPlayerInfoList.ContainsKey(id))
                                to = flHookPlayerInfoList[id].system;
                        }
                    }
                    else if (chattype == "player")
                    {
                        to = values[4];
                    }

                    LogGameEvent(String.Format("{0} {1} {2}->{3}: {4}",
                           DateTime.Now.ToShortDateString(),
                           DateTime.Now.ToLongTimeString(),
                           from, to, text));
                }
                else if (type == "disconnect")
                {
                    // disconnect char=? id=? - clear everything
                    int id = Convert.ToInt32(values[2]);

                    lock (flHookPlayerInfoList)
                    {
                        FLHookSocket.PlayerInfo playerInfo = new FLHookSocket.PlayerInfo();
                        playerInfo.charname = "";
                        playerInfo.ip = "";
                        playerInfo.ping = 0;
                        playerInfo.system = "";
                        flHookPlayerInfoList[id] = playerInfo;
                    }
                }
                else if (type == "login")
                {
                    // login char=? accountdirname=? id=? ip=? - set id, ip and charname clear everything else
                    int id = Convert.ToInt32(values[3]);
                    string charname = values[1];
                    string ip = values[4];

                    lock (flHookPlayerInfoList)
                    {
                        FLHookSocket.PlayerInfo playerInfo = new FLHookSocket.PlayerInfo();
                        if (flHookPlayerInfoList.ContainsKey(id))
                            playerInfo = flHookPlayerInfoList[id];
                        playerInfo.charname = charname;
                        playerInfo.ip = ip;
                        flHookPlayerInfoList[id] = playerInfo;
                    }
                }
                else if (type == "baseenter")
                {
                    // baseenter char=? id=? base=? system=? - update don't clear
                    int id = Convert.ToInt32(values[2]);
                    string systemNick = values[4];

                    lock (flHookPlayerInfoList)
                    {
                        FLHookSocket.PlayerInfo playerInfo = new FLHookSocket.PlayerInfo();
                        if (flHookPlayerInfoList.ContainsKey(id))
                            playerInfo = flHookPlayerInfoList[id];
                        playerInfo.system = systemNick;
                        flHookPlayerInfoList[id] = playerInfo;
                    }
                }
                else if (type == "launch")
                {
                    // launch char=? id=1 base=? system=? - update don't clear
                    int id = Convert.ToInt32(values[2]);
                    string systemNick = values[4];

                    lock (flHookPlayerInfoList)
                    {
                        FLHookSocket.PlayerInfo playerInfo = new FLHookSocket.PlayerInfo();
                        if (flHookPlayerInfoList.ContainsKey(id))
                            playerInfo = flHookPlayerInfoList[id];
                        playerInfo.system = systemNick;
                        flHookPlayerInfoList[id] = playerInfo;
                    }
                }
                else if (type == "spawn")
                {
                    // spawn char=? id=? system=? - update don't clear
                    int id = Convert.ToInt32(values[2]);
                    string systemNick = values[3];

                    lock (flHookPlayerInfoList)
                    {
                        FLHookSocket.PlayerInfo playerInfo = new FLHookSocket.PlayerInfo();
                        if (flHookPlayerInfoList.ContainsKey(id))
                            playerInfo = flHookPlayerInfoList[id];
                        playerInfo.system = systemNick;
                        flHookPlayerInfoList[id] = playerInfo;
                    }
                }
                else if (type == "kill")
                {
                    // Ignore this.
                }
            }

        }

        bool extCmd1Executed = false;
        bool extCmd2Executed = false;

        /// <summary>
        /// Execute external commands on a timer. This needs to be rewritten and is
        /// more than a little hacky.
        /// </summary>
        void ExecuteExtCommandsIfNecessary()
        {
            DateTime now = DateTime.Now;

            if (AppSettings.Default.setDailyExtCmd1.Length > 0)
            {
                DateTime extCmdTime1 = new DateTime(now.Year, now.Month, now.Day, 0, 0, 55).AddHours(AppSettings.Default.setDailyExtCmd1Time);
                if ((int)extCmdTime1.Subtract(now).TotalMinutes == 0)
                {
                    if (!extCmd1Executed)
                    {
                        extCmd1Executed = true;
                        try
                        {
                            AddLog(String.Format("Executing external command {0}", AppSettings.Default.setDailyExtCmd1));
                            Process.Start(AppSettings.Default.setDailyExtCmd1);
                        }
                        catch (Exception ex)
                        {
                            AddLog("Executing external command failed: " + ex.Message);
                        }
                    }
                }
                else
                {
                    extCmd1Executed = false;
                }
            }

            if (AppSettings.Default.setDailyExtCmd2.Length > 0)
            {
                DateTime extCmdTime2 = new DateTime(now.Year, now.Month, now.Day, 0, 0, 55).AddHours(AppSettings.Default.setDailyExtCmd2Time);
                if ((int)extCmdTime2.Subtract(now).TotalMinutes == 0)
                {
                    if (!extCmd2Executed)
                    {
                        extCmd2Executed = true;
                        try
                        {
                            AddLog(String.Format("Executing external command {0}", AppSettings.Default.setDailyExtCmd2));
                            Process.Start(AppSettings.Default.setDailyExtCmd2);
                        }
                        catch (Exception ex)
                        {
                            AddLog("Executing external command failed: " + ex.Message);
                        }
                    }
                }
                else
                {
                    extCmd2Executed = false;
                }
            }
        }

        /// <summary>
        /// Execute daily restart if needed. Will send warning messages too.
        /// </summary>
        void ExecuteDailyRestartIfNecessary()
        {
            DateTime now = DateTime.Now;

            // If it is approaching daily restart time notify players.
            if (AppSettings.Default.setDailyRestart)
            {
                DateTime restartTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 55).AddHours(AppSettings.Default.setRestartTime);
                int minsToRestart = (int)restartTime.Subtract(now).TotalMinutes;
                if (minsToRestart == 0 && dailyRestartState != DAILY_RESTART_STATES.RESTARTING)
                {
                    dailyRestartState = DAILY_RESTART_STATES.RESTARTING;
                    TryToStopServer("Killing server for daily restart");
                    return;
                }
                else if (minsToRestart == 10 && dailyRestartState != DAILY_RESTART_STATES.WARNING_10MINS)
                {
                    AddLog(String.Format("Daily restart in {0} mins", minsToRestart));
                    dailyRestartState = DAILY_RESTART_STATES.WARNING_10MINS;
                    if (AppSettings.Default.setDailyRestartWarning10min.Length > 0)
                    {
                        using (FLHookSocket flCmd = new FLHookSocket())
                            flCmd.CmdMsgU(AppSettings.Default.setDailyRestartWarning10min);
                    }
                }
                else if (minsToRestart == 5 && dailyRestartState != DAILY_RESTART_STATES.WARNING_5MINS)
                {
                    AddLog(String.Format("Daily restart in {0} mins", minsToRestart));
                    dailyRestartState = DAILY_RESTART_STATES.WARNING_5MINS;
                    if (AppSettings.Default.setDailyRestartWarning5min.Length > 0)
                    {
                        using (FLHookSocket flCmd = new FLHookSocket())
                            flCmd.CmdMsgU(AppSettings.Default.setDailyRestartWarning5min);
                    }
                }
                else if (minsToRestart == 1 && dailyRestartState != DAILY_RESTART_STATES.WARNING_1MIN)
                {
                    AddLog(String.Format("Daily restart in 1 min"));
                    dailyRestartState = DAILY_RESTART_STATES.WARNING_1MIN;
                    if (AppSettings.Default.setDailyRestartWarning1min.Length > 0)
                    {
                        using (FLHookSocket flCmd = new FLHookSocket())
                            flCmd.CmdMsgU(AppSettings.Default.setDailyRestartWarning1min);
                    }
                }
                else if (minsToRestart < 0 || minsToRestart > 10)
                {
                    dailyRestartState = DAILY_RESTART_STATES.IDLE;
                }
            }
        }

        /// <summary>
        /// Find the flserver process if it is running.
        /// </summary>
        /// <returns></returns>
        Process FindFLServerProcess()
        {
            foreach (Process p in Process.GetProcesses(Environment.MachineName))
            {
                try
                {
                    if (!p.HasExited && AppSettings.Default.setFlEXE.ToLowerInvariant()
                        == p.MainModule.FileName.ToLowerInvariant())
                    {
                        return p;
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Main processing loop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerStatus_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            ExecuteExtCommandsIfNecessary();

            float load;
            float npcSpawnEnabled;
            bool isConnected;
            lock(locker)
            {
                // Get status from server via FLHook
                load = flhookLoad;
                npcSpawnEnabled = flhookNpcSpawnEnabled ? 1.0f : 0.0f;
                isConnected = flhookConnected;
            }

            // If the FLServer error screen is open then close it.
            Dictionary<IntPtr, string> desktopWindows = GetDesktopWindows();
            foreach (KeyValuePair<IntPtr, string> window in desktopWindows)
            {
                if (window.Value.Equals("Freelancer-Server"))
                {
                    AddLog("Closing server error window ");
                    _EndTask(window.Key, false, true);
                }
                if (window.Value.Equals("FLServer.exe"))
                {
                    AddLog("Closing server error window ");
                    _EndTask(window.Key, false, true);
                }
            }

            // Get the process memory.
            float memory = 0;
            Process flserverProcess = FindFLServerProcess();
            if (flserverProcess != null)
                memory = (float)(flserverProcess.VirtualMemorySize64 / (1024 * 1024));

            // Get the number of players
            float players = 0;
            lock (flHookPlayerInfoList)
            {
                players = flHookPlayerInfoList.Count;
            }

            // Remove old statistics.
            int numStats = pictureBoxStatus.Size.Width * (int)AppSettings.Default.setGraphUpdateRate * 2;
            if (numStats < 2000)
                numStats = 2000;

            while (stats[StatsType.LOAD].Count > numStats)
                stats[StatsType.LOAD].RemoveLast();
            while (stats[StatsType.MEMORY].Count > numStats)
                stats[StatsType.MEMORY].RemoveLast();
            while (stats[StatsType.NPC_SPAWN].Count > numStats)
                stats[StatsType.NPC_SPAWN].RemoveLast();
            while (stats[StatsType.PLAYERS].Count > numStats)
                stats[StatsType.PLAYERS].RemoveLast();

            // Save the new server statistics
            stats[StatsType.LOAD].AddFirst(new DataPoint(now.Ticks, load ));
            stats[StatsType.MEMORY].AddFirst(new DataPoint(now.Ticks, memory));
            stats[StatsType.NPC_SPAWN].AddFirst(new DataPoint(now.Ticks, npcSpawnEnabled));
            lock (flHookPlayerInfoList)
            {
                stats[StatsType.PLAYERS].AddFirst(new DataPoint(now.Ticks, flHookPlayerInfoList.Count));
            }

            // Update the player and network lists
            UpdatePlayerInfoList();
            UpdateNetworkInfoList();

            // Update the textual status
            textBoxStatus.Text = "-";
            switch (flServerState)
            {
                case FLSERVER_STATES.NOT_RUNNING:
                    textBoxStatus.Text = "Not Running";
                    break;
                case FLSERVER_STATES.STARTING:
                    textBoxStatus.Text = "Starting";
                    break;
                case FLSERVER_STATES.RUNNING:
                    textBoxStatus.Text = "Running";
                    break;
            }
            labelStatusPlayers.Text = String.Format("Players - {0}", players);
            labelStatusLoad.Text = String.Format("Load - {0} ms", load);
            labelStatusNPC.Text = String.Format("NPC - {0}", npcSpawnEnabled==1.0f ? "On" : "Off");
            labelStatusMemory.Text = String.Format("Mem - {0} MB", memory);
            textBoxRestartCount.Text = flServerRestartCount.ToString();

            notifyIcon1.Text = String.Format("FLServer\nStatus: {0}\nLoad: {1} msec\nMemory: {2} MB",
                textBoxStatus.Text, load, memory);

            // Update the load graph. Each pixel represents 10 seconds.
            if (this.Visible && pictureBoxStatus.Size.Width > 0)
            {
                Bitmap bm = new Bitmap(pictureBoxStatus.Size.Width, pictureBoxStatus.Size.Height);
                Graphics g = Graphics.FromImage(bm);
                DrawBackground(g, bm);
                DrawGraph(g, bm, 100, stats[StatsType.LOAD], " ms");
                pictureBoxStatus.Image = bm;

                Bitmap bm1 = new Bitmap(pictureBoxNPC.Size.Width, pictureBoxNPC.Size.Height);
                Graphics g1 = Graphics.FromImage(bm1);
                DrawBackground(g1, bm1);
                DrawOnOffGraph(g1, bm1, stats[StatsType.NPC_SPAWN]);
                pictureBoxNPC.Image = bm1;

                Bitmap bm2 = new Bitmap(pictureBoxMemory.Size.Width, pictureBoxMemory.Size.Height);
                Graphics g2 = Graphics.FromImage(bm2);
                DrawBackground(g2, bm2);
                DrawGraph(g2, bm2, 400, stats[StatsType.MEMORY], " MB");
                pictureBoxMemory.Image = bm2;

                Bitmap bm3 = new Bitmap(pictureBoxPlayers.Size.Width, pictureBoxPlayers.Size.Height);
                Graphics g3 = Graphics.FromImage(bm3);
                DrawBackground(g3, bm3);
                DrawGraph(g3, bm3, 20, stats[StatsType.PLAYERS], "");
                pictureBoxPlayers.Image = bm3;
            }

            // Update the connected flag so that the connection state doesn't change
            // to failed until 20 seconds have passed.
            if (isConnected)
            {
                lastFLHookReply = DateTime.Now;
            }
            else if (AppSettings.Default.setFLHookPort > 0 && !isConnected && lastFLHookReply != DateTime.MinValue)
            {
                if (lastFLHookReply.AddSeconds(20) > DateTime.Now)
                    isConnected = true;
            }

            // Determine if the server is overloaded for longer than 20 seconds.
            bool isOverLoadLimit = false;
            if (load <= (float)AppSettings.Default.setRestartMaxLoad)
            {
                lastNormalLoad = DateTime.Now;
            }
            else if (load > (float)AppSettings.Default.setRestartMaxLoad)
            {
                if (lastNormalLoad.AddSeconds(20) < DateTime.Now)
                    isOverLoadLimit = true;
            }

            // Update the server state if needed.
            switch (flServerState)
            {
                case FLSERVER_STATES.DETERMINING_STATUS:
                    // As the name suggests figure out the current state.
                    if (flserverProcess == null)
                    {
                        AddLog("Server is not running");
                        flServerState = FLSERVER_STATES.NOT_RUNNING;
                    }
                    else if (isConnected)
                    {
                        AddLog("Server is running: connected to flhook");
                        flServerState = FLSERVER_STATES.RUNNING;
                    }
                    else
                    {
                        AddLog("Server is running: waiting for connection to flhook");
                        flServerState = FLSERVER_STATES.STARTING;
                    }
                    break;

                case FLSERVER_STATES.NOT_RUNNING:
                    // If the FLServer process is running then change to the starting state.
                    lastFLHookReply = DateTime.MinValue;
                    lastNormalLoad = DateTime.MinValue;

                    if (flserverProcess != null)
                    {
                        if (AppSettings.Default.setFLHookPort > 0)
                        {
                            AddLog("Server is starting: waiting for connection to flhook");
                            flServerState = FLSERVER_STATES.STARTING;
                        }
                        else
                        {
                            AddLog("Server is running: connection to flhook disabled");
                            flServerState = FLSERVER_STATES.RUNNING;
                        }
                    }
                    // If the FLServer process is not running then start it.
                    // If no cool down time is set then set one
                    else if (flServerCoolDownTime == DateTime.MinValue)
                    {
                        flServerCoolDownTime = now.AddSeconds(20);
                    }
                    // If the cool down time has expired then start the server
                    else if (flServerCoolDownTime < now)
                    {
                        TryToStartServer();
                    }

                    break;

                case FLSERVER_STATES.STARTING:
                    // The server is starting up in this state but flhook comms
                    // are not running.
                    if (flserverProcess == null)
                    {
                        AddLog("Server is not running");
                        flServerState = FLSERVER_STATES.NOT_RUNNING;
                    }
                    else if (isConnected)
                    {
                        AddLog("Server is running: connected to flhook");
                        flServerState = FLSERVER_STATES.RUNNING;
                    }
                    // Kill the server if the restart is taking to long.
                    else if (DateTime.Now.Subtract(flServerStartTime).TotalSeconds > (float)AppSettings.Default.setStartupMaxReplyTime)
                    {
                        TryToStopServer(String.Format("Killing server due to no flhook response during startup [Time={0:N0} Allowed={1}]",
                                    DateTime.Now.Subtract(flServerStartTime).TotalSeconds,
                                    AppSettings.Default.setStartupMaxReplyTime));
                    }
                    // Kill the server if the memory limit is exceeded
                    else if (memory > (float)AppSettings.Default.setRestartMaxMemory)
                    {
                        TryToStopServer(String.Format("Killing server due to high memory [Current={0} Allowed={1}]",
                                memory, AppSettings.Default.setRestartMaxMemory));
                    }

                    break;

                case FLSERVER_STATES.RUNNING:
                    // If the server is dead then change to the stopping state.
                    if (flserverProcess == null)
                    {
                        flServerState = FLSERVER_STATES.STOPPING;
                    }
                    // Kill FLServer if flhook does not respond.
                    else if (AppSettings.Default.setFLHookPort > 0 && !isConnected)
                    {
                        TryToStopServer("Killing server due to no flhook response");
                    }
                    // Kill FLServer if the load exceeds the limit.
                    else if (isOverLoadLimit)
                    {
                        TryToStopServer(String.Format("Killing server due to high load [Current={0} Allowed={1}]",
                                    load, AppSettings.Default.setRestartMaxLoad));
                    }
                    // Kill the server if the memory limit is exceeded.
                    else if (memory > (float)AppSettings.Default.setRestartMaxMemory)
                    {
                        TryToStopServer(String.Format("Killing server due to high memory [Current={0} Allowed={1}]",
                                memory, AppSettings.Default.setRestartMaxMemory));
                    }
                    else
                    {
                        // Ensure that FLServer is running at a slightly higher priority
                        flserverProcess.PriorityClass = ProcessPriorityClass.High;

                        // Restart for maintenance.
                        ExecuteDailyRestartIfNecessary();
                    }

                    break;

                case FLSERVER_STATES.STOPPING:
                    if (flserverProcess == null)
                    {
                        AddLog("Server is not running");
                        flServerState = FLSERVER_STATES.NOT_RUNNING;
                    }
                    else
                    {
                        TryToStopServer("Stopping server");
                    }
                    break;
            }
        }

        /// <summary>
        /// Start the server. This will do nothing if a start/stop server operation is in progress.
        /// </summary>
        private void TryToStartServer()
        {
            if (bgwkrFLStarterStopper == null)
            {
                flServerStartTime = DateTime.Now;
                flServerCoolDownTime = DateTime.MinValue;
                flServerRestartCount++;

                bgwkrFLStarterStopper = new BackgroundWorker();
                bgwkrFLStarterStopper.DoWork += new DoWorkEventHandler(StartServerImpl);
                bgwkrFLStarterStopper.RunWorkerCompleted += new RunWorkerCompletedEventHandler(StartStopServerFinished);
                bgwkrFLStarterStopper.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Stop the server. This will do nothing if a start/stop server operation is in progress.
        /// </summary>
        private void TryToStopServer(string msg)
        {
            if (bgwkrFLStarterStopper == null)
            {
                AddLog(msg);
                bgwkrFLStarterStopper = new BackgroundWorker();
                bgwkrFLStarterStopper.DoWork += new DoWorkEventHandler(StopServerImpl);
                bgwkrFLStarterStopper.RunWorkerCompleted += new RunWorkerCompletedEventHandler(StartStopServerFinished);
                bgwkrFLStarterStopper.RunWorkerAsync();
            }
        }

        /// <summary>
        /// The background worker used to start and stop the 
        /// </summary>
        private BackgroundWorker bgwkrFLStarterStopper = null;

        /// <summary>
        /// Background worker to start the server. This will wait until the process starts its
        /// idle loop before returning.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartServerImpl(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (AppSettings.Default.setBeforeRestartExtCmd.Length > 0)
                {
                    AddLog(String.Format("Executing external command {0}", AppSettings.Default.setBeforeRestartExtCmd));
                    Process extCmd = Process.Start(AppSettings.Default.setBeforeRestartExtCmd);
                    extCmd.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                AddLog("Executing external command failed: " + ex.Message);
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = AppSettings.Default.setFlEXE;
                startInfo.Arguments = AppSettings.Default.setFLExeArgs;
                startInfo.WorkingDirectory = Path.GetDirectoryName(AppSettings.Default.setFlEXE);
                AddLog(String.Format("Starting server {0} {1}",
                            new Object[] { startInfo.FileName, startInfo.Arguments }));
                Process flserver = Process.Start(startInfo);
                flserver.WaitForInputIdle(60000);
            }
            catch (Exception ex)
            {
                AddLog("Starting server failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Background worker to stop the server. It will request a normal shutdown and
        /// then force a shutdown if that fails.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopServerImpl(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (Process flserverProcess = FindFLServerProcess())
                {
                    if (flserverProcess != null && !e.Cancel)
                    {
                        Dictionary<IntPtr, string> desktopWindows = GetDesktopWindows();
                        foreach (KeyValuePair<IntPtr, string> window in desktopWindows)
                        {
                            if (window.Value.StartsWith("FLServer - Version "))
                            {
                                AddLog(String.Format("Requesting server shutdown"));
                                _EndTask(window.Key, false, false);
                                flserverProcess.WaitForExit(30000);
                            }
                        }                      
                    }
                }

                using (Process flserverProcess = FindFLServerProcess())
                {
                    if (flserverProcess != null && !e.Cancel)
                    {
                        AddLog(String.Format("Killing server"));
                        flserverProcess.Kill();
                        flserverProcess.WaitForExit(30000);
                    }
                }

                using (Process flserverProcess = FindFLServerProcess())
                {
                    if (flserverProcess != null && !e.Cancel)
                    {
                        AddLog(String.Format("Stopping server failed"));
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog("Stopping server failed: " + ex.Message);
                e.Result = null;
            }
        }

        /// <summary>
        /// Called when start or stop server backgroun worker threads complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartStopServerFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            bgwkrFLStarterStopper = null;
        }

        /// <summary>
        /// Add an entry to the on screen log. This is thread safe and the actual
        /// logging is done by UpdateUIAddLog
        /// </summary>
        /// <param name="msg"></param>

        /// <summary>
        /// Draw the background for a graph
        /// </summary>
        /// <param name="g"></param>
        /// <param name="bm"></param>
        private void DrawBackground(Graphics g, Bitmap bm)
        {
            g.Clear(Color.Black);
            Pen gpen = new Pen(Color.Gray, 1);
            int offset = 12 - (((int)DateTime.Now.TimeOfDay.TotalSeconds) / (int)AppSettings.Default.setGraphUpdateRate) % 12;
            g.DrawLine(gpen, 60, 0, 60, bm.Height);
            for (int x = bm.Width + offset; x > 60; x -= 10)
                g.DrawLine(gpen, x, 0, x, bm.Height);
            for (int y = 0; y < bm.Height; y += 20)
                g.DrawLine(gpen, 60, y, bm.Width, y);
        }

        /// <summary>
        /// Draw the load graph
        /// </summary>
        /// <param name="g"></param>
        /// <param name="bm"></param>
        private void DrawGraph(Graphics g, Bitmap bm, float defaultMaxMagnitude, LinkedList<DataPoint> data, string type)
        {
            // Find largest load in data
            float maxMagnitude = defaultMaxMagnitude;
            foreach (DataPoint dp in data)
            {
                if (dp.magnitude > (maxMagnitude * 0.8))
                    maxMagnitude += defaultMaxMagnitude;
            }
            float scalingFactor = bm.Height / maxMagnitude;

            // Draw key
            Brush b = Brushes.LawnGreen;
            Font f = new Font(Font.SystemFontName, 7f);
            g.DrawString(maxMagnitude.ToString() + type, f, b, new PointF(10, 2));
            g.DrawString((maxMagnitude / 2).ToString() + type, f, b, new PointF(10, ((bm.Height - f.Height) / 2)));
            g.DrawString("0" + type, f, b, new PointF(10, bm.Height - f.Height - 2));

            // Draw the graph based on load date until we either run out of date or 
            long endTimeInTicks = DateTime.Now.Subtract(new TimeSpan(0, 0, (bm.Width - 60) * (int)AppSettings.Default.setGraphUpdateRate)).Ticks;
            Pen penData = new Pen(Color.LawnGreen, 1);
            Pen penNoData = new Pen(Color.Gray, 1);

            float lastX = bm.Width;
            float lastY = bm.Height - 1;
            bool isFirst = true;
            foreach (DataPoint dp in data)
            {
                if (dp.timeInTicks < endTimeInTicks)
                    break;

                float x = 60 + (float)new TimeSpan(dp.timeInTicks - endTimeInTicks).TotalSeconds / (int)AppSettings.Default.setGraphUpdateRate;
                float y = bm.Height - (dp.magnitude * scalingFactor) - 1;
                if (y > bm.Height)
                    y = bm.Height;

                // If there's a gap in the data then fill it in.
                if (Math.Abs(x - lastX) > 2)
                {
                    g.DrawLine(penData, lastX, bm.Height - 2, lastX, lastY);
                    g.DrawLine(penNoData, x, bm.Height - 2, lastX, bm.Height - 1);
                    lastX = x;
                    lastY = bm.Height - 1;
                }

                if (!isFirst)
                    g.DrawLine(penData, x, y, lastX, lastY);

                lastX = x;
                lastY = y;
                isFirst = false;
            }
        }

        private void DrawOnOffGraph(Graphics g, Bitmap bm, LinkedList<DataPoint> data)
        {
            // Draw key
            Brush bOn = Brushes.LawnGreen;
            Brush bOff = Brushes.IndianRed;
            Font f = new Font(Font.SystemFontName, 7f);
            g.DrawString("On", f, bOn, new PointF(10, ((bm.Height - f.Height) / 2)));

            long endTimeInTicks = DateTime.Now.Subtract(new TimeSpan(0, 0, (bm.Width - 60) * (int)AppSettings.Default.setGraphUpdateRate)).Ticks;
            Pen penOn = new Pen(Color.LawnGreen, 10);
            Pen penOff = new Pen(Color.IndianRed, 10);
            Pen penNoData = new Pen(Color.Gray, 1);

            float lastX = bm.Width;
            bool isFirst = true;
            foreach (DataPoint dp in data)
            {
                if (dp.timeInTicks < endTimeInTicks)
                    break;

                float x = 60 + (float)new TimeSpan(dp.timeInTicks - endTimeInTicks).TotalSeconds / (int)AppSettings.Default.setGraphUpdateRate;
                float y = bm.Height / 2;

                // If there's a gap in the data then fill it in.
                if (Math.Abs(x - lastX) > 2)
                {
                    g.DrawLine(penNoData, x, y, lastX, y);
                    lastX = x;
                }

                if (!isFirst)
                {
                    if (dp.magnitude>0)
                        g.DrawLine(penOn, x, y, lastX, y);
                    else
                        g.DrawLine(penOff, x, y, lastX, y);
                }

                lastX = x;
                isFirst = false;
            }
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            new Config().ShowDialog(this);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
                Hide();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.Visible = false;
            flhookCommsCmdrThread.Abort();
            flHookLstr.Dispose();
        }

        private static Dictionary<IntPtr, string> mWindows;

        private delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        [DllImport("user32.dll", EntryPoint = "EndTask",
         ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool _EndTask(IntPtr hWnd, bool fShutDown, bool fForce);
        
        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
         ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool _EnumDesktopWindows(IntPtr hDesktop,
        EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowText",
         ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int _GetWindowText(IntPtr hWnd,
        StringBuilder lpWindowText, int nMaxCount);

        /// <summary>
        /// Call back from desktop window enum request.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static bool EnumWindowsProc(IntPtr hWnd, int lParam)
        {
            StringBuilder title = new StringBuilder(255);
            int titleLength = _GetWindowText(hWnd, title, title.Capacity + 1);
            title.Length = titleLength;
            mWindows.Add(hWnd, title.ToString());
            return true;
        }

        /// <summary>
        /// Returns the caption of all desktop windows.
        /// </summary>
        public static Dictionary<IntPtr, string> GetDesktopWindows()
        {
            mWindows = new Dictionary<IntPtr, string>();
            EnumDelegate enumfunc = new EnumDelegate(EnumWindowsProc);
            _EnumDesktopWindows(IntPtr.Zero, enumfunc, IntPtr.Zero);
            return mWindows;
        }

        /// <summary>
        /// Update the player list.
        /// </summary>
        private void UpdatePlayerInfoList()
        {
            int offset = dataGridViewPlayers.FirstDisplayedScrollingRowIndex;
            List<TableData.PlayerInfoRow> pendingDelete = new List<TableData.PlayerInfoRow>();

            lock (flHookPlayerInfoList)
            {
                // Add or update items in the list.
                foreach (KeyValuePair<int, FLHookSocket.PlayerInfo> kvp in flHookPlayerInfoList)
                {
                    TableData.PlayerInfoRow row = tableData.PlayerInfo.FindByColID(kvp.Key);
                    if (row == null)
                    {
                        tableData.PlayerInfo.AddPlayerInfoRow(kvp.Key,
                            kvp.Value.charname, kvp.Value.ip, kvp.Value.ping, kvp.Value.system);
                    }
                    else
                    {
                        row.ColCharname = kvp.Value.charname;
                        row.ColIP = kvp.Value.ip;
                        row.ColPing = kvp.Value.ping;
                        row.ColSystem = kvp.Value.system;    
                    }
                }

                // Delete items that have been removed
                foreach (TableData.PlayerInfoRow row in tableData.PlayerInfo.Rows)
                {
                    if (!flHookPlayerInfoList.ContainsKey(row.ColID))
                        pendingDelete.Add(row);
                }
            }

            foreach (TableData.PlayerInfoRow row in pendingDelete)
            {
                row.Delete();
            }


            if (offset >= 0 && offset < dataGridViewPlayers.Rows.Count)
                dataGridViewPlayers.FirstDisplayedScrollingRowIndex = offset;

        }

        /// <summary>
        /// Update the network info.
        /// </summary>
        private void UpdateNetworkInfoList()
        {
            Dictionary<System.Net.IPAddress, Sniffer.TrafficInformation> summary = sniffer.GetDataSummary();

            int offset = dataGridViewNetworkInfo.FirstDisplayedScrollingRowIndex;

            List<TableData.NetworkInfoRow> pendingDelete = new List<TableData.NetworkInfoRow>();

            // Add or update items in the list.
            foreach (KeyValuePair<System.Net.IPAddress, Sniffer.TrafficInformation> kvp in summary)
            {
                double rxRateNow = (kvp.Value.rxBytes10sec * 8) / 1024.0f / 10.0f;
                double rxCount10Mins = kvp.Value.rxBytes10min / 1024.0f;
                double txRateNow = (kvp.Value.txBytes10sec * 8) / 1024.0f / 10.0f;
                double txCount10Mins = kvp.Value.txBytes10min / 1024.0f;

                TableData.NetworkInfoRow row = tableData.NetworkInfo.FindByColIP(kvp.Key);
                if (row == null)
                {
                    row = tableData.NetworkInfo.AddNetworkInfoRow(kvp.Key,
                       rxRateNow, rxCount10Mins, txRateNow, txCount10Mins);
                }
                else
                {
                    row.ColInNow = rxRateNow;
                    row.ColIn10Min = rxCount10Mins;
                    row.ColOutNow = txRateNow;
                    row.ColOut10Min = txCount10Mins;
                }

                // If incoming traffic exceeded the allowed limit, taking into account in the
                // number of players on this IP then log and optionally temporarily block the
                // IP.
                if (row.ColInNow > (double)AppSettings.Default.setLogHighNetTrafficLimit)
                {
                    int allowedLimit = 0;
                    lock (flHookPlayerInfoList)
                    {
                        foreach (FLHookSocket.PlayerInfo player in flHookPlayerInfoList.Values)
                        {
                            if (player.ip == row.ColIP.ToString())
                            {
                                allowedLimit += (int)AppSettings.Default.setLogHighNetTrafficLimit;
                            }
                        }
                    }
                    if (allowedLimit == 0)
                        allowedLimit = (int)AppSettings.Default.setLogHighNetTrafficLimit;

                    if (row.ColInNow > allowedLimit)
                    {
                        AddLog("High-traffic from " + row.ColIP.ToString().PadRight(17) + "\n"
                                + "\tInbound rate " + row.ColInNow.ToString("N2") + " kb/s "
                                + "\tInbound (10 mins) " + row.ColIn10Min.ToString("N2") + " KB "
                                + "\tOutbound rate " + row.ColOutNow.ToString("N2") + " kb/s "
                                + "\tOutbound (10 mins) " + row.ColOut10Min.ToString("N2") + " KB ");

                        if (AppSettings.Default.setBlockHighNetTraffic)
                        {
                            // networkFilter.AddTempBan();
                        }
                    }
                }
            }

            // Delete items that have been removed
            foreach (TableData.NetworkInfoRow row in tableData.NetworkInfo.Rows)
            {
                if (!summary.ContainsKey((System.Net.IPAddress)row.ColIP))
                    pendingDelete.Add(row);
            }
            foreach (TableData.NetworkInfoRow row in pendingDelete)
            {
                row.Delete();
            }

            if (offset >= 0 && offset < dataGridViewNetworkInfo.Rows.Count)
                dataGridViewNetworkInfo.FirstDisplayedScrollingRowIndex = offset;
        }

        /// <summary>
        /// Kick and ban the selected player.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonBan_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridViewPlayers.SelectedRows)
            {
                using (FLHookSocket flCmd = new FLHookSocket())
                flCmd.CmdKickBan(((TableData.PlayerInfoRow)((DataRowView)row.DataBoundItem).Row).ColID);
            }
        }

        /// <summary>
        /// Kick the selected player.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonKick_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridViewPlayers.SelectedRows)
            {
                using (FLHookSocket flCmd = new FLHookSocket())
                    flCmd.CmdKick(((TableData.PlayerInfoRow)((DataRowView)row.DataBoundItem).Row).ColID);
            }
        }

        /// <summary>
        /// Thread safe logging of messages to screen and file.
        /// </summary>
        /// <param name="msg"></param>
        public void AddLog(string msg)
        {
            try
            {
                lock (locker)
                {
                    if (AppSettings.Default.setMainLogPath.Length > 0)
                    {
                        try
                        {
                            StreamWriter writer = File.AppendText(AppSettings.Default.setMainLogPath);
                            writer.WriteLine(msg);
                            writer.Close();
                        }
                        catch (Exception e)
                        {
                            string errMsg = "Write to game log failed: " + e.Message;
                            if (InvokeRequired)
                            {
                                try
                                {

                                    Invoke(new UpdateUIAddLogDelegate(UpdateUIAddLog), new object[] { errMsg });
                                }
                                catch { }
                            }
                            else
                            {
                                UpdateUIAddLog(errMsg);
                            }
                        }
                    }
                }

                if (InvokeRequired)
                {
                    try
                    {
                        Invoke(new UpdateUIAddLogDelegate(UpdateUIAddLog), new object[] { msg });
                    }
                    catch { }
                }
                else
                {
                    UpdateUIAddLog(msg);
                }
            }
            catch { }
        }

        /// <summary>
        /// Add an entry to the on screen log and unminimise the program if required.
        /// </summary>
        /// <param name="msg"></param>
        delegate void UpdateUIAddLogDelegate(string msg);
        private void UpdateUIAddLog(string msg)
        {
            string oldText = richTextBoxLog.Text;
            if (oldText.Length > 10000)
                oldText = oldText.Substring(0, 10000);
            richTextBoxLog.Text = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString()
                            + ":" + msg + "\n" + oldText;

            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        /// <summary>
        /// Add an entry to the event log. This is thread safe and the actual
        /// logging is done by UpdateUILogGameEvent
        /// </summary>
        /// <param name="msg"></param>
        void LogGameEvent(string msg)
        {
            lock (locker)
            {
                if (AppSettings.Default.setGameLogPath.Length > 0)
                {
                    try
                    {
                        StreamWriter writer = File.AppendText(AppSettings.Default.setGameLogPath);
                        writer.WriteLine(msg);
                        writer.Close();
                    }
                    catch (Exception e)
                    {
                        AddLog("Write to game log failed: " + e.Message);
                    }
                }
            }

            if (InvokeRequired)
            {
                Invoke(new UpdateUILogGameEventDelegate(UpdateUILogGameEvent), new object[] { msg });
            }
            else
            {
                UpdateUILogGameEvent(msg);
            }
        }

        /// <summary>
        /// Add an entry to the on event log
        /// </summary>
        /// <param name="msg"></param>
        delegate void UpdateUILogGameEventDelegate(string msg);
        private void UpdateUILogGameEvent(string msg)
        {
            // Add to on screen log.
            string oldText = richTextBoxEvents.Text;
            if (oldText.Length > 10000)
                oldText = oldText.Substring(0, 10000);
            richTextBoxEvents.Text = msg + "\n" + oldText;
        }
	}
}
