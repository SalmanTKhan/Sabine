-- Castle ownership state (Phase 8). One row per castle map; the
-- castle template (display name, on-break event) lives in
-- `system/data/castles.txt`. Rows are written lazily — the first
-- time a castle's state is touched (a script `SetCastleData` call
-- or a guild capture). Castles with no row are treated as
-- unoccupied with default values.

CREATE TABLE `castle_state` (
	`mapStringId` varchar(64) NOT NULL,
	`ownerGuildId` int(11) NOT NULL DEFAULT 0,
	`economy` int(11) NOT NULL DEFAULT 0,
	`defense` int(11) NOT NULL DEFAULT 0,
	`triggerE` int(11) NOT NULL DEFAULT 0,
	`triggerD` int(11) NOT NULL DEFAULT 0,
	`treasureFlags` int(11) NOT NULL DEFAULT 0,
	`updatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
	PRIMARY KEY (`mapStringId`),
	KEY `ownerGuildId` (`ownerGuildId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
