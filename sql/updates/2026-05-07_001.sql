-- Party persistence. Promotes the in-memory `Party` / `PartyManager`
-- registry to a DB-backed one: every party and its member roster
-- survives across restarts. Mirrors the guild persistence introduced
-- in `2026-05-03_002.sql`.

CREATE TABLE `parties` (
	`partyId` int(11) NOT NULL AUTO_INCREMENT,
	`name` varchar(24) NOT NULL,
	`expShare` tinyint(1) NOT NULL DEFAULT 0,
	`itemShare` tinyint(1) NOT NULL DEFAULT 0,
	`leaderCharId` bigint(20) NOT NULL,
	`createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`partyId`),
	UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

CREATE TABLE `party_members` (
	`partyId` int(11) NOT NULL,
	`characterId` bigint(20) NOT NULL,
	`isLeader` tinyint(1) NOT NULL DEFAULT 0,
	`joinedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`characterId`),
	KEY `partyId` (`partyId`),
	CONSTRAINT `fk_party_members_party` FOREIGN KEY (`partyId`)
		REFERENCES `parties` (`partyId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
