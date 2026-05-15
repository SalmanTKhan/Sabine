-- Homunculus persistence — replace the legacy `homun.*` packed-keys
-- store on `vars_character` with a dedicated row-per-character table.
-- One row per character (UNIQUE on `characterId`) so the typical
-- contract lifecycle is a single UPSERT per save.
--
-- Skills are denormalized into a delimited string column to mirror
-- the format `HomunculusComponent.SerializeSkills` already emits
-- (`<skillId>:<level>,...`). Keeping the format identical lets the
-- vars-fallback load path coexist while existing players migrate.

CREATE TABLE `homunculus` (
	`homunculusId` int(11) NOT NULL AUTO_INCREMENT,
	`characterId` bigint(20) NOT NULL,
	`type` int(11) NOT NULL DEFAULT 0,
	`state` int(11) NOT NULL DEFAULT 0,
	`name` varchar(24) NOT NULL DEFAULT '',
	`level` int(11) NOT NULL DEFAULT 1,
	`hp` int(11) NOT NULL DEFAULT 0,
	`hpMax` int(11) NOT NULL DEFAULT 0,
	`intimacy` int(11) NOT NULL DEFAULT 0,
	`hunger` int(11) NOT NULL DEFAULT 0,
	`exp` int(11) NOT NULL DEFAULT 0,
	`skillPoints` int(11) NOT NULL DEFAULT 0,
	`skills` text NOT NULL,
	`updatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
	PRIMARY KEY (`homunculusId`),
	UNIQUE KEY `uniq_character` (`characterId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
