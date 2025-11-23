CREATE TABLE `skills` (
  `charSkillId` bigint(20) NOT NULL,
  `characterId` bigint(20) NOT NULL,
  `skillId` smallint(6) NOT NULL,
  `level` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

ALTER TABLE `skills`
  ADD PRIMARY KEY (`charSkillId`),
  ADD UNIQUE KEY `character_skill_unique` (`characterId`,`skillId`),
  ADD KEY `characterId` (`characterId`);
  
ALTER TABLE `skills`
  MODIFY `charSkillId` bigint(20) NOT NULL AUTO_INCREMENT;

ALTER TABLE `skills`
  ADD CONSTRAINT `skills_ibfk_1` FOREIGN KEY (`characterId`) REFERENCES `characters` (`characterId`) ON DELETE CASCADE ON UPDATE CASCADE;