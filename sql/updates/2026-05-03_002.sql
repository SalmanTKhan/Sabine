-- Guild persistence (Phase 6). Promotes the in-memory `Guild` /
-- `GuildManager` stub to a DB-backed registry: every guild and its
-- member roster survives across restarts. Allies / antagonists /
-- custom rank names are out of scope for this round; they stay in
-- memory for now and can be persisted in a follow-up migration.

CREATE TABLE `guilds` (
	`guildId` int(11) NOT NULL AUTO_INCREMENT,
	`name` varchar(64) NOT NULL,
	`masterCharId` bigint(20) NOT NULL,
	`level` int(11) NOT NULL DEFAULT 1,
	`expCurrent` int(11) NOT NULL DEFAULT 0,
	`emblemId` int(11) NOT NULL DEFAULT 0,
	`notice` text NOT NULL,
	`createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`guildId`),
	UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

CREATE TABLE `guild_members` (
	`guildId` int(11) NOT NULL,
	`characterId` bigint(20) NOT NULL,
	`isMaster` tinyint(1) NOT NULL DEFAULT 0,
	`position` varchar(64) NOT NULL DEFAULT '',
	`joinedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`characterId`),
	KEY `guildId` (`guildId`),
	CONSTRAINT `fk_guild_members_guild` FOREIGN KEY (`guildId`)
		REFERENCES `guilds` (`guildId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
