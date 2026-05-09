-- Guild storage persistence (Phase C). Adds the `guild_storage_items`
-- table that mirrors `storage_items` column-for-column, swapping the
-- owner key from `accountId` to `guildId`. The FK cascades on guild
-- delete so disbanding a guild also wipes its shared stash.

CREATE TABLE `guild_storage_items` (
  `storageItemId` int(11) NOT NULL AUTO_INCREMENT,
  `guildId` int(11) NOT NULL,
  `classId` int(11) NOT NULL,
  `amount` int(11) NOT NULL,
  `refine` tinyint(4) NOT NULL DEFAULT 0,
  `identified` tinyint(4) NOT NULL DEFAULT 1,
  `damaged` tinyint(4) NOT NULL DEFAULT 0,
  PRIMARY KEY (`storageItemId`),
  KEY `guildId` (`guildId`),
  CONSTRAINT `guild_storage_items_ibfk_1` FOREIGN KEY (`guildId`) REFERENCES `guilds` (`guildId`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
