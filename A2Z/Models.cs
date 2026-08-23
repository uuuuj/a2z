using System.Collections.Generic;
using VIZCore3D.NET;

namespace A2Z
{
    /// <summary>
    /// 체인 치수 데이터 구조체
    /// </summary>
    public class ChainDimensionData
    {
        /// <summary>
        /// 치수 번호 (ListView와 일치)
        /// </summary>
        public int No { get; set; }
        public string Axis { get; set; }
        public string ViewName { get; set; }
        /// <summary>
        /// 이 치수가 보이는 뷰 방향 (T-028). "X"/"Y"/"Z" 또는 콤마 구분 "X,Y"(중복 제거 병합 시).
        /// null/공백 = 모든 뷰 공통. 글로벌 X/Y/Z 버튼 누르면 이 필드로 필터링.
        /// </summary>
        public string ViewDirection { get; set; }
        public float Distance { get; set; }
        public VIZCore3D.NET.Data.Vector3D StartPoint { get; set; }
        public VIZCore3D.NET.Data.Vector3D EndPoint { get; set; }
        public string StartPointStr { get; set; }
        public string EndPointStr { get; set; }
        public bool IsTotal { get; set; }

        /// <summary>
        /// 치수 우선순위 (높을수록 중요, 1~10)
        /// 10: 전체 길이, 8: 주요 구간(상위 30%), 5: 중간 구간, 3: 작은 구간, 1: 매우 작은 구간
        /// </summary>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// 표시 레벨 (0: 기본, 1~n: 추가 레벨)
        /// </summary>
        public int DisplayLevel { get; set; } = 0;

        /// <summary>
        /// 표시 여부 (필터링 후 결정)
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 병합된 치수 여부 (여러 짧은 치수를 하나로 통합)
        /// </summary>
        public bool IsMerged { get; set; } = false;

        /// <summary>
        /// 설치 위치를 설명하는 필수 치수. 스마트 필터의 개수 제한과 겹침 제거보다 우선한다.
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 이 치수의 두 점에 해당하는 부재 인덱스 (REQ-005, 2026-05-11)
        /// lvDimension 행 선택 시 3D 강조 + fit에 사용. ExtractInstallationDimensions에서 정확히 채움,
        /// ComputeViewDimensionsForMembers에서는 좌표↔nodeIdx 사후 매핑으로 채움. 비어있으면 핸들러 skip
        /// </summary>
        public List<int> MemberIndices { get; set; } = new List<int>();
    }

