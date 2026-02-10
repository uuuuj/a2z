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
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lvOsnap = new System.Windows.Forms.ListView();
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader14 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader11 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader12 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader13 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panelOsnapButtons = new System.Windows.Forms.Panel();
            this.btnOsnapAdd = new System.Windows.Forms.Button();
            this.btnOsnapDelete = new System.Windows.Forms.Button();
            this.btnOsnapShowSelected = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.panelClashButtons = new System.Windows.Forms.Panel();
            this.btnClashShowSelected = new System.Windows.Forms.Button();
            this.btnClashShowAll = new System.Windows.Forms.Button();
            this.lvClash = new System.Windows.Forms.ListView();
            this.panelDimensionButtons = new System.Windows.Forms.Panel();
            this.btnDimensionDelete = new System.Windows.Forms.Button();
            this.btnDimensionShowSelected = new System.Windows.Forms.Button();
            this.btnShowAxisX = new System.Windows.Forms.Button();
            this.btnShowAxisY = new System.Windows.Forms.Button();
            this.btnShowAxisZ = new System.Windows.Forms.Button();
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lvBOM = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnMainDimension = new System.Windows.Forms.Button();
            this.btnExportPDF = new System.Windows.Forms.Button();
            this.btnGenerate2D = new System.Windows.Forms.Button();
            this.btnClashDetection = new System.Windows.Forms.Button();
            this.btnCollectBOM = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnCollectOsnap = new System.Windows.Forms.Button();
            this.btnExtractDimension = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.lvDimension = new System.Windows.Forms.ListView();
            this.colDimNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimAxis = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimView = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimDistance = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimStart = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDimEnd = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panelViewer = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.panelClashButtons.SuspendLayout();
            this.panelOsnapButtons.SuspendLayout();
            this.panelDimensionButtons.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            //
            // splitContainer1
            //
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            //
            // splitContainer1.Panel1
            //
            this.splitContainer1.Panel1.Controls.Add(this.groupBox3);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox5);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox4);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox2);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            //
            // splitContainer1.Panel2
            //
            this.splitContainer1.Panel2.Controls.Add(this.panelViewer);
            this.splitContainer1.Size = new System.Drawing.Size(1400, 800);
            this.splitContainer1.SplitterDistance = 400;
            this.splitContainer1.TabIndex = 0;
            //
            // groupBox3
            //
            this.groupBox3.Controls.Add(this.panelClashButtons);
            this.groupBox3.Controls.Add(this.lvClash);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(0, 270);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(400, 160);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Clash Detection (Z값 기준 정렬)";
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
            this.lvClash.Location = new System.Drawing.Point(3, 17);
            this.lvClash.Name = "lvClash";
            this.lvClash.Size = new System.Drawing.Size(394, 360);
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
            // panelClashButtons
            //
            this.panelClashButtons.Controls.Add(this.btnClashShowSelected);
            this.panelClashButtons.Controls.Add(this.btnClashShowAll);
            this.panelClashButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelClashButtons.Location = new System.Drawing.Point(3, 130);
            this.panelClashButtons.Name = "panelClashButtons";
            this.panelClashButtons.Size = new System.Drawing.Size(394, 30);
            this.panelClashButtons.TabIndex = 1;
            //
            // btnClashShowSelected
            //
            this.btnClashShowSelected.Location = new System.Drawing.Point(5, 3);
            this.btnClashShowSelected.Name = "btnClashShowSelected";
            this.btnClashShowSelected.Size = new System.Drawing.Size(120, 24);
            this.btnClashShowSelected.TabIndex = 0;
            this.btnClashShowSelected.Text = "선택 항목만 보기";
            this.btnClashShowSelected.UseVisualStyleBackColor = true;
            this.btnClashShowSelected.Click += new System.EventHandler(this.btnClashShowSelected_Click);
            //
            // btnClashShowAll
            //
            this.btnClashShowAll.Location = new System.Drawing.Point(130, 3);
            this.btnClashShowAll.Name = "btnClashShowAll";
            this.btnClashShowAll.Size = new System.Drawing.Size(100, 24);
            this.btnClashShowAll.TabIndex = 1;
            this.btnClashShowAll.Text = "전체 보기";
            this.btnClashShowAll.UseVisualStyleBackColor = true;
            this.btnClashShowAll.Click += new System.EventHandler(this.btnClashShowAll_Click);
            //
            // groupBox4
            //
            this.groupBox4.Controls.Add(this.panelOsnapButtons);
            this.groupBox4.Controls.Add(this.lvOsnap);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox4.Location = new System.Drawing.Point(0, 430);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(400, 140);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Osnap 좌표 목록";
            //
            // groupBox5
            //
            this.groupBox5.Controls.Add(this.panelDimensionButtons);
            this.groupBox5.Controls.Add(this.lvDimension);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox5.Location = new System.Drawing.Point(0, 570);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(400, 150);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "체인 치수 목록";
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
            this.lvDimension.Location = new System.Drawing.Point(3, 17);
            this.lvDimension.Name = "lvDimension";
            this.lvDimension.Size = new System.Drawing.Size(394, 130);
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
            // panelDimensionButtons
            //
            this.panelDimensionButtons.Controls.Add(this.btnDimensionShowSelected);
            this.panelDimensionButtons.Controls.Add(this.btnDimensionDelete);
            this.panelDimensionButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelDimensionButtons.Location = new System.Drawing.Point(3, 97);
            this.panelDimensionButtons.Name = "panelDimensionButtons";
            this.panelDimensionButtons.Size = new System.Drawing.Size(394, 50);
            this.panelDimensionButtons.TabIndex = 1;
            //
            // btnDimensionShowSelected
            //
            this.btnDimensionShowSelected.Location = new System.Drawing.Point(5, 3);
            this.btnDimensionShowSelected.Name = "btnDimensionShowSelected";
            this.btnDimensionShowSelected.Size = new System.Drawing.Size(110, 24);
            this.btnDimensionShowSelected.TabIndex = 0;
            this.btnDimensionShowSelected.Text = "선택 보기";
            this.btnDimensionShowSelected.UseVisualStyleBackColor = true;
            this.btnDimensionShowSelected.Click += new System.EventHandler(this.btnDimensionShowSelected_Click);
            //
            // btnDimensionDelete
            //
            this.btnDimensionDelete.Location = new System.Drawing.Point(120, 3);
            this.btnDimensionDelete.Name = "btnDimensionDelete";
            this.btnDimensionDelete.Size = new System.Drawing.Size(70, 24);
            this.btnDimensionDelete.TabIndex = 1;
            this.btnDimensionDelete.Text = "선택 삭제";
            this.btnDimensionDelete.UseVisualStyleBackColor = true;
            this.btnDimensionDelete.Click += new System.EventHandler(this.btnDimensionDelete_Click);
            //
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
            this.lvOsnap.Location = new System.Drawing.Point(3, 17);
            this.lvOsnap.Name = "lvOsnap";
            this.lvOsnap.Size = new System.Drawing.Size(394, 160);
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
            // panelOsnapButtons
            //
            this.panelOsnapButtons.Controls.Add(this.btnOsnapAdd);
            this.panelOsnapButtons.Controls.Add(this.btnOsnapDelete);
            this.panelOsnapButtons.Controls.Add(this.btnOsnapShowSelected);
            this.panelOsnapButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelOsnapButtons.Location = new System.Drawing.Point(3, 117);
            this.panelOsnapButtons.Name = "panelOsnapButtons";
            this.panelOsnapButtons.Size = new System.Drawing.Size(394, 30);
            this.panelOsnapButtons.TabIndex = 1;
            //
            // btnOsnapAdd
            //
            this.btnOsnapAdd.Location = new System.Drawing.Point(5, 3);
            this.btnOsnapAdd.Name = "btnOsnapAdd";
            this.btnOsnapAdd.Size = new System.Drawing.Size(90, 24);
            this.btnOsnapAdd.TabIndex = 0;
            this.btnOsnapAdd.Text = "좌표 추가";
            this.btnOsnapAdd.UseVisualStyleBackColor = true;
            this.btnOsnapAdd.Click += new System.EventHandler(this.btnOsnapAdd_Click);
            //
            // btnOsnapDelete
            //
            this.btnOsnapDelete.Location = new System.Drawing.Point(100, 3);
            this.btnOsnapDelete.Name = "btnOsnapDelete";
            this.btnOsnapDelete.Size = new System.Drawing.Size(90, 24);
            this.btnOsnapDelete.TabIndex = 1;
            this.btnOsnapDelete.Text = "선택 삭제";
            this.btnOsnapDelete.UseVisualStyleBackColor = true;
            this.btnOsnapDelete.Click += new System.EventHandler(this.btnOsnapDelete_Click);
            //
            // btnOsnapShowSelected
            //
            this.btnOsnapShowSelected.Location = new System.Drawing.Point(195, 3);
            this.btnOsnapShowSelected.Name = "btnOsnapShowSelected";
            this.btnOsnapShowSelected.Size = new System.Drawing.Size(100, 24);
            this.btnOsnapShowSelected.TabIndex = 2;
            this.btnOsnapShowSelected.Text = "선택 좌표 보기";
            this.btnOsnapShowSelected.UseVisualStyleBackColor = true;
            this.btnOsnapShowSelected.Click += new System.EventHandler(this.btnOsnapShowSelected_Click);
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.lvBOM);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(0, 130);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(400, 150);
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
            this.lvBOM.Location = new System.Drawing.Point(3, 17);
            this.lvBOM.Name = "lvBOM";
            this.lvBOM.Size = new System.Drawing.Size(394, 280);
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
            this.columnHeader2.Width = 60;
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
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.btnExtractDimension);
            this.groupBox1.Controls.Add(this.btnCollectOsnap);
            this.groupBox1.Controls.Add(this.btnExportPDF);
            this.groupBox1.Controls.Add(this.btnGenerate2D);
            this.groupBox1.Controls.Add(this.btnClashDetection);
            this.groupBox1.Controls.Add(this.btnCollectBOM);
            this.groupBox1.Controls.Add(this.btnShowAxisX);
            this.groupBox1.Controls.Add(this.btnShowAxisY);
            this.groupBox1.Controls.Add(this.btnShowAxisZ);
            this.groupBox1.Controls.Add(this.btnMainDimension);
            this.groupBox1.Controls.Add(this.btnOpen);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(400, 165);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "작업";
            //
            // btnOpen - 메인 버튼
            //
            this.btnOpen.BackColor = System.Drawing.Color.SteelBlue;
            this.btnOpen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpen.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnOpen.ForeColor = System.Drawing.Color.White;
            this.btnOpen.Location = new System.Drawing.Point(10, 20);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(185, 40);
            this.btnOpen.TabIndex = 0;
            this.btnOpen.Text = "파일 열기";
            this.btnOpen.UseVisualStyleBackColor = false;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            //
            // btnMainDimension - 메인 버튼
            //
            this.btnMainDimension.BackColor = System.Drawing.Color.SeaGreen;
            this.btnMainDimension.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMainDimension.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnMainDimension.ForeColor = System.Drawing.Color.White;
            this.btnMainDimension.Location = new System.Drawing.Point(205, 20);
            this.btnMainDimension.Name = "btnMainDimension";
            this.btnMainDimension.Size = new System.Drawing.Size(185, 40);
            this.btnMainDimension.TabIndex = 1;
            this.btnMainDimension.Text = "치수 추출";
            this.btnMainDimension.UseVisualStyleBackColor = false;
            this.btnMainDimension.Click += new System.EventHandler(this.btnMainDimension_Click);
            //
            // btnShowAxisX - 중간 버튼 (groupBox1)
            //
            this.btnShowAxisX.BackColor = System.Drawing.Color.LightCoral;
            this.btnShowAxisX.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisX.Location = new System.Drawing.Point(10, 68);
            this.btnShowAxisX.Name = "btnShowAxisX";
            this.btnShowAxisX.Size = new System.Drawing.Size(120, 30);
            this.btnShowAxisX.TabIndex = 2;
            this.btnShowAxisX.Text = "X축";
            this.btnShowAxisX.UseVisualStyleBackColor = false;
            this.btnShowAxisX.Click += new System.EventHandler(this.btnShowAxisX_Click);
            //
            // btnShowAxisY - 중간 버튼 (groupBox1)
            //
            this.btnShowAxisY.BackColor = System.Drawing.Color.LightGreen;
            this.btnShowAxisY.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisY.Location = new System.Drawing.Point(140, 68);
            this.btnShowAxisY.Name = "btnShowAxisY";
            this.btnShowAxisY.Size = new System.Drawing.Size(120, 30);
            this.btnShowAxisY.TabIndex = 3;
            this.btnShowAxisY.Text = "Y축";
            this.btnShowAxisY.UseVisualStyleBackColor = false;
            this.btnShowAxisY.Click += new System.EventHandler(this.btnShowAxisY_Click);
            //
            // btnShowAxisZ - 중간 버튼 (groupBox1)
            //
            this.btnShowAxisZ.BackColor = System.Drawing.Color.LightBlue;
            this.btnShowAxisZ.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisZ.Location = new System.Drawing.Point(270, 68);
            this.btnShowAxisZ.Name = "btnShowAxisZ";
            this.btnShowAxisZ.Size = new System.Drawing.Size(120, 30);
            this.btnShowAxisZ.TabIndex = 4;
            this.btnShowAxisZ.Text = "Z축";
            this.btnShowAxisZ.UseVisualStyleBackColor = false;
            this.btnShowAxisZ.Click += new System.EventHandler(this.btnShowAxisZ_Click);
            //
            // btnCollectBOM - 서브 버튼
            //
            this.btnCollectBOM.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnCollectBOM.Location = new System.Drawing.Point(10, 105);
            this.btnCollectBOM.Name = "btnCollectBOM";
            this.btnCollectBOM.Size = new System.Drawing.Size(55, 25);
            this.btnCollectBOM.TabIndex = 5;
            this.btnCollectBOM.Text = "BOM";
            this.btnCollectBOM.UseVisualStyleBackColor = true;
            this.btnCollectBOM.Click += new System.EventHandler(this.btnCollectBOM_Click);
            //
            // btnClashDetection - 서브 버튼
            //
            this.btnClashDetection.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnClashDetection.Location = new System.Drawing.Point(70, 105);
            this.btnClashDetection.Name = "btnClashDetection";
            this.btnClashDetection.Size = new System.Drawing.Size(55, 25);
            this.btnClashDetection.TabIndex = 6;
            this.btnClashDetection.Text = "Clash";
            this.btnClashDetection.UseVisualStyleBackColor = true;
            this.btnClashDetection.Click += new System.EventHandler(this.btnClashDetection_Click);
            //
            // btnCollectOsnap - 서브 버튼
            //
            this.btnCollectOsnap.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnCollectOsnap.Location = new System.Drawing.Point(130, 105);
            this.btnCollectOsnap.Name = "btnCollectOsnap";
            this.btnCollectOsnap.Size = new System.Drawing.Size(60, 25);
            this.btnCollectOsnap.TabIndex = 7;
            this.btnCollectOsnap.Text = "Osnap";
            this.btnCollectOsnap.UseVisualStyleBackColor = true;
            this.btnCollectOsnap.Click += new System.EventHandler(this.btnCollectOsnap_Click);
            //
            // btnExtractDimension - 서브 버튼
            //
            this.btnExtractDimension.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnExtractDimension.Location = new System.Drawing.Point(195, 105);
            this.btnExtractDimension.Name = "btnExtractDimension";
            this.btnExtractDimension.Size = new System.Drawing.Size(50, 25);
            this.btnExtractDimension.TabIndex = 8;
            this.btnExtractDimension.Text = "치수";
            this.btnExtractDimension.UseVisualStyleBackColor = true;
            this.btnExtractDimension.Click += new System.EventHandler(this.btnExtractDimension_Click);
            //
            // btnGenerate2D - 서브 버튼
            //
            this.btnGenerate2D.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnGenerate2D.Location = new System.Drawing.Point(250, 105);
            this.btnGenerate2D.Name = "btnGenerate2D";
            this.btnGenerate2D.Size = new System.Drawing.Size(50, 25);
            this.btnGenerate2D.TabIndex = 9;
            this.btnGenerate2D.Text = "2D";
            this.btnGenerate2D.UseVisualStyleBackColor = true;
            this.btnGenerate2D.Click += new System.EventHandler(this.btnGenerate2D_Click);
            //
            // btnExportPDF - 서브 버튼
            //
            this.btnExportPDF.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btnExportPDF.Location = new System.Drawing.Point(305, 105);
            this.btnExportPDF.Name = "btnExportPDF";
            this.btnExportPDF.Size = new System.Drawing.Size(50, 25);
            this.btnExportPDF.TabIndex = 10;
            this.btnExportPDF.Text = "PDF";
            this.btnExportPDF.UseVisualStyleBackColor = true;
            this.btnExportPDF.Click += new System.EventHandler(this.btnExportPDF_Click);
            //
            // panelViewer
            //
            this.panelViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelViewer.Location = new System.Drawing.Point(0, 0);
            this.panelViewer.Name = "panelViewer";
            this.panelViewer.Size = new System.Drawing.Size(996, 800);
            this.panelViewer.TabIndex = 0;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 800);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "2D 제작도 생성기 - VIZCore3D.NET";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.panelClashButtons.ResumeLayout(false);
            this.panelOsnapButtons.ResumeLayout(false);
            this.panelDimensionButtons.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
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
        private System.Windows.Forms.Button btnShowAxisX;
        private System.Windows.Forms.Button btnShowAxisY;
        private System.Windows.Forms.Button btnShowAxisZ;
    }
}
