# Graph Report - A2Z  (2026-08-30)

## Corpus Check
- 20 files · ~76,772 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 656 nodes · 1719 edges · 37 communities (30 shown, 7 thin omitted)
- Extraction: 82% EXTRACTED · 18% INFERRED · 0% AMBIGUOUS · INFERRED: 304 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `06773b58`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- DrawingSheetData
- .DiagLog
- Form1
- .BuildDrawingBomPreparationContext
- List
- .btnMainDimension_Click
- List
- MfgViewPose
- .BuildMfgSceneCore
- ChainDimensionData
- DrawingReferenceFrame
- BOMData
- .InitializeComponent
- .UpdateAttributeTable
- .TryBuildMfgOrientationReferenceFrame
- EventArgs
- .SearchStruByName
- .BuildBodyToPartNameMap
- Resources
- .DrawDimension
- InstallationConnectionData
- .ApplyGlobalView
- ClashData
- HoleInfo
- SlotHoleInfo
- .BuildMfgPendingNotes
- .btnExtractDimension_Click
- Models.cs
- .ComputeViewDimensionsForMembers
- DrawingSheetExportKind
- .Main
- MfgViewPose.cs
- .GeometryUtility_OnOsnapPickingItem

## God Nodes (most connected - your core abstractions)
1. `Form1` - 389 edges
2. `MfgViewPose` - 53 edges
3. `DrawingSheetData` - 49 edges
4. `BOMData` - 34 edges
5. `ChainDimensionData` - 32 edges
6. `DrawingReferenceFrame` - 24 edges
7. `InstallationConnectionData` - 17 edges
8. `A2Z` - 16 edges
9. `SlotHoleInfo` - 14 edges
10. `HoleInfo` - 13 edges

## Surprising Connections (you probably didn't know these)
- `Form1` --references--> `BOMData`  [EXTRACTED]
  A2Z/Form1.Attribute.cs → A2Z/Models.cs
- `Form1` --references--> `ChainDimensionData`  [EXTRACTED]
  A2Z/Form1.Attribute.cs → A2Z/Models.cs
- `Form1` --references--> `ClashData`  [EXTRACTED]
  A2Z/Form1.Attribute.cs → A2Z/Models.cs
- `Form1` --references--> `DrawingSheetData`  [EXTRACTED]
  A2Z/Form1.Attribute.cs → A2Z/Models.cs
- `Form1` --references--> `Label`  [EXTRACTED]
  A2Z/Form1.Attribute.cs → A2Z/Models.cs

## Import Cycles
- None detected.

## Communities (37 total, 7 thin omitted)

### Community 0 - "DrawingSheetData"
Cohesion: 0.05
Nodes (25): DrawingSheetExportKind, First, CameraDirection, IEnumerable, List, ListViewItem, Vector3D, KeyValuePair (+17 more)

### Community 1 - ".DiagLog"
Cohesion: 0.08
Nodes (9): Action, EventArgs, EventArgs, IEnumerable, List, Func, ItemCheckEventArgs, MfgDrawingResult (+1 more)

### Community 2 - "Form1"
Cohesion: 0.06
Nodes (22): axis, BodyBoundsData, A2Z, Form1, Control, Dictionary, HashSet, List (+14 more)

### Community 3 - ".BuildDrawingBomPreparationContext"
Cohesion: 0.09
Nodes (21): DrawingBomPartData, DrawingBomPreparationContext, DrawingBomSnapshot, BodyBoundsData, DrawingBomPartData, DrawingBomPreparationContext, DrawingBomSnapshot, Dictionary (+13 more)

### Community 4 - "List"
Cohesion: 0.08
Nodes (17): Dictionary, IList, List, ListViewItem, MfgAxisDetectionResult, IsTilted, MfgAxisDirectionBin, Direction (+9 more)

### Community 5 - ".btnMainDimension_Click"
Cohesion: 0.12
Nodes (9): ClashEventArgs, List, Node, Button, EventArgs, Panel, Control, Dictionary (+1 more)

### Community 6 - "List"
Cohesion: 0.11
Nodes (15): FabricationNeighborAssemblyNote, FabricationNeighborAssemblyNote, AssemblyIndex, AssemblyName, X, Y, Z, Node (+7 more)

### Community 7 - "MfgViewPose"
Cohesion: 0.06
Nodes (35): CameraData, MfgViewPose, ApplyR180, ApplyZ90, CameraData, CameraDirection, CornerAtMax, CornerAxis (+27 more)

### Community 8 - ".BuildMfgSceneCore"
Cohesion: 0.14
Nodes (8): angle, IEnumerable, Vector3D, Dictionary, nodeName, point, Vertex3D, orientAxis

### Community 9 - "ChainDimensionData"
Cohesion: 0.11
Nodes (18): List, ChainDimensionData, Axis, DisplayLevel, Distance, EndPoint, EndPointStr, IsMerged (+10 more)

### Community 10 - "DrawingReferenceFrame"
Cohesion: 0.15
Nodes (14): Vertex3D, DrawingReferenceFrame, AlignmentAngleDegrees, MaxX, MaxY, MaxZ, MinX, MinY (+6 more)

