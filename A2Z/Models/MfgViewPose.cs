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
//   - ApplyR180          : EA L자 펼침용 180° 회전 (현재 isEA=false로 dead 분기)
//   - OrientationAngle   : ApplyOrientationRotation 보존값 (UDA ORIENTATION 기반)

namespace A2Z
{
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
        /// 후처리 회전 필요 여부 (파생 property — ApplyZ90 OR ApplyR180).
        /// LvDrawingSheet_SelectedIndexChanged의 시트 진입 후처리 회전 분기에서 사용.
        /// </summary>
        public bool RequiresPostSelectionRotation
        {
            get { return ApplyZ90 || ApplyR180; }
        }
    }
}
