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
        public float ZValue { get; set; }
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

        public DrawingSheetData()
        {
            MemberIndices = new List<int>();
            MemberNames = new List<string>();
        }
    }
}
