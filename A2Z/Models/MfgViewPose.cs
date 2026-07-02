// Step B1a (2026-05-19): 가공도 공통 3D 장면 결과 모델.
// 수동(ExecuteMfgDrawing)·자동(RenderMfgViewForDrawing) 공통 코어 BuildMfgSceneCore가 반환.
// T-036 전역 필드(_mfgDrawingZ90Applied 등) 대체 — 카메라 회전 의도를 객체로 캡슐화.
// LvDrawingSheet_SelectedIndexChanged 후처리 회전이 _lastMfgViewPose를 참조해 적용.
//
// Codex 3차 설계 (8필드):
//   - CameraData         : 카메라 상태 스냅샷
//   - ViewDirection      : "X"/"Y"/"Z" 정면 뷰 방향
//   - LongestAxis        : 부재 BBox 최장축
//   - CameraDirection    : SDK enum (X_PLUS/X_MINUS/Y_PLUS/Y_MINUS/Z_PLUS/Z_MINUS)
//   - UsedMinusCamera    : MINUS 방향 채택 여부 (EA 보정 시)
//   - ApplyZ90           : Z 최장축 시 90° 회전 필요 (가로로 길게 표시)
//   - ApplyR180          : EA L자 열린 방향 정렬용 180° 회전
//   - OrientationAngle   : ApplyOrientationRotation 보존값 (UDA ORIENTATION 기반)

namespace A2Z
{
    /// <summary>
    /// 가공도에서 캡처 후 실측 배율로 그릴 치수 한 건 (offset 미적용). Level: 0=체인1·1=체인2·2=전체.
    /// </summary>
    internal sealed class MfgPendingDim
    {
        public VIZCore3D.NET.Data.Vector3D Start;
        public VIZCore3D.NET.Data.Vector3D End;
        public string Axis;
        public int Level;     // 0=chain1, 1=chain2, 2=total
        public bool PosOff;
    }

    /// <summary>
    /// 가공도 3D 장면 생성 결과. BuildMfgSceneCore가 반환, 호출자가 후속 적용(회전·뷰 유지·2D 캡처)에 사용.
    /// </summary>
    internal sealed class MfgViewPose
    {
        public VIZCore3D.NET.Data.CameraData CameraData { get; set; }
        public string ViewDirection { get; set; }
        public string LongestAxis { get; set; }
        public VIZCore3D.NET.Data.CameraDirection CameraDirection { get; set; }
        public bool UsedMinusCamera { get; set; }
        public bool ApplyZ90 { get; set; }
        public bool ApplyR180 { get; set; }
        public string OrientationAxis { get; set; }
        public float OrientationAngle { get; set; }

        /// <summary>
        /// Step B3 (2026-05-19): 코어가 vizcore3d.ShapeDrawing.AddLine으로 등록한 보조선 ID 목록.
        /// 자동 어댑터(RenderMfgViewForDrawing)가 Add2DObjectFromShapeDrawing 호출에 사용.
        /// 수동 어댑터(ExecuteMfgDrawing)는 3D 시각화만 하므로 미사용.
        /// </summary>
        public System.Collections.Generic.List<int> ShapeDrawingIds { get; set; } = new System.Collections.Generic.List<int>();

        /// <summary>
        /// 그릴 치수 목록 (offset 미적용). BuildMfgSceneCore/BuildEaSecondaryScene가 수집하고,
        /// CaptureMfgSceneToViewArea가 모델 캡처 후 확정된 실측 newScale로 DrawMfgDimsAtScale에서 그린다.
        /// 추정 스케일(EstimateFitScaleForViewArea)은 2D 은선 투영 실측과 달라 보조선 길이가 어긋났음 (설계 §4.4 v2-c, 2026-07-01).
        /// </summary>
        public System.Collections.Generic.List<MfgPendingDim> PendingDims { get; set; } = new System.Collections.Generic.List<MfgPendingDim>();

        /// <summary>
        /// EA 접힘 모서리(두 플랜지가 만나는 변) 판정 — 두 뷰 상하 스왑용 (2026-07-02).
        /// CornerAxis = 1차 뷰 높이축, CornerAtMax = 코너가 그 축 max쪽에 있는지.
        /// Sec* = 2차 뷰 높이축(=1차 뷰 깊이축) 기준 동일 판정 — 2차 뷰 상하 미러 결정용.
        /// </summary>
        public bool HasCorner { get; set; }
        public string CornerAxis { get; set; }
        public bool CornerAtMax { get; set; }
        public bool HasSecCorner { get; set; }
        public string SecCornerAxis { get; set; }
        public bool SecCornerAtMax { get; set; }

        /// <summary>
        /// 이 뷰를 상하 반전해 그릴지 (2차 뷰 전용, 2026-07-02). 모델은 2D 미러(SetSelected3DMirrorBy2DView),
        /// 치수·보조선은 MirrorAxis 기준 3D 좌표 반전으로 함께 뒤집는다.
        /// </summary>
        public bool MirrorVertical { get; set; }
        public string MirrorAxis { get; set; }

        /// <summary>
        /// 두 뷰 상하 스왑 여부 — 코어(5-2a)에서 판정 (2026-07-02).
        /// 길이 치수를 '위 슬롯 뷰'에 배치하기 위해 치수 수집 전에 확정 필요.
        /// CornerAxisUp = +CornerAxis가 화면 위인지 (길이 치수 posOff 계산용).
        /// </summary>
        public bool SwapViews { get; set; }
        public bool CornerAxisUp { get; set; }

        /// <summary>
        /// 후처리 회전 필요 여부 (파생 property — ApplyZ90 OR ApplyR180).
        /// LvDrawingSheet_SelectedIndexChanged의 시트 진입 후처리 회전 분기에서 사용.
        /// </summary>
        public bool RequiresPostSelectionRotation
        {
            get { return ApplyZ90 || ApplyR180; }
        }
    }
}