    /// <summary>
    /// BOM 데이터 구조체
    /// </summary>
    public class BOMData
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public float RotationAngle { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MinZ { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float MaxZ { get; set; }
        public float CircleRadius { get; set; }
        public string Purpose { get; set; }
        public string HoleSize { get; set; }
        public List<HoleInfo> Holes { get; set; }
        public string SlotHoleSize { get; set; }
        public List<SlotHoleInfo> SlotHoles { get; set; }

        public BOMData()
        {
            Holes = new List<HoleInfo>();
            HoleSize = "";
            SlotHoles = new List<SlotHoleInfo>();
            SlotHoleSize = "";
        }
    }

    /// <summary>
    /// 홀 정보 구조체
    /// </summary>
    public class HoleInfo
    {
        public float Diameter { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public int CylinderBodyIndex { get; set; }
        public VIZCore3D.NET.Data.Vector3D ThroughAxis { get; set; }
        public string ThroughAxisSource { get; set; }
    }

    /// <summary>
    /// 슬롯홀 정보 구조체
    /// </summary>
    public class SlotHoleInfo
    {
        public float Radius { get; set; }
        public float SlotLength { get; set; }
        public float Depth { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public VIZCore3D.NET.Data.Vector3D ThroughAxis { get; set; }
        public string ThroughAxisSource { get; set; }
    }

    /// <summary>
    /// Clash 데이터 구조체
    /// </summary>
    public class ClashData
    {
        public int Index1 { get; set; }
        public int Index2 { get; set; }
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public float XValue { get; set; }
        public float YValue { get; set; }
        public float ZValue { get; set; }
        public bool HasHotPoint { get; set; }
    }

    /// <summary>
    /// 설치도에서 선택 STRU와 외부 연결 Part 사이의 실제 접합 영역.
    /// Assembly 정보는 접합 노트의 문맥 이름으로만 보존한다.
    /// ContactPoints는 GeometryUtility 접합선의 시작/끝점이며, 접합선이 없는 근접 결과는 HotPoint 1개를 담는다.
    /// 이 점들은 화면 치수점이 아니라 접합측 Connected Body 모서리를 고르는 내부 판정 자료다.
    /// </summary>
    public class InstallationConnectionData
    {
        public int TargetPartIndex { get; set; }
        public int TargetBodyIndex { get; set; }
        public int ConnectedPartIndex { get; set; }
        public int ConnectedBodyIndex { get; set; }
        public int ConnectedAssemblyIndex { get; set; }
        public string ConnectedPartName { get; set; }
        public string ConnectedAssemblyName { get; set; }
        /// <summary>직접 연결 Part 단위 A/B/C 라벨. 같은 Body 쌍의 여러 접합영역은 같은 라벨을 공유한다.</summary>
        public string Label { get; set; }
        public bool IsProximityFallback { get; set; }
        public List<VIZCore3D.NET.Data.Vector3D> ContactPoints { get; set; }

        public InstallationConnectionData()
        {
            ContactPoints = new List<VIZCore3D.NET.Data.Vector3D>();
        }
    }

    /// <summary>
    /// 도면 시트 데이터 구조체
    /// </summary>
    public class DrawingSheetData
    {
        public int SheetNumber { get; set; }
        public string BaseMemberName { get; set; }
        public int BaseMemberIndex { get; set; }
        public List<int> MemberIndices { get; set; }
        public List<string> MemberNames { get; set; }
        public int MfgDrawingNo { get; set; }
        /// <summary>
        /// 같은 STRU에서 생성된 모든 도면이 공유하는 PAINT CODE(PNT 계열 UDA) — 첫 번째 값.
        /// null은 "아직 조회 안 함", 빈 문자열은 "조회했는데 값이 없음"이다 (캐시 판정 기준).
        /// </summary>
        public string PaintCode { get; set; }
        /// <summary>
        /// 두 번째 PAINT CODE (#68). 값이 하나뿐인 도면은 빈 문자열이다.
        /// 조회 여부 판정은 <see cref="PaintCode"/>로만 한다 — 둘은 항상 같이 채워진다.
        /// </summary>
        public string PaintCode2 { get; set; }
        public List<ChainDimensionData> PreparedDimensions { get; set; }
        public bool DimensionsPrepared { get; set; }
        public List<DrawingBomRowData> PreparedBomRows { get; set; }
        public bool BomPrepared { get; set; }
        /// <summary>
        /// 설치도에 점선으로 표시할 외부 부재 인덱스.
        /// #63부터 접합한 Part 하나가 아니라 그 Part가 속한 STRU 전체의 BODY가 들어간다
        /// (STRU를 못 찾은 경우에만 그 Part 인덱스). 배율·화면 맞춤 기준에는 쓰지 않는다.
        /// </summary>
        public List<int> InstallationContextIndices { get; set; }
        public List<InstallationConnectionData> InstallationConnections { get; set; }

        public DrawingSheetData()
        {
            MemberIndices = new List<int>();
            MemberNames = new List<string>();
            PreparedDimensions = new List<ChainDimensionData>();
            PreparedBomRows = new List<DrawingBomRowData>();
            InstallationContextIndices = new List<int>();
            InstallationConnections = new List<InstallationConnectionData>();
        }
    }

    /// <summary>
    /// 제작도 4면도에서 기울어진 어셈블리를 세계축에 맞춰 투영하기 위한 임시 로컬 좌표계.
    /// X/Y/Z는 월드 좌표의 단위벡터이고, Min/Max는 Origin 기준 로컬 좌표 범위다.
    /// </summary>
    internal sealed class DrawingReferenceFrame
    {
        public VIZCore3D.NET.Data.Vector3D XAxis { get; set; }
        public VIZCore3D.NET.Data.Vector3D YAxis { get; set; }
        public VIZCore3D.NET.Data.Vector3D ZAxis { get; set; }
        public VIZCore3D.NET.Data.Vector3D Origin { get; set; }
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MinZ { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float MaxZ { get; set; }
        public float AlignmentAngleDegrees { get; set; }
        public int SourceNodeIndex { get; set; }
    }

    /// <summary>
    /// 도면정보 탭 BOM 한 행의 사전 준비 데이터.
    /// ListViewItem 자체를 보관하지 않아 여러 시트 사이에서 안전하게 재사용한다.
    /// </summary>
    public class DrawingBomRowData
    {
        public string No { get; set; }
        public string Item { get; set; }
        public string Material { get; set; }
        public string Size { get; set; }
        public string Quantity { get; set; }
        public string TotalWeight { get; set; }
        public string Ma { get; set; }
        public string Fa { get; set; }
    }

    /// <summary>
    /// 표제부 REV 이력 표 한 행 (#64). 6칸 = REV./DATE/DESCRIPTION/DRAWN/CHECKED/APPROVED.
    /// Phase 1은 첫 기재행 1건만 쓰고, 이력 누적(Phase 3)에서 여러 건을 오름차순으로 넘긴다.
    /// </summary>
    public class RevisionEntry
    {
        public string Rev { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public string Drawn { get; set; }
        public string Checked { get; set; }
        public string Approved { get; set; }
    }
}