### Community 11 - "BOMData"
Cohesion: 0.11
Nodes (19): BOMData, CenterX, CenterY, CenterZ, CircleRadius, Holes, HoleSize, Index (+11 more)

### Community 12 - ".InitializeComponent"
Cohesion: 0.15
Nodes (12): CheckedListBox, ColumnHeader, DataGridView, Form, GroupBox, ListView, Size, Label (+4 more)

### Community 15 - "EventArgs"
Cohesion: 0.19
Nodes (3): dynamic, EventArgs, List

### Community 16 - ".SearchStruByName"
Cohesion: 0.19
Nodes (4): EventArgs, IEnumerable, List, Node

### Community 17 - ".BuildBodyToPartNameMap"
Cohesion: 0.14
Nodes (4): Dictionary, EventArgs, Timer, ViewKind

### Community 18 - "Resources"
Cohesion: 0.18
Nodes (9): ApplicationSettingsBase, A2Z.Properties, CultureInfo, Resources, Culture, ResourceManager, Settings, Default (+1 more)

### Community 19 - ".DrawDimension"
Cohesion: 0.29
Nodes (3): Func, Vertex3D, Vertex3DItemCollection

### Community 20 - "InstallationConnectionData"
Cohesion: 0.18
Nodes (11): InstallationConnectionData, ConnectedAssemblyIndex, ConnectedAssemblyName, ConnectedBodyIndex, ConnectedPartIndex, ConnectedPartName, ContactPoints, IsProximityFallback (+3 more)

### Community 22 - "ClashData"
Cohesion: 0.22
Nodes (9): ClashData, HasHotPoint, Index1, Index2, Name1, Name2, XValue, YValue (+1 more)

### Community 23 - "HoleInfo"
Cohesion: 0.22
Nodes (9): HoleInfo, CenterX, CenterY, CenterZ, CylinderBodyIndex, Diameter, ThroughAxis, ThroughAxisSource (+1 more)

### Community 24 - "SlotHoleInfo"
Cohesion: 0.22
Nodes (9): SlotHoleInfo, CenterX, CenterY, CenterZ, Depth, Radius, SlotLength, ThroughAxis (+1 more)

### Community 25 - ".BuildMfgPendingNotes"
Cohesion: 0.43
Nodes (5): Color, IEnumerable, IList, MfgPendingNote, Vertex3D

### Community 27 - "Models.cs"
Cohesion: 0.25
Nodes (7): RevisionEntry, Approved, Checked, Date, Description, Drawn, Rev

### Community 28 - ".ComputeViewDimensionsForMembers"
Cohesion: 0.62
Nodes (3): Dictionary, nodeName, point

### Community 31 - "DrawingSheetExportKind"
Cohesion: 0.50
Nodes (4): DrawingSheetExportKind, Assembly, Fabrication, Installation

## Knowledge Gaps
- **144 isolated node(s):** `Fabrication`, `Assembly`, `Installation`, `AssemblyIndex`, `AssemblyName` (+139 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Form1` connect `Form1` to `DrawingSheetData`, `.DiagLog`, `.BuildDrawingBomPreparationContext`, `List`, `.btnMainDimension_Click`, `List`, `MfgViewPose`, `.BuildMfgSceneCore`, `ChainDimensionData`, `DrawingReferenceFrame`, `BOMData`, `.InitializeComponent`, `.UpdateAttributeTable`, `.TryBuildMfgOrientationReferenceFrame`, `EventArgs`, `.SearchStruByName`, `.BuildBodyToPartNameMap`, `.DrawDimension`, `.ApplyGlobalView`, `ClashData`, `.BuildMfgPendingNotes`, `.btnExtractDimension_Click`, `.ComputeViewDimensionsForMembers`, `.CaptureMfgSceneToViewArea`, `.AddImageSlotIfExists`, `DrawingSheetExportKind`, `.Main`, `.GeometryUtility_OnOsnapPickingItem`?**
  _High betweenness centrality (0.813) - this node is a cross-community bridge._
- **Why does `MfgViewPose` connect `MfgViewPose` to `MfgViewPose.cs`, `Form1`, `.BuildMfgSceneCore`, `.TryBuildMfgOrientationReferenceFrame`, `.BuildMfgPendingNotes`, `.CaptureMfgSceneToViewArea`?**
  _High betweenness centrality (0.105) - this node is a cross-community bridge._
- **Why does `BOMData` connect `BOMData` to `.DiagLog`, `Form1`, `List`, `.btnMainDimension_Click`, `.BuildMfgSceneCore`, `.TryBuildMfgOrientationReferenceFrame`, `InstallationConnectionData`, `HoleInfo`, `SlotHoleInfo`, `.BuildMfgPendingNotes`, `Models.cs`, `.CaptureMfgSceneToViewArea`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **What connects `Fabrication`, `Assembly`, `Installation` to the rest of the system?**
  _144 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `DrawingSheetData` be split into smaller, more focused modules?**
  _Cohesion score 0.0546448087431694 - nodes in this community are weakly interconnected._
- **Should `.DiagLog` be split into smaller, more focused modules?**
  _Cohesion score 0.0824829931972789 - nodes in this community are weakly interconnected._
- **Should `Form1` be split into smaller, more focused modules?**
  _Cohesion score 0.06342494714587738 - nodes in this community are weakly interconnected._