# Domain Glossary

> Pure terminology — no implementation details. See `docs/adr/` for design decisions.

## Core Entities

### Machine (工位)
A physical workstation — one industrial PC with one or more network interface cards (NICs). Identified by a set of MAC addresses stored in the `MacAddresses` table. One machine runs one application instance. Referenced by `macs_id` throughout the system.

### Station (站点)
A logical station created within the application. One machine can host multiple stations. Represented by `workstation` in application code.

> **Note:** "workstation" in code refers to a logical station (站点), NOT the physical machine (工位).

### Mission / ProductMission (任务)
A manufacturing task assigned to a station. The central entity of the system. Each mission belongs to one machine (`macs_id`) and contains:
- **ProductSide (产品面)**: A side/view of the product being assembled
- **ProductBolt (螺丝点位)**: A bolt/screw position on a product side. Every bolt is a screw position — the term "螺丝点位" does NOT imply the presence of an arranger (排列机). Optional equipment includes arranger (`specification`/`arranger_id` for screw feeding) and setter selector (`bit_specification`/`setter_selector_id` for bit switching). Material codes bound to a bolt are stored in `parts_bar_code_ids` (comma-separated barcode matching rule IDs); these rules must be scanned when that bolt becomes active.

Missions can have dependencies: predecessor missions (`predecessor_mission_id`), challenge missions (`challenge_mission_id`), and part-specific predecessor missions.

Mission names must be **unique within a machine** (`macs_id` scope). Names are stored with an `{id} - ` prefix (e.g., `123 - 拧紧任务A`), which is automatically added on save and stripped for display in edit fields.

### Barcode Matching Rule (条码匹配规则)
A rule that matches scanned barcodes to missions. Configurable by barcode type (product/traceability code or parts code), length, end character, and key position matching.

## Key Concepts

### deleted field
Uses `YesOrNo` enum with standard semantics: `YES (1)` = deleted, `NO (2)` = not deleted. New records default to `NO (2)` — meaning "not deleted." Queries filter by `deleted = 2` to return only active records.

### Role-based visibility
- **DEVELOPER** / **ADMIN**: See all missions across all machines (no `macs_id` filter)
- **OPERATOR**: See only missions belonging to their machine (filtered by `macs_id`)

This applies to mission queries and duplicate name validation.

### Multi-site support
The system supports multiple factory sites (WHYC, SCII, GLB, YF, TZYX), each with its own machines.

### Tightening Data / Operation Data (拧紧数据 / 操作数据)
Measurement data for a single bolt operation, existing in three forms:
- **Raw** (`TighteningData`): data received from the tool
- **DTO** (`OperationDataDTO`): converted for database storage, gets a unique `id`
- **VO** (`OperationDataVO`): view object for UI display with grid column metadata

Can be either tightening (拧紧) or loosening (反松), determined by `result_type`.

### Curve Data (曲线数据)
Optional torque-angle curve samples associated with a tightening operation.
Association depends on tool type:
- **PF series**: follows tightening data in the same message (timing guarantee)
- **FIT series**: carries a `TighteningId` that matches the tightening data

Stored as `CurveDataTemp` (raw from tool) then `CurveDataDTO` (persisted with
`operation_data_id`).

### Loosening (反松)
A reverse operation that undoes a previously tightened bolt. Distinguished
from tightening by `result_type` field. Whether loosening data is stored
is controlled by the `data_storage_store_loosening_data` setting.

### Data Storage (数据存储)
The pipeline that persists tightening/loosening data from tools to database
and file exports. Comprises:
- **Online storage**: real-time database write via API for each operation
- **File export**: batch export to Excel/TXT on mission completion

Configurable via settings: storage path, field sort order, and whether to
store loosening data.

### Single-Tool Assumption (单工具假设)
**All current versions assume a single active tool per workstation at any
given time.** Tightening data and curve data are processed sequentially
within a single callback chain — there is no multi-tool concurrency within
one mission. This means timing-based association between tightening data
and curve data (FIFO order in a message queue) is sufficient. If multi-tool
support is added in the future, curve data must carry an explicit identifier
to match its corresponding tightening record.
