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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnExtractDimension = new System.Windows.Forms.Button();
            this.btnCollectOsnap = new System.Windows.Forms.Button();
            this.btnExportPDF = new System.Windows.Forms.Button();
            this.btnGenerate2D = new System.Windows.Forms.Button();
            this.btnClashDetection = new System.Windows.Forms.Button();
            this.btnCollectBOM = new System.Windows.Forms.Button();
            this.btnShowAxisX = new System.Windows.Forms.Button();
            this.btnShowAxisY = new System.Windows.Forms.Button();
            this.btnShowAxisZ = new System.Windows.Forms.Button();
            this.btnMainDimension = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.panelViewer = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.panelClashButtons.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.panelDimensionButtons.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.panelOsnapButtons.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
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
            this.splitContainer1.Panel1.Controls.Add(this.groupBox3);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox5);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox4);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox2);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panelViewer);
            this.splitContainer1.Size = new System.Drawing.Size(1600, 1000);
            this.splitContainer1.SplitterDistance = 457;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.panelClashButtons);
            this.groupBox3.Controls.Add(this.lvClash);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(0, 740);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox3.Size = new System.Drawing.Size(457, 260);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Clash Detection (Z값 기준 정렬)";
            // 
            // panelClashButtons
            // 
            this.panelClashButtons.Controls.Add(this.btnClashShowSelected);
            this.panelClashButtons.Controls.Add(this.btnClashShowAll);
            this.panelClashButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelClashButtons.Location = new System.Drawing.Point(3, 218);
            this.panelClashButtons.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panelClashButtons.Name = "panelClashButtons";
            this.panelClashButtons.Size = new System.Drawing.Size(451, 38);
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
            this.lvClash.Size = new System.Drawing.Size(451, 234);
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
            this.groupBox5.Location = new System.Drawing.Point(0, 552);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox5.Size = new System.Drawing.Size(457, 188);
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
            this.panelDimensionButtons.Size = new System.Drawing.Size(451, 38);
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
            this.lvDimension.Size = new System.Drawing.Size(451, 162);
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
            this.groupBox4.Location = new System.Drawing.Point(0, 363);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox4.Size = new System.Drawing.Size(457, 189);
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
            this.panelOsnapButtons.Size = new System.Drawing.Size(451, 38);
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
            this.lvOsnap.Size = new System.Drawing.Size(451, 163);
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
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(0, 175);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Size = new System.Drawing.Size(457, 188);
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
            this.lvBOM.Size = new System.Drawing.Size(451, 162);
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
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Size = new System.Drawing.Size(457, 175);
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
            // btnShowAxisX
            // 
            this.btnShowAxisX.BackColor = System.Drawing.Color.LightCoral;
            this.btnShowAxisX.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisX.Location = new System.Drawing.Point(11, 85);
            this.btnShowAxisX.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnShowAxisX.Name = "btnShowAxisX";
            this.btnShowAxisX.Size = new System.Drawing.Size(137, 38);
            this.btnShowAxisX.TabIndex = 2;
            this.btnShowAxisX.Text = "X축";
            this.btnShowAxisX.UseVisualStyleBackColor = false;
            this.btnShowAxisX.Click += new System.EventHandler(this.btnShowAxisX_Click);
            // 
            // btnShowAxisY
            // 
            this.btnShowAxisY.BackColor = System.Drawing.Color.LightGreen;
            this.btnShowAxisY.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisY.Location = new System.Drawing.Point(160, 85);
            this.btnShowAxisY.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnShowAxisY.Name = "btnShowAxisY";
            this.btnShowAxisY.Size = new System.Drawing.Size(137, 38);
            this.btnShowAxisY.TabIndex = 3;
            this.btnShowAxisY.Text = "Y축";
            this.btnShowAxisY.UseVisualStyleBackColor = false;
            this.btnShowAxisY.Click += new System.EventHandler(this.btnShowAxisY_Click);
            // 
            // btnShowAxisZ
            // 
            this.btnShowAxisZ.BackColor = System.Drawing.Color.LightBlue;
            this.btnShowAxisZ.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnShowAxisZ.Location = new System.Drawing.Point(309, 85);
            this.btnShowAxisZ.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnShowAxisZ.Name = "btnShowAxisZ";
            this.btnShowAxisZ.Size = new System.Drawing.Size(137, 38);
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
            this.groupBox3.ResumeLayout(false);
            this.panelClashButtons.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.panelDimensionButtons.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.panelOsnapButtons.ResumeLayout(false);
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
