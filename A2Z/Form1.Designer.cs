namespace A2Z
{
    partial class Form1
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControlLeft = new System.Windows.Forms.TabControl();
            this.tabPageWork = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.panelClashButtons = new System.Windows.Forms.Panel();
            this.btnClashShowSelected = new System.Windows.Forms.Button();
            this.btnClashShowAll = new System.Windows.Forms.Button();
            this.lvClash = new System.Windows.Forms.ListView();
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.panelDimensionButtons = new System.Windows.Forms.Panel();
            this.btnDimensionShowSelected = new System.Windows.Forms.Button();
            this.btnDimensionDelete = new System.Windows.Forms.Button();
            this.lvDimension = new System.Windows.Forms.ListView();
            this.colDimNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimAxis = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimView = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimDistance = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimStart = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimEnd = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.panelOsnapButtons = new System.Windows.Forms.Panel();
            this.btnOsnapAdd = new System.Windows.Forms.Button();
            this.btnOsnapDelete = new System.Windows.Forms.Button();
            this.btnOsnapShowSelected = new System.Windows.Forms.Button();
            this.lvOsnap = new System.Windows.Forms.ListView();
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader14 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader11 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader12 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader13 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lvBOM = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panelBOMButtons = new System.Windows.Forms.Panel();
            this.btnMfgDrawing = new System.Windows.Forms.Button();
            this.btnBalloonAdjust = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnExtractDimension = new System.Windows.Forms.Button();
            this.btnCollectOsnap = new System.Windows.Forms.Button();
            this.btnExportPDF = new System.Windows.Forms.Button();
            this.btnGenerate2D = new System.Windows.Forms.Button();
            this.btnClashDetection = new System.Windows.Forms.Button();
            this.btnCollectBOM = new System.Windows.Forms.Button();
            this.btnShowISO = new System.Windows.Forms.Button();
            this.btnShowAxisX = new System.Windows.Forms.Button();
            this.btnShowAxisY = new System.Windows.Forms.Button();
            this.btnShowAxisZ = new System.Windows.Forms.Button();
            this.btnMainDimension = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.tabPageAttribute = new System.Windows.Forms.TabPage();
            this.tabPageDrawing = new System.Windows.Forms.TabPage();
            this.lvDrawingSheet = new System.Windows.Forms.ListView();
            this.colSheetNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSheetBase = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSheetMembers = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSheetCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panelDrawingHeader = new System.Windows.Forms.Panel();
            this.lblDrawingDesc = new System.Windows.Forms.Label();
            this.lblDrawingTitle = new System.Windows.Forms.Label();
            this.panelDrawingButtons = new System.Windows.Forms.Panel();
            this.btnGenerateSheets = new System.Windows.Forms.Button();
            this.btnDrawingISO = new System.Windows.Forms.Button();
            this.btnDrawingAxisX = new System.Windows.Forms.Button();
            this.btnDrawingAxisY = new System.Windows.Forms.Button();
            this.btnDrawingAxisZ = new System.Windows.Forms.Button();
            this.dgvAttributes = new System.Windows.Forms.DataGridView();
            this.panelAttributeButtons = new System.Windows.Forms.Panel();
            this.btnUdaImportCSV = new System.Windows.Forms.Button();
            this.btnUdaDelete = new System.Windows.Forms.Button();
            this.btnUdaEdit = new System.Windows.Forms.Button();
            this.btnUdaAdd = new System.Windows.Forms.Button();
            this.btnExportAttributeCSV = new System.Windows.Forms.Button();
            this.btnClearSelection = new System.Windows.Forms.Button();
            this.panelAttributeHeader = new System.Windows.Forms.Panel();
            this.lblSelectedNode = new System.Windows.Forms.Label();
            this.lblAttributeTitle = new System.Windows.Forms.Label();
            this.panelViewer = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControlLeft.SuspendLayout();
            this.tabPageWork.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.panelClashButtons.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.panelDimensionButtons.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.panelOsnapButtons.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panelBOMButtons.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPageAttribute.SuspendLayout();
            this.tabPageDrawing.SuspendLayout();
            this.panelDrawingHeader.SuspendLayout();
            this.panelDrawingButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAttributes)).BeginInit();
            this.panelAttributeButtons.SuspendLayout();
            this.panelAttributeHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabControlLeft);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panelViewer);
            this.splitContainer1.Size = new System.Drawing.Size(1600, 1000);
            this.splitContainer1.SplitterDistance = 457;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 0;
            // 
            // tabControlLeft
            // 
            this.tabControlLeft.Controls.Add(this.tabPageWork);
            this.tabControlLeft.Controls.Add(this.tabPageAttribute);
            this.tabControlLeft.Controls.Add(this.tabPageDrawing);
            this.tabControlLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlLeft.Location = new System.Drawing.Point(0, 0);
            this.tabControlLeft.Name = "tabControlLeft";
            this.tabControlLeft.SelectedIndex = 0;
            this.tabControlLeft.Size = new System.Drawing.Size(457, 1000);
            this.tabControlLeft.TabIndex = 0;
            // 
            // tabPageWork
            // 
            this.tabPageWork.Controls.Add(this.groupBox3);
            this.tabPageWork.Controls.Add(this.groupBox5);
            this.tabPageWork.Controls.Add(this.groupBox4);
            this.tabPageWork.Controls.Add(this.groupBox2);
            this.tabPageWork.Controls.Add(this.groupBox1);
            this.tabPageWork.Location = new System.Drawing.Point(4, 25);
            this.tabPageWork.Name = "tabPageWork";
            this.tabPageWork.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageWork.Size = new System.Drawing.Size(449, 971);
            this.tabPageWork.TabIndex = 0;
            this.tabPageWork.Text = "작업/데이터";
            this.tabPageWork.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.panelClashButtons);
            this.groupBox3.Controls.Add(this.lvClash);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(3, 743);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox3.Size = new System.Drawing.Size(443, 225);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Clash Detection (Z값 기준 정렬)";
            // 
            // panelClashButtons
            // 
            this.panelClashButtons.Controls.Add(this.btnClashShowSelected);
            this.panelClashButtons.Controls.Add(this.btnClashShowAll);
            this.panelClashButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelClashButtons.Location = new System.Drawing.Point(3, 183);
            this.panelClashButtons.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panelClashButtons.Name = "panelClashButtons";
            this.panelClashButtons.Size = new System.Drawing.Size(437, 38);
            this.panelClashButtons.TabIndex = 1;
            // 
            // btnClashShowSelected
            // 
            this.btnClashShowSelected.Location = new System.Drawing.Point(6, 4);
            this.btnClashShowSelected.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnClashShowSelected.Name = "btnClashShowSelected";
            this.btnClashShowSelected.Size = new System.Drawing.Size(137, 30);
            this.btnClashShowSelected.TabIndex = 0;
            this.btnClashShowSelected.Text = "선택 항목만 보기";
            this.btnClashShowSelected.UseVisualStyleBackColor = true;
            this.btnClashShowSelected.Click += new System.EventHandler(this.btnClashShowSelected_Click);
            // 
            // btnClashShowAll
            // 
            this.btnClashShowAll.Location = new System.Drawing.Point(149, 4);
            this.btnClashShowAll.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnClashShowAll.Name = "btnClashShowAll";
            this.btnClashShowAll.Size = new System.Drawing.Size(114, 30);
            this.btnClashShowAll.TabIndex = 1;
            this.btnClashShowAll.Text = "전체 보기";
            this.btnClashShowAll.UseVisualStyleBackColor = true;
            this.btnClashShowAll.Click += new System.EventHandler(this.btnClashShowAll_Click);
            // 
            // lvClash
            // 
            this.lvClash.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9});
            this.lvClash.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvClash.FullRowSelect = true;
            this.lvClash.GridLines = true;
            this.lvClash.HideSelection = false;
            this.lvClash.Location = new System.Drawing.Point(3, 22);
            this.lvClash.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lvClash.Name = "lvClash";
            this.lvClash.Size = new System.Drawing.Size(437, 199);
            this.lvClash.TabIndex = 0;
            this.lvClash.UseCompatibleStateImageBehavior = false;
            this.lvClash.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "부재 1";
            this.columnHeader7.Width = 120;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "부재 2";
            this.columnHeader8.Width = 120;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "Z 값";
            this.columnHeader9.Width = 100;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.panelDimensionButtons);
            this.groupBox5.Controls.Add(this.lvDimension);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox5.Location = new System.Drawing.Point(3, 555);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox5.Size = new System.Drawing.Size(443, 188);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "체인 치수 목록";
            // 
            // panelDimensionButtons
            // 
            this.panelDimensionButtons.Controls.Add(this.btnDimensionShowSelected);
            this.panelDimensionButtons.Controls.Add(this.btnDimensionDelete);
            this.panelDimensionButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelDimensionButtons.Location = new System.Drawing.Point(3, 146);
            this.panelDimensionButtons.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panelDimensionButtons.Name = "panelDimensionButtons";
            this.panelDimensionButtons.Size = new System.Drawing.Size(437, 38);
            this.panelDimensionButtons.TabIndex = 1;
            // 
            // btnDimensionShowSelected
            // 
            this.btnDimensionShowSelected.Location = new System.Drawing.Point(6, 4);
            this.btnDimensionShowSelected.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnDimensionShowSelected.Name = "btnDimensionShowSelected";
            this.btnDimensionShowSelected.Size = new System.Drawing.Size(126, 30);
            this.btnDimensionShowSelected.TabIndex = 0;
            this.btnDimensionShowSelected.Text = "선택 보기";
            this.btnDimensionShowSelected.UseVisualStyleBackColor = true;
            this.btnDimensionShowSelected.Click += new System.EventHandler(this.btnDimensionShowSelected_Click);
            // 
            // btnDimensionDelete
            // 
            this.btnDimensionDelete.Location = new System.Drawing.Point(137, 4);
            this.btnDimensionDelete.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnDimensionDelete.Name = "btnDimensionDelete";
            this.btnDimensionDelete.Size = new System.Drawing.Size(80, 30);
            this.btnDimensionDelete.TabIndex = 1;
            this.btnDimensionDelete.Text = "선택 삭제";
            this.btnDimensionDelete.UseVisualStyleBackColor = true;
            this.btnDimensionDelete.Click += new System.EventHandler(this.btnDimensionDelete_Click);
            // 
            // lvDimension
            // 
            this.lvDimension.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colDimNo,
            this.colDimAxis,
            this.colDimView,
            this.colDimDistance,
            this.colDimStart,
            this.colDimEnd});
            this.lvDimension.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvDimension.FullRowSelect = true;
            this.lvDimension.GridLines = true;
            this.lvDimension.HideSelection = false;
            this.lvDimension.Location = new System.Drawing.Point(3, 22);
            this.lvDimension.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lvDimension.Name = "lvDimension";
            this.lvDimension.Size = new System.Drawing.Size(437, 162);
            this.lvDimension.TabIndex = 0;
            this.lvDimension.UseCompatibleStateImageBehavior = false;
            this.lvDimension.View = System.Windows.Forms.View.Details;
            // 
            // colDimNo
            // 
            this.colDimNo.Text = "No";
            this.colDimNo.Width = 35;
            // 
            // colDimAxis
            // 
            this.colDimAxis.Text = "축";
            this.colDimAxis.Width = 35;
            // 
            // colDimView
            // 
            this.colDimView.Text = "뷰";
            this.colDimView.Width = 55;
            // 
            // colDimDistance
            // 
            this.colDimDistance.Text = "거리(mm)";
            this.colDimDistance.Width = 70;
            // 
            // colDimStart
            // 
            this.colDimStart.Text = "시작점";
            this.colDimStart.Width = 95;
            // 
            // colDimEnd
            // 
            this.colDimEnd.Text = "끝점";
            this.colDimEnd.Width = 95;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.panelOsnapButtons);
            this.groupBox4.Controls.Add(this.lvOsnap);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox4.Location = new System.Drawing.Point(3, 366);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox4.Size = new System.Drawing.Size(443, 189);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Osnap 좌표 목록";
            // 
            // panelOsnapButtons
            // 
            this.panelOsnapButtons.Controls.Add(this.btnOsnapAdd);
            this.panelOsnapButtons.Controls.Add(this.btnOsnapDelete);
            this.panelOsnapButtons.Controls.Add(this.btnOsnapShowSelected);
            this.panelOsnapButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelOsnapButtons.Location = new System.Drawing.Point(3, 147);
            this.panelOsnapButtons.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panelOsnapButtons.Name = "panelOsnapButtons";
            this.panelOsnapButtons.Size = new System.Drawing.Size(437, 38);
            this.panelOsnapButtons.TabIndex = 1;
            // 
            // btnOsnapAdd
            // 
            this.btnOsnapAdd.Location = new System.Drawing.Point(6, 4);
            this.btnOsnapAdd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOsnapAdd.Name = "btnOsnapAdd";
            this.btnOsnapAdd.Size = new System.Drawing.Size(103, 30);
            this.btnOsnapAdd.TabIndex = 0;
            this.btnOsnapAdd.Text = "좌표 추가";
            this.btnOsnapAdd.UseVisualStyleBackColor = true;
            this.btnOsnapAdd.Click += new System.EventHandler(this.btnOsnapAdd_Click);
            // 
            // btnOsnapDelete
            // 
            this.btnOsnapDelete.Location = new System.Drawing.Point(114, 4);
            this.btnOsnapDelete.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOsnapDelete.Name = "btnOsnapDelete";
            this.btnOsnapDelete.Size = new System.Drawing.Size(103, 30);
            this.btnOsnapDelete.TabIndex = 1;
            this.btnOsnapDelete.Text = "선택 삭제";
            this.btnOsnapDelete.UseVisualStyleBackColor = true;
            this.btnOsnapDelete.Click += new System.EventHandler(this.btnOsnapDelete_Click);
            // 
            // btnOsnapShowSelected
            // 
            this.btnOsnapShowSelected.Location = new System.Drawing.Point(223, 4);
            this.btnOsnapShowSelected.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOsnapShowSelected.Name = "btnOsnapShowSelected";
            this.btnOsnapShowSelected.Size = new System.Drawing.Size(114, 30);
            this.btnOsnapShowSelected.TabIndex = 2;
            this.btnOsnapShowSelected.Text = "선택 좌표 보기";
            this.btnOsnapShowSelected.UseVisualStyleBackColor = true;
            this.btnOsnapShowSelected.Click += new System.EventHandler(this.btnOsnapShowSelected_Click);
            // 
            // lvOsnap
            // 
            this.lvOsnap.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader10,
            this.columnHeader14,
            this.columnHeader11,
            this.columnHeader12,
            this.columnHeader13});
            this.lvOsnap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvOsnap.FullRowSelect = true;
            this.lvOsnap.GridLines = true;
            this.lvOsnap.HideSelection = false;
            this.lvOsnap.Location = new System.Drawing.Point(3, 22);
            this.lvOsnap.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lvOsnap.Name = "lvOsnap";
            this.lvOsnap.Size = new System.Drawing.Size(437, 163);
            this.lvOsnap.TabIndex = 0;
            this.lvOsnap.UseCompatibleStateImageBehavior = false;
            this.lvOsnap.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader10
            // 
            this.columnHeader10.Text = "No";
            this.columnHeader10.Width = 50;
            // 
            // columnHeader14
            // 
            this.columnHeader14.Text = "부재 이름";
            this.columnHeader14.Width = 120;
            // 
            // columnHeader11
            // 
            this.columnHeader11.Text = "X";
            this.columnHeader11.Width = 100;
            // 
            // columnHeader12
            // 
            this.columnHeader12.Text = "Y";
            this.columnHeader12.Width = 100;
            // 
            // columnHeader13
            // 
            this.columnHeader13.Text = "Z";
            this.columnHeader13.Width = 100;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lvBOM);
            this.groupBox2.Controls.Add(this.panelBOMButtons);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(3, 178);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Size = new System.Drawing.Size(443, 188);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "BOM 데이터";
            // 
            // lvBOM
            // 
            this.lvBOM.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.lvBOM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvBOM.FullRowSelect = true;
            this.lvBOM.GridLines = true;
            this.lvBOM.HideSelection = false;
            this.lvBOM.Location = new System.Drawing.Point(3, 22);
            this.lvBOM.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lvBOM.Name = "lvBOM";
            this.lvBOM.Size = new System.Drawing.Size(437, 128);
            this.lvBOM.TabIndex = 0;
            this.lvBOM.UseCompatibleStateImageBehavior = false;
            this.lvBOM.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "부재 이름";
            this.columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "각도";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "중심점";
            this.columnHeader3.Width = 150;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "X Min~Max";
            this.columnHeader4.Width = 120;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Y Min~Max";
            this.columnHeader5.Width = 120;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Z Min~Max";
            this.columnHeader6.Width = 120;
            // 
            // panelBOMButtons
            //
            this.panelBOMButtons.Controls.Add(this.btnBalloonAdjust);
            this.panelBOMButtons.Controls.Add(this.btnMfgDrawing);
            this.panelBOMButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBOMButtons.Location = new System.Drawing.Point(3, 150);
            this.panelBOMButtons.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panelBOMButtons.Name = "panelBOMButtons";
            this.panelBOMButtons.Size = new System.Drawing.Size(437, 34);
            this.panelBOMButtons.TabIndex = 1;
            // 
            // btnMfgDrawing
            // 
            this.btnMfgDrawing.Location = new System.Drawing.Point(6, 4);
            this.btnMfgDrawing.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnMfgDrawing.Name = "btnMfgDrawing";
            this.btnMfgDrawing.Size = new System.Drawing.Size(120, 26);
            this.btnMfgDrawing.TabIndex = 0;
            this.btnMfgDrawing.Text = "가공도 출력";
            this.btnMfgDrawing.UseVisualStyleBackColor = true;
            this.btnMfgDrawing.Click += new System.EventHandler(this.btnMfgDrawing_Click);
            //
            // btnBalloonAdjust
            //
            this.btnBalloonAdjust.Location = new System.Drawing.Point(132, 4);
            this.btnBalloonAdjust.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnBalloonAdjust.Name = "btnBalloonAdjust";
            this.btnBalloonAdjust.Size = new System.Drawing.Size(120, 26);
            this.btnBalloonAdjust.TabIndex = 1;
            this.btnBalloonAdjust.Text = "풍선 위치 조정";
            this.btnBalloonAdjust.UseVisualStyleBackColor = true;
            this.btnBalloonAdjust.Click += new System.EventHandler(this.btnBalloonAdjust_Click);
            //
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnExtractDimension);
            this.groupBox1.Controls.Add(this.btnCollectOsnap);
            this.groupBox1.Controls.Add(this.btnExportPDF);
            this.groupBox1.Controls.Add(this.btnGenerate2D);
            this.groupBox1.Controls.Add(this.btnClashDetection);
            this.groupBox1.Controls.Add(this.btnCollectBOM);
            this.groupBox1.Controls.Add(this.btnShowISO);
            this.groupBox1.Controls.Add(this.btnShowAxisX);
            this.groupBox1.Controls.Add(this.btnShowAxisY);
            this.groupBox1.Controls.Add(this.btnShowAxisZ);
            this.groupBox1.Controls.Add(this.btnMainDimension);
            this.groupBox1.Controls.Add(this.btnOpen);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Size = new System.Drawing.Size(443, 175);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "작업";
            // 
            // btnExtractDimension
            // 
            this.btnExtractDimension.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnExtractDimension.Location = new System.Drawing.Point(223, 131);
            this.btnExtractDimension.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnExtractDimension.Name = "btnExtractDimension";
            this.btnExtractDimension.Size = new System.Drawing.Size(57, 31);
            this.btnExtractDimension.TabIndex = 8;
            this.btnExtractDimension.Text = "치수";
            this.btnExtractDimension.UseVisualStyleBackColor = true;
            this.btnExtractDimension.Click += new System.EventHandler(this.btnExtractDimension_Click);
            // 
            // btnCollectOsnap
            // 
            this.btnCollectOsnap.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnCollectOsnap.Location = new System.Drawing.Point(149, 131);
            this.btnCollectOsnap.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnCollectOsnap.Name = "btnCollectOsnap";
            this.btnCollectOsnap.Size = new System.Drawing.Size(69, 31);
            this.btnCollectOsnap.TabIndex = 7;
            this.btnCollectOsnap.Text = "Osnap";
            this.btnCollectOsnap.UseVisualStyleBackColor = true;
            this.btnCollectOsnap.Click += new System.EventHandler(this.btnCollectOsnap_Click);
            // 
            // btnExportPDF
            // 
            this.btnExportPDF.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnExportPDF.Location = new System.Drawing.Point(349, 131);
            this.btnExportPDF.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnExportPDF.Name = "btnExportPDF";
            this.btnExportPDF.Size = new System.Drawing.Size(57, 31);
            this.btnExportPDF.TabIndex = 10;
            this.btnExportPDF.Text = "PDF";
            this.btnExportPDF.UseVisualStyleBackColor = true;
            this.btnExportPDF.Click += new System.EventHandler(this.btnExportPDF_Click);
            // 
            // btnGenerate2D
            // 
            this.btnGenerate2D.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnGenerate2D.Location = new System.Drawing.Point(286, 131);
            this.btnGenerate2D.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnGenerate2D.Name = "btnGenerate2D";
            this.btnGenerate2D.Size = new System.Drawing.Size(57, 31);
            this.btnGenerate2D.TabIndex = 9;
            this.btnGenerate2D.Text = "2D";
            this.btnGenerate2D.UseVisualStyleBackColor = true;
            this.btnGenerate2D.Click += new System.EventHandler(this.btnGenerate2D_Click);
            // 
            // btnClashDetection
            // 
            this.btnClashDetection.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnClashDetection.Location = new System.Drawing.Point(80, 131);
            this.btnClashDetection.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnClashDetection.Name = "btnClashDetection";
            this.btnClashDetection.Size = new System.Drawing.Size(63, 31);
            this.btnClashDetection.TabIndex = 6;
            this.btnClashDetection.Text = "Clash";
            this.btnClashDetection.UseVisualStyleBackColor = true;
            this.btnClashDetection.Click += new System.EventHandler(this.btnClashDetection_Click);
            // 
            // btnCollectBOM
            // 
            this.btnCollectBOM.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnCollectBOM.Location = new System.Drawing.Point(11, 131);
            this.btnCollectBOM.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnCollectBOM.Name = "btnCollectBOM";
            this.btnCollectBOM.Size = new System.Drawing.Size(63, 31);
            this.btnCollectBOM.TabIndex = 5;
            this.btnCollectBOM.Text = "BOM";
            this.btnCollectBOM.UseVisualStyleBackColor = true;
            this.btnCollectBOM.Click += new System.EventHandler(this.btnCollectBOM_Click);
            //
            // btnShowISO
            //
            this.btnShowISO.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(200)))), ((int)(((byte)(80)))));
            this.btnShowISO.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowISO.Location = new System.Drawing.Point(11, 85);
            this.btnShowISO.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnShowISO.Name = "btnShowISO";
            this.btnShowISO.Size = new System.Drawing.Size(100, 38);
            this.btnShowISO.TabIndex = 11;
            this.btnShowISO.Text = "ISO";
            this.btnShowISO.UseVisualStyleBackColor = false;
            this.btnShowISO.Click += new System.EventHandler(this.btnShowISO_Click);
            //
            // btnShowAxisX
            //
            this.btnShowAxisX.BackColor = System.Drawing.Color.LightCoral;
            this.btnShowAxisX.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisX.Location = new System.Drawing.Point(115, 85);
            this.btnShowAxisX.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnShowAxisX.Name = "btnShowAxisX";
            this.btnShowAxisX.Size = new System.Drawing.Size(106, 38);
            this.btnShowAxisX.TabIndex = 2;
            this.btnShowAxisX.Text = "X축";
            this.btnShowAxisX.UseVisualStyleBackColor = false;
            this.btnShowAxisX.Click += new System.EventHandler(this.btnShowAxisX_Click);
            //
            // btnShowAxisY
            //
            this.btnShowAxisY.BackColor = System.Drawing.Color.LightGreen;
            this.btnShowAxisY.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisY.Location = new System.Drawing.Point(225, 85);
            this.btnShowAxisY.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnShowAxisY.Name = "btnShowAxisY";
            this.btnShowAxisY.Size = new System.Drawing.Size(106, 38);
            this.btnShowAxisY.TabIndex = 3;
            this.btnShowAxisY.Text = "Y축";
            this.btnShowAxisY.UseVisualStyleBackColor = false;
            this.btnShowAxisY.Click += new System.EventHandler(this.btnShowAxisY_Click);
            //
            // btnShowAxisZ
            //
            this.btnShowAxisZ.BackColor = System.Drawing.Color.LightBlue;
            this.btnShowAxisZ.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisZ.Location = new System.Drawing.Point(335, 85);
            this.btnShowAxisZ.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnShowAxisZ.Name = "btnShowAxisZ";
            this.btnShowAxisZ.Size = new System.Drawing.Size(106, 38);
            this.btnShowAxisZ.TabIndex = 4;
            this.btnShowAxisZ.Text = "Z축";
            this.btnShowAxisZ.UseVisualStyleBackColor = false;
            this.btnShowAxisZ.Click += new System.EventHandler(this.btnShowAxisZ_Click);
            // 
            // btnMainDimension
            // 
            this.btnMainDimension.BackColor = System.Drawing.Color.SeaGreen;
            this.btnMainDimension.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMainDimension.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnMainDimension.ForeColor = System.Drawing.Color.White;
            this.btnMainDimension.Location = new System.Drawing.Point(234, 25);
            this.btnMainDimension.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnMainDimension.Name = "btnMainDimension";
            this.btnMainDimension.Size = new System.Drawing.Size(211, 50);
            this.btnMainDimension.TabIndex = 1;
            this.btnMainDimension.Text = "치수 추출";
            this.btnMainDimension.UseVisualStyleBackColor = false;
            this.btnMainDimension.Click += new System.EventHandler(this.btnMainDimension_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.BackColor = System.Drawing.Color.SteelBlue;
            this.btnOpen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpen.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnOpen.ForeColor = System.Drawing.Color.White;
            this.btnOpen.Location = new System.Drawing.Point(11, 25);
            this.btnOpen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(211, 50);
            this.btnOpen.TabIndex = 0;
            this.btnOpen.Text = "파일 열기";
            this.btnOpen.UseVisualStyleBackColor = false;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // tabPageAttribute
            // 
            this.tabPageAttribute.Controls.Add(this.dgvAttributes);
            this.tabPageAttribute.Controls.Add(this.panelAttributeButtons);
            this.tabPageAttribute.Controls.Add(this.panelAttributeHeader);
            this.tabPageAttribute.Location = new System.Drawing.Point(4, 25);
            this.tabPageAttribute.Name = "tabPageAttribute";
            this.tabPageAttribute.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAttribute.Size = new System.Drawing.Size(449, 971);
            this.tabPageAttribute.TabIndex = 1;
            this.tabPageAttribute.Text = "부재 정보";
            this.tabPageAttribute.UseVisualStyleBackColor = true;
            // 
            // dgvAttributes
            // 
            this.dgvAttributes.AllowUserToAddRows = false;
            this.dgvAttributes.AllowUserToDeleteRows = false;
            this.dgvAttributes.BackgroundColor = System.Drawing.Color.White;
            this.dgvAttributes.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvAttributes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAttributes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAttributes.Location = new System.Drawing.Point(3, 58);
            this.dgvAttributes.Name = "dgvAttributes";
            this.dgvAttributes.ReadOnly = true;
            this.dgvAttributes.RowHeadersVisible = false;
            this.dgvAttributes.RowTemplate.Height = 23;
            this.dgvAttributes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAttributes.Size = new System.Drawing.Size(443, 840);
            this.dgvAttributes.TabIndex = 1;
            // 
            // panelAttributeButtons
            // 
            this.panelAttributeButtons.Controls.Add(this.btnUdaImportCSV);
            this.panelAttributeButtons.Controls.Add(this.btnUdaDelete);
            this.panelAttributeButtons.Controls.Add(this.btnUdaEdit);
            this.panelAttributeButtons.Controls.Add(this.btnUdaAdd);
            this.panelAttributeButtons.Controls.Add(this.btnExportAttributeCSV);
            this.panelAttributeButtons.Controls.Add(this.btnClearSelection);
            this.panelAttributeButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelAttributeButtons.Location = new System.Drawing.Point(3, 898);
            this.panelAttributeButtons.Name = "panelAttributeButtons";
            this.panelAttributeButtons.Size = new System.Drawing.Size(443, 70);
            this.panelAttributeButtons.TabIndex = 2;
            // 
            // btnUdaImportCSV
            // 
            this.btnUdaImportCSV.Location = new System.Drawing.Point(200, 5);
            this.btnUdaImportCSV.Name = "btnUdaImportCSV";
            this.btnUdaImportCSV.Size = new System.Drawing.Size(94, 25);
            this.btnUdaImportCSV.TabIndex = 5;
            this.btnUdaImportCSV.Text = "CSV 입력";
            this.btnUdaImportCSV.UseVisualStyleBackColor = true;
            this.btnUdaImportCSV.Click += new System.EventHandler(this.btnUdaImportCSV_Click);
            // 
            // btnUdaDelete
            // 
            this.btnUdaDelete.Location = new System.Drawing.Point(200, 36);
            this.btnUdaDelete.Name = "btnUdaDelete";
            this.btnUdaDelete.Size = new System.Drawing.Size(94, 25);
            this.btnUdaDelete.TabIndex = 4;
            this.btnUdaDelete.Text = "UDA 삭제";
            this.btnUdaDelete.UseVisualStyleBackColor = true;
            this.btnUdaDelete.Click += new System.EventHandler(this.btnUdaDelete_Click);
            // 
            // btnUdaEdit
            // 
            this.btnUdaEdit.Location = new System.Drawing.Point(101, 36);
            this.btnUdaEdit.Name = "btnUdaEdit";
            this.btnUdaEdit.Size = new System.Drawing.Size(93, 25);
            this.btnUdaEdit.TabIndex = 3;
            this.btnUdaEdit.Text = "UDA 편집";
            this.btnUdaEdit.UseVisualStyleBackColor = true;
            this.btnUdaEdit.Click += new System.EventHandler(this.btnUdaEdit_Click);
            // 
            // btnUdaAdd
            // 
            this.btnUdaAdd.Location = new System.Drawing.Point(5, 36);
            this.btnUdaAdd.Name = "btnUdaAdd";
            this.btnUdaAdd.Size = new System.Drawing.Size(90, 25);
            this.btnUdaAdd.TabIndex = 2;
            this.btnUdaAdd.Text = "UDA 추가";
            this.btnUdaAdd.UseVisualStyleBackColor = true;
            this.btnUdaAdd.Click += new System.EventHandler(this.btnUdaAdd_Click);
            // 
            // btnExportAttributeCSV
            // 
            this.btnExportAttributeCSV.Location = new System.Drawing.Point(101, 5);
            this.btnExportAttributeCSV.Name = "btnExportAttributeCSV";
            this.btnExportAttributeCSV.Size = new System.Drawing.Size(94, 25);
            this.btnExportAttributeCSV.TabIndex = 1;
            this.btnExportAttributeCSV.Text = "CSV 출력";
            this.btnExportAttributeCSV.UseVisualStyleBackColor = true;
            this.btnExportAttributeCSV.Click += new System.EventHandler(this.btnExportAttributeCSV_Click);
            // 
            // btnClearSelection
            // 
            this.btnClearSelection.Location = new System.Drawing.Point(5, 5);
            this.btnClearSelection.Name = "btnClearSelection";
            this.btnClearSelection.Size = new System.Drawing.Size(90, 25);
            this.btnClearSelection.TabIndex = 0;
            this.btnClearSelection.Text = "선택 해제";
            this.btnClearSelection.UseVisualStyleBackColor = true;
            this.btnClearSelection.Click += new System.EventHandler(this.btnClearSelection_Click);
            // 
            // panelAttributeHeader
            // 
            this.panelAttributeHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.panelAttributeHeader.Controls.Add(this.lblSelectedNode);
            this.panelAttributeHeader.Controls.Add(this.lblAttributeTitle);
            this.panelAttributeHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAttributeHeader.Location = new System.Drawing.Point(3, 3);
            this.panelAttributeHeader.Name = "panelAttributeHeader";
            this.panelAttributeHeader.Size = new System.Drawing.Size(443, 55);
            this.panelAttributeHeader.TabIndex = 0;
            // 
            // lblSelectedNode
            // 
            this.lblSelectedNode.AutoSize = true;
            this.lblSelectedNode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSelectedNode.ForeColor = System.Drawing.Color.LightGray;
            this.lblSelectedNode.Location = new System.Drawing.Point(10, 32);
            this.lblSelectedNode.Name = "lblSelectedNode";
            this.lblSelectedNode.Size = new System.Drawing.Size(223, 20);
            this.lblSelectedNode.TabIndex = 1;
            this.lblSelectedNode.Text = "3D 뷰어에서 부재를 선택하세요";
            // 
            // lblAttributeTitle
            // 
            this.lblAttributeTitle.AutoSize = true;
            this.lblAttributeTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAttributeTitle.ForeColor = System.Drawing.Color.White;
            this.lblAttributeTitle.Location = new System.Drawing.Point(10, 8);
            this.lblAttributeTitle.Name = "lblAttributeTitle";
            this.lblAttributeTitle.Size = new System.Drawing.Size(205, 25);
            this.lblAttributeTitle.TabIndex = 0;
            this.lblAttributeTitle.Text = "부재 속성 (Attributes)";
            //
            // tabPageDrawing
            //
            this.tabPageDrawing.Controls.Add(this.lvDrawingSheet);
            this.tabPageDrawing.Controls.Add(this.panelDrawingButtons);
            this.tabPageDrawing.Controls.Add(this.panelDrawingHeader);
            this.tabPageDrawing.Location = new System.Drawing.Point(4, 25);
            this.tabPageDrawing.Name = "tabPageDrawing";
            this.tabPageDrawing.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDrawing.Size = new System.Drawing.Size(449, 971);
            this.tabPageDrawing.TabIndex = 2;
            this.tabPageDrawing.Text = "도면정보";
            this.tabPageDrawing.UseVisualStyleBackColor = true;
            //
            // panelDrawingHeader
            //
            this.panelDrawingHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.panelDrawingHeader.Controls.Add(this.lblDrawingDesc);
            this.panelDrawingHeader.Controls.Add(this.lblDrawingTitle);
            this.panelDrawingHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelDrawingHeader.Location = new System.Drawing.Point(3, 3);
            this.panelDrawingHeader.Name = "panelDrawingHeader";
            this.panelDrawingHeader.Size = new System.Drawing.Size(443, 55);
            this.panelDrawingHeader.TabIndex = 0;
            //
            // lblDrawingTitle
            //
            this.lblDrawingTitle.AutoSize = true;
            this.lblDrawingTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblDrawingTitle.ForeColor = System.Drawing.Color.White;
            this.lblDrawingTitle.Location = new System.Drawing.Point(10, 8);
            this.lblDrawingTitle.Name = "lblDrawingTitle";
            this.lblDrawingTitle.Size = new System.Drawing.Size(160, 25);
            this.lblDrawingTitle.TabIndex = 0;
            this.lblDrawingTitle.Text = "도면 시트 목록";
            //
            // lblDrawingDesc
            //
            this.lblDrawingDesc.AutoSize = true;
            this.lblDrawingDesc.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDrawingDesc.ForeColor = System.Drawing.Color.LightGray;
            this.lblDrawingDesc.Location = new System.Drawing.Point(10, 32);
            this.lblDrawingDesc.Name = "lblDrawingDesc";
            this.lblDrawingDesc.Size = new System.Drawing.Size(290, 20);
            this.lblDrawingDesc.TabIndex = 1;
            this.lblDrawingDesc.Text = "Clash 기반 BFS 탐색으로 시트를 생성합니다";
            //
            // lvDrawingSheet
            //
            this.lvDrawingSheet.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSheetNo,
            this.colSheetBase,
            this.colSheetMembers,
            this.colSheetCount});
            this.lvDrawingSheet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvDrawingSheet.FullRowSelect = true;
            this.lvDrawingSheet.GridLines = true;
            this.lvDrawingSheet.HideSelection = false;
            this.lvDrawingSheet.Location = new System.Drawing.Point(3, 58);
            this.lvDrawingSheet.Name = "lvDrawingSheet";
            this.lvDrawingSheet.Size = new System.Drawing.Size(443, 870);
            this.lvDrawingSheet.TabIndex = 1;
            this.lvDrawingSheet.UseCompatibleStateImageBehavior = false;
            this.lvDrawingSheet.View = System.Windows.Forms.View.Details;
            //
            // colSheetNo
            //
            this.colSheetNo.Text = "도면번호";
            this.colSheetNo.Width = 70;
            //
            // colSheetBase
            //
            this.colSheetBase.Text = "기준부재";
            this.colSheetBase.Width = 100;
            //
            // colSheetMembers
            //
            this.colSheetMembers.Text = "포함부재";
            this.colSheetMembers.Width = 180;
            //
            // colSheetCount
            //
            this.colSheetCount.Text = "부재수";
            this.colSheetCount.Width = 60;
            //
            // panelDrawingButtons
            //
            this.panelDrawingButtons.Controls.Add(this.btnDrawingAxisZ);
            this.panelDrawingButtons.Controls.Add(this.btnDrawingAxisY);
            this.panelDrawingButtons.Controls.Add(this.btnDrawingAxisX);
            this.panelDrawingButtons.Controls.Add(this.btnDrawingISO);
            this.panelDrawingButtons.Controls.Add(this.btnGenerateSheets);
            this.panelDrawingButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelDrawingButtons.Location = new System.Drawing.Point(3, 888);
            this.panelDrawingButtons.Name = "panelDrawingButtons";
            this.panelDrawingButtons.Size = new System.Drawing.Size(443, 80);
            this.panelDrawingButtons.TabIndex = 2;
            //
            // btnGenerateSheets
            //
            this.btnGenerateSheets.Location = new System.Drawing.Point(6, 5);
            this.btnGenerateSheets.Name = "btnGenerateSheets";
            this.btnGenerateSheets.Size = new System.Drawing.Size(120, 30);
            this.btnGenerateSheets.TabIndex = 0;
            this.btnGenerateSheets.Text = "도면 생성";
            this.btnGenerateSheets.UseVisualStyleBackColor = true;
            this.btnGenerateSheets.Click += new System.EventHandler(this.btnGenerateSheets_Click);
            //
            // btnDrawingISO
            //
            this.btnDrawingISO.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(200)))), ((int)(((byte)(80)))));
            this.btnDrawingISO.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnDrawingISO.Location = new System.Drawing.Point(6, 42);
            this.btnDrawingISO.Name = "btnDrawingISO";
            this.btnDrawingISO.Size = new System.Drawing.Size(100, 32);
            this.btnDrawingISO.TabIndex = 1;
            this.btnDrawingISO.Text = "ISO";
            this.btnDrawingISO.UseVisualStyleBackColor = false;
            this.btnDrawingISO.Click += new System.EventHandler(this.btnDrawingISO_Click);
            //
            // btnDrawingAxisX
            //
            this.btnDrawingAxisX.BackColor = System.Drawing.Color.LightCoral;
            this.btnDrawingAxisX.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnDrawingAxisX.Location = new System.Drawing.Point(112, 42);
            this.btnDrawingAxisX.Name = "btnDrawingAxisX";
            this.btnDrawingAxisX.Size = new System.Drawing.Size(100, 32);
            this.btnDrawingAxisX.TabIndex = 2;
            this.btnDrawingAxisX.Text = "X축";
            this.btnDrawingAxisX.UseVisualStyleBackColor = false;
            this.btnDrawingAxisX.Click += new System.EventHandler(this.btnDrawingAxisX_Click);
            //
            // btnDrawingAxisY
            //
            this.btnDrawingAxisY.BackColor = System.Drawing.Color.LightGreen;
            this.btnDrawingAxisY.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnDrawingAxisY.Location = new System.Drawing.Point(218, 42);
            this.btnDrawingAxisY.Name = "btnDrawingAxisY";
            this.btnDrawingAxisY.Size = new System.Drawing.Size(100, 32);
            this.btnDrawingAxisY.TabIndex = 3;
            this.btnDrawingAxisY.Text = "Y축";
            this.btnDrawingAxisY.UseVisualStyleBackColor = false;
            this.btnDrawingAxisY.Click += new System.EventHandler(this.btnDrawingAxisY_Click);
            //
            // btnDrawingAxisZ
            //
            this.btnDrawingAxisZ.BackColor = System.Drawing.Color.LightBlue;
            this.btnDrawingAxisZ.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnDrawingAxisZ.Location = new System.Drawing.Point(324, 42);
            this.btnDrawingAxisZ.Name = "btnDrawingAxisZ";
            this.btnDrawingAxisZ.Size = new System.Drawing.Size(100, 32);
            this.btnDrawingAxisZ.TabIndex = 4;
            this.btnDrawingAxisZ.Text = "Z축";
            this.btnDrawingAxisZ.UseVisualStyleBackColor = false;
            this.btnDrawingAxisZ.Click += new System.EventHandler(this.btnDrawingAxisZ_Click);
            //
            // panelViewer
            //
            this.panelViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelViewer.Location = new System.Drawing.Point(0, 0);
            this.panelViewer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panelViewer.Name = "panelViewer";
            this.panelViewer.Size = new System.Drawing.Size(1138, 1000);
            this.panelViewer.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 1000);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "2D 제작도 생성기 - VIZCore3D.NET";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControlLeft.ResumeLayout(false);
            this.tabPageWork.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.panelClashButtons.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.panelDimensionButtons.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.panelOsnapButtons.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.panelBOMButtons.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tabPageAttribute.ResumeLayout(false);
            this.tabPageDrawing.ResumeLayout(false);
            this.panelDrawingHeader.ResumeLayout(false);
            this.panelDrawingHeader.PerformLayout();
            this.panelDrawingButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvAttributes)).EndInit();
            this.panelAttributeButtons.ResumeLayout(false);
            this.panelAttributeHeader.ResumeLayout(false);
            this.panelAttributeHeader.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnMainDimension;
        private System.Windows.Forms.Panel panelViewer;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListView lvBOM;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.Button btnCollectBOM;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListView lvClash;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.Button btnClashDetection;
        private System.Windows.Forms.Button btnGenerate2D;
        private System.Windows.Forms.Button btnExportPDF;
        private System.Windows.Forms.Button btnCollectOsnap;
        private System.Windows.Forms.Button btnExtractDimension;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.ListView lvDimension;
        private System.Windows.Forms.ColumnHeader colDimNo;
        private System.Windows.Forms.ColumnHeader colDimAxis;
        private System.Windows.Forms.ColumnHeader colDimView;
        private System.Windows.Forms.ColumnHeader colDimDistance;
        private System.Windows.Forms.ColumnHeader colDimStart;
        private System.Windows.Forms.ColumnHeader colDimEnd;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ListView lvOsnap;
        private System.Windows.Forms.ColumnHeader columnHeader10;
        private System.Windows.Forms.ColumnHeader columnHeader14;
        private System.Windows.Forms.ColumnHeader columnHeader11;
        private System.Windows.Forms.ColumnHeader columnHeader12;
        private System.Windows.Forms.ColumnHeader columnHeader13;
        private System.Windows.Forms.Panel panelClashButtons;
        private System.Windows.Forms.Button btnClashShowSelected;
        private System.Windows.Forms.Button btnClashShowAll;
        private System.Windows.Forms.Panel panelOsnapButtons;
        private System.Windows.Forms.Button btnOsnapAdd;
        private System.Windows.Forms.Button btnOsnapDelete;
        private System.Windows.Forms.Button btnOsnapShowSelected;
        private System.Windows.Forms.Panel panelDimensionButtons;
        private System.Windows.Forms.Button btnDimensionShowSelected;
        private System.Windows.Forms.Button btnDimensionDelete;
        private System.Windows.Forms.Button btnShowISO;
        private System.Windows.Forms.Button btnShowAxisX;
        private System.Windows.Forms.Button btnShowAxisY;
        private System.Windows.Forms.Button btnShowAxisZ;
        private System.Windows.Forms.TabControl tabControlLeft;
        private System.Windows.Forms.TabPage tabPageWork;
        private System.Windows.Forms.TabPage tabPageAttribute;
        private System.Windows.Forms.Panel panelAttributeHeader;
        private System.Windows.Forms.Label lblAttributeTitle;
        private System.Windows.Forms.Label lblSelectedNode;
        private System.Windows.Forms.DataGridView dgvAttributes;
        private System.Windows.Forms.Panel panelAttributeButtons;
        private System.Windows.Forms.Button btnClearSelection;
        private System.Windows.Forms.Button btnExportAttributeCSV;
        private System.Windows.Forms.Button btnUdaAdd;
        private System.Windows.Forms.Button btnUdaEdit;
        private System.Windows.Forms.Button btnUdaDelete;
        private System.Windows.Forms.Button btnUdaImportCSV;
        private System.Windows.Forms.Panel panelBOMButtons;
        private System.Windows.Forms.Button btnMfgDrawing;
        private System.Windows.Forms.Button btnBalloonAdjust;
        private System.Windows.Forms.TabPage tabPageDrawing;
        private System.Windows.Forms.Panel panelDrawingHeader;
        private System.Windows.Forms.Label lblDrawingTitle;
        private System.Windows.Forms.Label lblDrawingDesc;
        private System.Windows.Forms.ListView lvDrawingSheet;
        private System.Windows.Forms.ColumnHeader colSheetNo;
        private System.Windows.Forms.ColumnHeader colSheetBase;
        private System.Windows.Forms.ColumnHeader colSheetMembers;
        private System.Windows.Forms.ColumnHeader colSheetCount;
        private System.Windows.Forms.Panel panelDrawingButtons;
        private System.Windows.Forms.Button btnGenerateSheets;
        private System.Windows.Forms.Button btnDrawingISO;
        private System.Windows.Forms.Button btnDrawingAxisX;
        private System.Windows.Forms.Button btnDrawingAxisY;
        private System.Windows.Forms.Button btnDrawingAxisZ;
    }
}
