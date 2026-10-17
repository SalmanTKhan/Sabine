-- Persistent guild skill levels. Guild skills are shared state and must not
-- be reconstructed from whichever member happens to be online.
CREATE TABLE `guild_skills` (
    `guildId` int(11) NOT NULL,
    `skillId` int(11) NOT NULL,
    `level` int(11) NOT NULL,
    PRIMARY KEY (`guildId`, `skillId`),
    CONSTRAINT `fk_guild_skills_guild` FOREIGN KEY (`guildId`)
        REFERENCES `guilds` (`guildId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
